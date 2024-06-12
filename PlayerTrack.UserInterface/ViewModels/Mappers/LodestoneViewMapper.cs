using System.Collections.Generic;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Helpers;
using Dalamud.Interface;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Helpers;

namespace PlayerTrack.UserInterface.ViewModels.Mappers;

public static class LodestoneViewMapper
{
    
    public static LodestoneServiceView MapLookups(bool hideHistory)
    {
        var lookups = PlayerLodestoneService.GetLodestoneLookups();
        
        var serviceView = new LodestoneServiceView
        {
            LastRefreshed = UnixTimestampHelper.CurrentTime().ToTimeSpan(),
            LodestoneLookups = new List<LodestoneLookupView>()
        };
        
        foreach (var lookup in lookups)
        {
            if (hideHistory && lookup.IsDone) continue;
            var view = new LodestoneLookupView
            {
                Id = lookup.Id,
                RequestPlayer = $"{lookup.PlayerName}@{DalamudContext.DataManager.GetWorldNameById(lookup.WorldId)}",
                Status = lookup.LodestoneStatus.ToString(),
                Created = lookup.Created.ToTimeSpan(),
                Updated = lookup.Updated.ToTimeSpan(),
                Color = ColorHelper.GetColorByStatus(lookup.LodestoneStatus),
                LodestoneId = lookup.LodestoneId,
                ShowLodestoneButton = lookup.LodestoneStatus == LodestoneStatus.Verified,
                TypeIcon = lookup.LodestoneLookupType == LodestoneLookupType.Batch
                    ? FontAwesomeIcon.PeopleGroup.ToIconString()
                    : FontAwesomeIcon.Redo.ToIconString()
            };

            if (lookup.LodestoneLookupType == LodestoneLookupType.Batch)
            {
                view.ResponsePlayer = view.RequestPlayer;
            }
            else if (!string.IsNullOrEmpty(lookup.UpdatedPlayerName) && lookup.UpdatedWorldId > 0)
            {
                view.ResponsePlayer =
                    $"{lookup.UpdatedPlayerName}@{DalamudContext.DataManager.GetWorldNameById(lookup.UpdatedWorldId)}";
                if (!lookup.PlayerName.Equals(lookup.UpdatedPlayerName) || lookup.WorldId != lookup.UpdatedWorldId)
                {
                    view.hasNameWorldChanged = true;
                }
            }
            else
            {
                view.ResponsePlayer = DalamudContext.LocManager.GetString("NotAvailable");
            }

            switch (lookup.LodestoneStatus)
            {
                case LodestoneStatus.Unverified:
                    view.NextAttemptDisplay = DalamudContext.LocManager.GetString("NextLookup");
                    view.Rank = 1;
                    view.NextAttempt = 0;
                    break;
                case LodestoneStatus.Failed:
                    var retry = LodestoneService.GetMillisecondsUntilRetry(lookup.Updated);
                    view.NextAttemptDisplay = retry.ToDuration();
                    view.Rank = 2;
                    view.NextAttempt = retry;
                    break;
                case LodestoneStatus.Verified:
                case LodestoneStatus.Banned:
                case LodestoneStatus.NotApplicable:
                case LodestoneStatus.Blocked:
                case LodestoneStatus.Cancelled:
                case LodestoneStatus.Unavailable:
                case LodestoneStatus.Invalid:
                default:
                    view.NextAttemptDisplay = DalamudContext.LocManager.GetString("NotApplicable");
                    view.Rank = 3;
                    view.NextAttempt = 0;
                    break;
            }

            if (lookup.LodestoneStatus is LodestoneStatus.Unverified or LodestoneStatus.Failed)
            {
                serviceView.InQueue++;
            }
            
            serviceView.LodestoneLookups.Add(view);
            
        }
        
        serviceView.LodestoneLookups.Sort((a, b) =>
        {
            var rankComparison = a.Rank.CompareTo(b.Rank);
            return rankComparison != 0 ? rankComparison : a.NextAttempt.CompareTo(b.NextAttempt);
        });
        
        serviceView.RefreshStatus();
        return serviceView;
    }
}
