using Dalamud.Game.Text.SeStringHandling;

namespace PlayerTrack.Models;

public class PlayerNameplate
{
    public bool CustomizeNameplate { get; set; }
    public bool NameplateUseColorIfDead { get; set; }
    public bool HasCustomTitle { get; set; }
    public SeString? CustomTitle { get; set; }
    
    public SeString? TitleLeftQuote { get; set; }
    
    public SeString? TitleRightQuote { get; set; }
    
    public (SeString, SeString) NameTextWrap { get; set; }
    
    public SeString? FreeCompanyLeftQuote { get; set; }
    
    public SeString? FreeCompanyRightQuote { get; set; }
}
