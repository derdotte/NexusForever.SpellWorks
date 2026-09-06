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

            /// <summary>Per-pane promoted flex columns, keyed by pane scope as the filters are.</summary>
            /// <remarks>
            /// Its own section rather than a member of the filter DTO, because the two are governed by
            /// their own preferences: a user can keep their promoted fields while starting clean.
            /// </remarks>
            [System.Text.Json.Serialization.JsonIgnore(
                Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public Dictionary<string, List<string>> Promoted { get; set; }
        }

        private static readonly JsonSerializerOptions options = new() { WriteIndented = true };

        /// <summary>
        /// The sections the last <see cref="Load"/> declined to read, because the preference governing
        /// them was off.
        /// </summary>
        /// <remarks>
        /// Kept so that saving from a session which started clean preserves them instead of writing the
        /// file back with nothing in their place. The switch governs what is <em>applied</em>, never what
        /// is kept - a setting that quietly deleted a user's saved work the first time the app wrote its
        /// workspace would be the one setting nobody could risk trying.
        ///
        /// They are pruned against the live scopes on the way out exactly as the live state is, so a
        /// session that started clean writes the same file a session that loaded would have.
        /// </remarks>
        private Dictionary<string, FilterQueryDto> _unreadFilters;
        private Dictionary<string, List<string>> _unreadPromoted;

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
                _state.Preferences.Locale          = snapshot.Preferences.Locale;
                _state.Preferences.LoadOnStart     = snapshot.Preferences.LoadOnStart;
                _state.Preferences.RestoreWindows  = snapshot.Preferences.RestoreWindows;
                _state.Preferences.MonospaceIds    = snapshot.Preferences.MonospaceIds;
                _state.Preferences.RailLabels      = snapshot.Preferences.RailLabels;
                _state.Preferences.RestoreFilters  = snapshot.Preferences.RestoreFilters;
                _state.Preferences.RestorePromoted = snapshot.Preferences.RestorePromoted;
            }

            if (snapshot.ColumnWidths != null)
                foreach ((string viewId, Dictionary<string, int> widths) in snapshot.ColumnWidths)
                    _state.ColumnWidths[viewId] = new Dictionary<string, int>(widths);

            // Read after the preferences block above, so the switch that governs these is the one the file
            // carries rather than the default it was constructed with. Both gate the load and not the
            // save: the sections stay in the file, ready for the switch to be turned back on.
            if (_state.Preferences.RestoreFilters)
                LoadFilters(snapshot.Filters);
            else
                _unreadFilters = snapshot.Filters;

            if (_state.Preferences.RestorePromoted)
                LoadPromoted(snapshot.Promoted);
            else
                _unreadPromoted = snapshot.Promoted;

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

                Filters = SaveFilters(),
                Promoted = SavePromoted()
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
            HashSet<string> live = LiveScopes();

            Dictionary<string, FilterQueryDto> filters = [];

            foreach ((string scope, PaneState pane) in _state.PaneStates)
            {
                if (!live.Contains(scope))
                    continue;

                if (FilterQueryDtoMapper.ToDto(pane.Filters) is { } dto)
                    filters[scope] = dto;
            }

            // A pane this session never filtered keeps whatever the file already held for it. The live
            // one wins where there is both: the user filtering a pane is them saying what it should be.
            foreach ((string scope, FilterQueryDto dto) in _unreadFilters ?? [])
                if (live.Contains(scope) && !filters.ContainsKey(scope))
                    filters[scope] = dto;

            return filters.Count > 0 ? filters : null;
        }

        /// <summary>The scopes a pane is still reachable through, and so still worth persisting.</summary>
        private HashSet<string> LiveScopes() =>
        [
            .. _state.Open,
            .. _state.Pinned,
            .. _state.Popouts.Select(p => p.ViewId)
        ];

        /// <summary>
        /// The promoted flex columns of every pane still worth remembering.
        /// </summary>
        /// <remarks>
        /// Pruned to live scopes exactly as <see cref="SaveFilters"/> is, and for the same reason: a
        /// <c>detail:7</c> the user opened once should not leave a promotion behind forever.
        /// </remarks>
        private Dictionary<string, List<string>> SavePromoted()
        {
            HashSet<string> live = LiveScopes();

            Dictionary<string, List<string>> promoted = [];

            foreach ((string scope, PaneState pane) in _state.PaneStates)
            {
                if (!live.Contains(scope) || pane.Promoted.Count == 0)
                    continue;

                promoted[scope] = [.. pane.Promoted];
            }

            foreach ((string scope, List<string> keys) in _unreadPromoted ?? [])
                if (live.Contains(scope) && !promoted.ContainsKey(scope))
                    promoted[scope] = keys;

            return promoted.Count > 0 ? promoted : null;
        }

        private void LoadPromoted(Dictionary<string, List<string>> promoted)
        {
            if (promoted == null || _schemas == null)
                return;

            foreach ((string scope, List<string> keys) in promoted)
            {
                // Per scope, as the filters are: one unreadable entry costs its own pane and no more.
                try
                {
                    FilterSchema schema = _schemas.For(_state.Describe(scope));
                    if (schema == null)
                        continue;

                    PaneState pane = _state.PaneStateFor(scope);
                    pane.Promoted.Clear();

                    // A column the archive no longer carries is dropped rather than kept: unlike a value
                    // that no longer parses there is nothing left to render or repair, which is the same
                    // stance FilterQueryDtoMapper takes on an unknown field key.
                    foreach (string key in (keys ?? []).Distinct())
                        if (schema.Field(key) is FilterColumnFieldSchema)
                            pane.Promoted.Add(key);
                }
                catch (Exception)
                {
                }
            }
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
