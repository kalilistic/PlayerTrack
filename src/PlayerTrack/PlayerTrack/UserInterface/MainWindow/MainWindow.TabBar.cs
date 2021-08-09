namespace PlayerTrack
{
    /// <summary>
    /// Main Tab Bar for navigation.
    /// </summary>
    public partial class MainWindow
    {
        private void TabBar()
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (this.plugin.Configuration.CurrentView)
            {
                case View.PlayerDetail:
                    this.PlayerDetail();
                    break;
                case View.Lodestone:
                    this.Lodestone();
                    break;
                case View.AddPlayer:
                    this.AddPlayer();
                    break;
                case View.None:
                    break;
            }
        }
    }
}
