using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using Dalamud.DrunkenToad;

namespace PlayerTrack
{
    /// <summary>
    /// Visibility Service.
    /// </summary>
    public class VisibilityService
    {
        /// <summary>
        /// Indicator if visibility is available.
        /// </summary>
        public bool IsVisibilityAvailable;
        private const string Reason = "PlayerTrack";
        private readonly VisibilityConsumer visibilityConsumer;
        private readonly PlayerTrackPlugin plugin;
        private readonly Timer syncTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisibilityService"/> class.
        /// </summary>
        /// <param name="plugin">player track plugin.</param>
        public VisibilityService(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;
            this.visibilityConsumer = new VisibilityConsumer();
            this.syncTimer = new Timer { Interval = this.plugin.Configuration.SyncWithVisibilityFrequency, Enabled = false };
            this.syncTimer.Elapsed += this.SyncTimerOnElapsed;
            if (this.plugin.Configuration.SyncWithVisibility)
            {
                this.IsVisibilityAvailable = this.visibilityConsumer.IsAvailable();
                if (this.IsVisibilityAvailable)
                {
                    this.SyncWithVisibility();
                }
            }
        }

        /// <summary>
        /// Dispose service.
        /// </summary>
        public void Dispose()
        {
            this.syncTimer.Enabled = false;
            this.syncTimer.Dispose();
        }

        /// <summary>
        /// Start service.
        /// </summary>
        public void Start()
        {
            this.syncTimer.Enabled = true;
        }

