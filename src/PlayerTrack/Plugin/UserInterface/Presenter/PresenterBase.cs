// ReSharper disable InconsistentNaming

namespace PlayerTrack
{
    public abstract class PresenterBase
    {
        protected IPlayerTrackPlugin _plugin;
        protected IWindowBase _view;

        protected PresenterBase(IPlayerTrackPlugin plugin)
        {
            _plugin = plugin;
        }

        public void DrawView()
        {
            if (!_plugin.IsLoggedIn()) return;
            _view.DrawView();
        }

        public void ToggleView()
        {
            _view.ToggleView();
        }

        public void ShowView()
        {
            _view.ShowView();
        }

        public void HideView()
        {
            _view.HideView();
        }

        public bool IsVisible()
        {
            return _view.IsVisible;
        }
    }
}