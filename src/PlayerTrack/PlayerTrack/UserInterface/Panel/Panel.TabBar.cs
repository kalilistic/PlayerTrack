using System;

namespace PlayerTrack
{
    /// <summary>
    /// Panel with tab bar.
    /// </summary>
    public partial class Panel
    {
        /// <summary>
        /// Draw panel.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">unrecognized view.</exception>
        public void Draw()
        {
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