        /// <summary>
        /// Synchronize players hidden state with Visibility.
        /// </summary>
        public void SyncWithVisibility()
        {
            if (!this.IsVisibilityAvailable || !this.plugin.Configuration.SyncWithVisibility) return;
            try
            {
                Logger.LogDebug("Starting visibility sync.");

                // player track lists
                var players = this.plugin.PlayerService.GetPlayers(VisibilityType.none);
                var voidedPlayers = this.plugin.PlayerService.GetPlayers(VisibilityType.voidlist);
                var whiteListedPlayers = this.plugin.PlayerService.GetPlayers(VisibilityType.whitelist);

                // remove players from void list
                var voidList = this.GetVisibilityPlayers(VisibilityType.voidlist);
                foreach (var (key, value) in voidList)
                {
                    if (!voidedPlayers.ContainsKey(key) && IsSyncedEntry(value.Reason))
                    {
                        this.visibilityConsumer.RemoveFromVoidList(value.Name, value.HomeWorldId);
                    }
                }

                // remove players from white list
                var whiteList = this.GetVisibilityPlayers(VisibilityType.whitelist);
                foreach (var (key, value) in whiteList)
                {
                    if (!whiteListedPlayers.ContainsKey(key) && IsSyncedEntry(value.Reason))
                    {
                        this.visibilityConsumer.RemoveFromWhiteList(value.Name, value.HomeWorldId);
                    }
                }

                // add players to void list
                voidList = this.GetVisibilityPlayers(VisibilityType.voidlist);
                foreach (var (key, value) in voidedPlayers)
                {
                    if (!voidList.ContainsKey(key))
                    {
                        this.visibilityConsumer.AddToVoidList(value.Names.First(), value.HomeWorlds.First().Key, Reason);
                    }
                }

                // add players to white list
                whiteList = this.GetVisibilityPlayers(VisibilityType.whitelist);
                foreach (var (key, value) in whiteListedPlayers)
                {
                    if (!whiteList.ContainsKey(key))
                    {
                        this.visibilityConsumer.AddToWhiteList(value.Names.First(), value.HomeWorlds.First().Key, Reason);
                    }
                }

                // add void list entries to ptrack
                voidList = this.GetVisibilityPlayers(VisibilityType.voidlist);
                foreach (var (key, value) in voidList)
                {
                    if (!players.ContainsKey(key))
                    {
                        this.plugin.PlayerService.AddPlayer(value.Name, (ushort)value.HomeWorldId, VisibilityType.voidlist);
                    }
                    else
                    {
                        var player = players[key];
                        var categoryVisibilityType = this.plugin.CategoryService.GetCategory(player.CategoryId).VisibilityType;
                        if (categoryVisibilityType == VisibilityType.none)
                        {
                            player.VisibilityType = VisibilityType.voidlist;
                            this.plugin.PlayerService.UpdatePlayerVisibilityType(player, false);
                        }
                    }
                }

                // add white list entries to ptrack
                whiteList = this.GetVisibilityPlayers(VisibilityType.whitelist);
                foreach (var (key, value) in whiteList)
                {
                    if (!players.ContainsKey(key))
                    {
                        this.plugin.PlayerService.AddPlayer(value.Name, (ushort)value.HomeWorldId, VisibilityType.whitelist);
                    }
                    else
                    {
                        var player = players[key];
                        var categoryVisibilityType = this.plugin.CategoryService.GetCategory(player.CategoryId).VisibilityType;
                        if (categoryVisibilityType == VisibilityType.none)
                        {
                            player.VisibilityType = VisibilityType.whitelist;
                            this.plugin.PlayerService.UpdatePlayerVisibilityType(player, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to sync with visibility.");
                this.IsVisibilityAvailable = this.visibilityConsumer.IsAvailable();
            }
        }

        /// <summary>
        /// Synchronize player hidden state with Visibility.
        /// </summary>
        /// <param name="player">player to update.</param>
        /// <exception cref="ArgumentOutOfRangeException">unrecognized visibility type.</exception>
        public void SyncPlayerWithVisibility(Player player)
        {
            if (!this.IsVisibilityAvailable || !this.plugin.Configuration.SyncWithVisibility) return;
            try
            {
                Logger.LogDebug($"Starting player visibility sync for {player.Names.First()}.");

                // visibility lists
                var voidList = this.GetVisibilityPlayers(VisibilityType.voidlist);
                var whiteList = this.GetVisibilityPlayers(VisibilityType.whitelist);

                switch (player.VisibilityType)
                {
                    case VisibilityType.none:
                        // remove player from void list
                        if (voidList.ContainsKey(player.Key) && IsSyncedEntry(voidList[player.Key].Reason))
                        {
                            this.visibilityConsumer.RemoveFromVoidList(voidList[player.Key].Name, voidList[player.Key].HomeWorldId);
                        }

                        // remove player from white list
                        if (whiteList.ContainsKey(player.Key) && IsSyncedEntry(whiteList[player.Key].Reason))
                        {
                            this.visibilityConsumer.RemoveFromWhiteList(whiteList[player.Key].Name, whiteList[player.Key].HomeWorldId);
                        }

                        break;
                    case VisibilityType.voidlist:
                        // remove player from white list
                        if (whiteList.ContainsKey(player.Key) && IsSyncedEntry(whiteList[player.Key].Reason))
                        {
                            this.visibilityConsumer.RemoveFromWhiteList(whiteList[player.Key].Name, whiteList[player.Key].HomeWorldId);
                        }

                        // add player to void list
                        if (!voidList.ContainsKey(player.Key))
                        {
                            this.visibilityConsumer.AddToVoidList(player.Names.First(), player.HomeWorlds.First().Key, Reason);
                        }

                        break;
                    case VisibilityType.whitelist:
                        // remove player from void list
                        if (voidList.ContainsKey(player.Key) && IsSyncedEntry(voidList[player.Key].Reason))
                        {
                            this.visibilityConsumer.RemoveFromVoidList(voidList[player.Key].Name, voidList[player.Key].HomeWorldId);
                        }

                        // add player to white list
                        if (!whiteList.ContainsKey(player.Key))
                        {
                            this.visibilityConsumer.AddToWhiteList(player.Names.First(), player.HomeWorlds.First().Key, Reason);
                        }

                        break;
                    default:
                        Logger.LogError("Unrecognized visibility type.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to sync with visibility.");
                this.IsVisibilityAvailable = this.visibilityConsumer.IsAvailable();
            }
        }

        private static bool IsSyncedEntry(string reason)
        {
            return reason.Equals(Reason, StringComparison.OrdinalIgnoreCase);
        }

        private void SyncTimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!this.plugin.Configuration.SyncWithVisibility) return;
            var newStatus = this.visibilityConsumer.IsAvailable();

            // do full sync if previously off
            if (!this.IsVisibilityAvailable && newStatus)
            {
                this.SyncWithVisibility();
            }

            this.IsVisibilityAvailable = newStatus;
        }

        private Dictionary<string, VisibilityEntry> GetVisibilityPlayers(VisibilityType visibilityType)
        {
            List<string> rawVisibilityEntries;
            switch (visibilityType)
            {
                case VisibilityType.none:
                    return new Dictionary<string, VisibilityEntry>();
                case VisibilityType.voidlist:
                    rawVisibilityEntries = this.visibilityConsumer.GetVoidListEntries().ToList();
                    break;
                case VisibilityType.whitelist:
                    rawVisibilityEntries = this.visibilityConsumer.GetWhiteListEntries().ToList();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visibilityType), visibilityType, null);
            }

            Dictionary<string, VisibilityEntry> visibilityEntries = new();
            if (!rawVisibilityEntries.Any()) return visibilityEntries;
            foreach (var voidListEntry in rawVisibilityEntries)
            {
                try
                {
                    var parts = voidListEntry.Split(" ");
                    if (parts.Length != 4) continue;
                    var visibilityEntry = new VisibilityEntry
                    {
                        Name = string.Concat(parts[0], " ", parts[1]),
                        HomeWorldId = Convert.ToUInt32(parts[2]),
                        Reason = parts[3],
                    };
                    visibilityEntry.Key = PlayerService.BuildPlayerKey(visibilityEntry.Name, visibilityEntry.HomeWorldId);
                    visibilityEntries.Add(visibilityEntry.Key, visibilityEntry);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to load visibility entry.");
                }
            }

            return visibilityEntries;
        }
    }
}
