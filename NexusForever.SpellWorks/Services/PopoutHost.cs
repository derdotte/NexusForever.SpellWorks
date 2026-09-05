namespace NexusForever.SpellWorks.Services
{
    /// <summary>
    /// Keeps track of the detached panes: how many windows are open, which pane key each one carries, and
    /// what the workspace should look like once one goes away.
    /// </summary>
    /// <remarks>
    /// The windows themselves come from <see cref="IPopoutWindowFactory"/> rather than being constructed
    /// here, because none of the bookkeeping needs a real one - and the cap is the sort of rule that is
    /// only ever wrong in production if nothing exercises it.
    /// </remarks>
    public sealed class PopoutHost : IPopoutHost
    {
        public int OpenCount => _open.Count;
        public int Cap => 8;

        private readonly Dictionary<string, IPopoutWindow> _open = [];

        #region Dependency Injection

        private readonly IPopoutWindowFactory _windowFactory;
        private readonly WorkspaceState _state;

        public PopoutHost(
            IPopoutWindowFactory windowFactory,
            WorkspaceState state)
        {
            _windowFactory = windowFactory;
            _state         = state;
        }

        #endregion

        public string Popout(string viewId)
        {
            if (viewId == null || _open.Count >= Cap)
                return null;

            string key = viewId + "-" + Guid.NewGuid().ToString("N")[..8];

            _state.RegisterPopout(key, viewId);

            IPopoutWindow window = _windowFactory.Create(key, viewId);
            window.Closed += (_, _) =>
            {
                if (_open.Remove(key))
                    _state.UnregisterPopout(key, dockBack: false);
            };

            _open[key] = window;

            // Cascade so several pop-outs do not land on top of each other.
            int index = _open.Count - 1;
            (double left, double top) = _windowFactory.Anchor;

            window.Left = left + 240 + index * 34;
            window.Top  = top + 130 + index * 30;

            window.Show();

            return key;
        }

        public void Dock(string key)
        {
            if (!_open.Remove(key, out IPopoutWindow window))
                return;

            _state.UnregisterPopout(key, dockBack: true);
            window.Close();
        }

        public void Close(string key)
        {
            if (_open.TryGetValue(key, out IPopoutWindow window))
                window.Close();
        }

        public void CloseAll()
        {
            foreach (IPopoutWindow window in _open.Values.ToList())
                window.Close();

            _open.Clear();
        }
    }
}
