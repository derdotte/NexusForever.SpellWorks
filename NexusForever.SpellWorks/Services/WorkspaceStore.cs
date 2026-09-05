using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using NexusForever.SpellWorks.Services.Filtering;

namespace NexusForever.SpellWorks.Services
{
    /// <summary>
    /// Persists the workspace (open views, layout, pinned tables, preferences) beside
    /// <c>Configuration.json</c>, and writes the patch path back into it.
    /// </summary>
    public sealed class WorkspaceStore
    {
        private sealed class Snapshot
        {
            public List<string> Open { get; set; }
            public string Active { get; set; }
            public string Layout { get; set; }
            public Dictionary<string, double> Flexes { get; set; }
            public List<string> Pinned { get; set; }
            public Preferences Preferences { get; set; }
            public List<string> Popouts { get; set; }
            public Dictionary<string, Dictionary<string, int>> ColumnWidths { get; set; }

            /// <summary>Per-pane filters, keyed by pane scope - the same key <c>PaneStateFor</c> uses.</summary>
            /// <remarks>Omitted entirely when nothing is filtered, so an unfiltered workspace reads as one.</remarks>
            [System.Text.Json.Serialization.JsonIgnore(
                Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public Dictionary<string, FilterQueryDto> Filters { get; set; }
        }

        private static readonly JsonSerializerOptions options = new() { WriteIndented = true };

        private readonly string _workspacePath;
        private readonly string _configurationPath;

        #region Dependency Injection

        private readonly WorkspaceState _state;
        private readonly FilterSchemaRegistry _schemas;

        public WorkspaceStore(
            WorkspaceState state,
            FilterSchemaRegistry schemas)
            : this(state, schemas, AppContext.BaseDirectory)
        {
        }

        /// <summary>
        /// Persist into <paramref name="directory"/> rather than beside the executable. Tests point this at
        /// a scratch folder so a save never writes into the install.
        /// </summary>
        public WorkspaceStore(
            WorkspaceState state,
            FilterSchemaRegistry schemas,
            string directory)
        {
            _state             = state;
            _schemas           = schemas;
            _workspacePath     = Path.Combine(directory, "Workspace.json");
            _configurationPath = Path.Combine(directory, "Configuration.json");
        }

        #endregion

        /// <summary>
        /// View ids that were popped out when the workspace was last saved. Honoured only when
        /// <see cref="Preferences.RestoreWindows"/> is set.
        /// </summary>
        public List<string> RestorablePopouts { get; private set; } = [];

        public void Load()
        {
            if (!File.Exists(_workspacePath))
                return;

            Snapshot snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(_workspacePath));
            }
            catch (Exception)
            {
                // A corrupt workspace file is not worth failing startup over - fall back to the defaults.
                return;
            }

            if (snapshot == null)
                return;

            if (snapshot.Open is { Count: > 0 })
            {
                _state.Open.Clear();
                _state.Open.AddRange(snapshot.Open);
            }

            if (snapshot.Active != null && _state.Open.Contains(snapshot.Active))
                _state.ActivateOnly(snapshot.Active);

            if (Enum.TryParse(snapshot.Layout, out LayoutMode layout))
                _state.SetLayout(layout);

            if (snapshot.Flexes != null)
                foreach ((string id, double flex) in snapshot.Flexes)
                    _state.Flexes[id] = flex;

            if (snapshot.Pinned != null)
            {
                _state.Pinned.Clear();
                _state.Pinned.AddRange(snapshot.Pinned);
            }

            if (snapshot.Preferences != null)
            {
                _state.Preferences.Locale         = snapshot.Preferences.Locale;
                _state.Preferences.LoadOnStart    = snapshot.Preferences.LoadOnStart;
                _state.Preferences.RestoreWindows = snapshot.Preferences.RestoreWindows;
                _state.Preferences.MonospaceIds   = snapshot.Preferences.MonospaceIds;
                _state.Preferences.RailLabels     = snapshot.Preferences.RailLabels;
            }

            if (snapshot.ColumnWidths != null)
                foreach ((string viewId, Dictionary<string, int> widths) in snapshot.ColumnWidths)
                    _state.ColumnWidths[viewId] = new Dictionary<string, int>(widths);

            LoadFilters(snapshot.Filters);

            RestorablePopouts = snapshot.Popouts ?? [];
        }

        public void Save()
        {
            var snapshot = new Snapshot
            {
                Open        = [.. _state.Open],
                Active      = _state.Active,
                Layout      = _state.Layout.ToString(),
                Flexes      = new Dictionary<string, double>(_state.Flexes),
                Pinned      = [.. _state.Pinned],
                Preferences = _state.Preferences,
                Popouts     = _state.Popouts.Select(p => p.ViewId).ToList(),

                ColumnWidths = _state.ColumnWidths.ToDictionary(
                    e => e.Key,
                    e => new Dictionary<string, int>(e.Value)),

                Filters = SaveFilters()
            };

            TryWrite(_workspacePath, JsonSerializer.Serialize(snapshot, options));
        }

        /// <summary>
        /// The filters worth keeping: those of panes that are open, pinned or popped out, and not empty.
        /// </summary>
        /// <remarks>
        /// Spawned scopes - <c>detail:2</c>, <c>effecttype:3</c> - are created liberally as the user follows
        /// cross-references, so persisting every one of them would grow the file forever with entries no pane
        /// will ever read back.
        /// </remarks>
        private Dictionary<string, FilterQueryDto> SaveFilters()
        {
            HashSet<string> live =
            [
                .. _state.Open,
                .. _state.Pinned,
                .. _state.Popouts.Select(p => p.ViewId)
            ];

            Dictionary<string, FilterQueryDto> filters = [];

            foreach ((string scope, PaneState pane) in _state.PaneStates)
            {
                if (!live.Contains(scope))
                    continue;

                if (FilterQueryDtoMapper.ToDto(pane.Filters) is { } dto)
                    filters[scope] = dto;
            }

            return filters.Count > 0 ? filters : null;
        }

        private void LoadFilters(Dictionary<string, FilterQueryDto> filters)
        {
            if (filters == null || _schemas == null)
                return;

            foreach ((string scope, FilterQueryDto dto) in filters)
            {
                // Per scope, so one unreadable entry costs its own pane's filter and nothing else - a
                // corrupt condition must never take the layout down with it.
                try
                {
                    FilterSchema schema = _schemas.For(_state.Describe(scope));
                    FilterQueryDtoMapper.Load(_state.PaneStateFor(scope).Filters, dto, schema);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Rewrite <c>PatchPath</c> in <c>Configuration.json</c>, leaving any other keys untouched.
        /// </summary>
        public void SavePatchPath(string patchPath)
        {
            JsonObject root = null;

            if (File.Exists(_configurationPath))
            {
                try
                {
                    root = JsonNode.Parse(File.ReadAllText(_configurationPath)) as JsonObject;
                }
                catch (Exception)
                {
                    root = null;
                }
            }

            root ??= [];
            root["PatchPath"] = patchPath;

            TryWrite(_configurationPath, root.ToJsonString(options));
        }

        private static void TryWrite(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
            }
            catch (IOException)
            {
                // Read-only install directory; the workspace is a convenience, not a requirement.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
