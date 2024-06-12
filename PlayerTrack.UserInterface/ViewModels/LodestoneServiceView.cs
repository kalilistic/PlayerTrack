using System.Collections.Generic;
using System.Numerics;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Helpers;

namespace PlayerTrack.UserInterface.ViewModels;

public class LodestoneServiceView
{
    public string LastRefreshed { get; set; } = string.Empty;
    public int InQueue { get; set; }
    public LodestoneServiceStatus ServiceStatus { get; set; }
    public Vector4 ServiceStatusColor { get; set; }
    public List<LodestoneLookupView> LodestoneLookups { get; set; } = new();

    public void RefreshStatus()
    {
        var serviceStatus = ServiceContext.LodestoneService.GetServiceStatus();
        ServiceStatus = serviceStatus;
        ServiceStatusColor = ColorHelper.GetColorByStatus(serviceStatus);
    }
}