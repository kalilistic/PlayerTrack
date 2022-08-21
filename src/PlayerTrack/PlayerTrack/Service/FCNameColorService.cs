using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;

using Dalamud.DrunkenToad;

namespace PlayerTrack
{
    /// <summary>
    /// FCNameColor Service.
    /// </summary>
    public class FCNameColorService
    {
        /// <summary>
        /// Indicator if FCNameColor is available.
        /// </summary>
        public bool IsFCNameColorAvailable;
        private readonly FCNameColorConsumer fCNameColorConsumer;
        private readonly PlayerTrackPlugin plugin;
        private readonly Timer syncTimer;
        private readonly Regex localPlayerRegex;
        private readonly Regex freeCompanyRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="FCNameColorService"/> class.
        /// </summary>
        /// <param name="plugin">player track plugin.</param>
        public FCNameColorService(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;
            this.fCNameColorConsumer = new FCNameColorConsumer();
            this.localPlayerRegex = new Regex(@"^(?<playerName>[^@]*)@(?<playerWorldName>[^ ]*) (?<playerId>[0-9]*)", RegexOptions.Compiled);
            this.freeCompanyRegex = new Regex(@"^(?<playerId>[^ ]*) (?<freeCompanyId>[^ ]*) (?<freeCompanyName>[^ ]*)", RegexOptions.Compiled);
            this.syncTimer = new Timer { Interval = this.plugin.Configuration.SyncWithFCNameColorFrequency, Enabled = false };
            this.syncTimer.Elapsed += this.SyncTimerOnElapsed;
            if (this.plugin.Configuration.SyncWithFCNameColor)
            {
                this.IsFCNameColorAvailable = this.fCNameColorConsumer.IsAvailable();
                if (this.IsFCNameColorAvailable)
                {
                    this.SyncWithFCNameColor();
                }
            }
        }

        /// <summary>
        /// Synchronize players FCNameColor.
        /// </summary>
        public void SyncWithFCNameColor()
        {
            var newCategoryCreated = false;
            if (!this.IsFCNameColorAvailable || !this.plugin.Configuration.SyncWithFCNameColor) return;
            try
            {
                var freeCompanies = this.GetFreeCompanies();
                var defaultCategoryId = this.plugin.CategoryService.GetDefaultCategory().Id;

                // add new free companies and free company members
                foreach (var freeCompany in freeCompanies)
                {
                    var existingCategory = true;
                    int? categoryId = null;
                    if (this.plugin.Configuration.CreateDynamicFCCategories)
                    {
                        categoryId = this.plugin.CategoryService.GetCategoryIdByFCLodestoneId(freeCompany.FreeCompanyLodestoneId);
                        if (categoryId is null or 0)
                        {
                            existingCategory = false;
                            newCategoryCreated = true;
                            categoryId = this.plugin.CategoryService.AddCategory();
                            var category = this.plugin.CategoryService.GetCategory((int)categoryId);
                            category.Name = freeCompany.FreeCompanyName;
                            category.FCLodestoneId = freeCompany.FreeCompanyLodestoneId;
                            this.plugin.CategoryService.SaveCategory(category);
                        }
                    }

                    foreach (var fcMember in freeCompany.FreeCompanyMembers)
                    {
                        var player = this.plugin.PlayerService.GetPlayerByLodestoneId(fcMember.LodestoneId);

                        if (!this.plugin.Configuration.CreateDynamicFCCategories && player == null)
                        {
                            this.plugin.PlayerService.AddPlayer(
                                fcMember.Name,
                                fcMember.HomeWorldId,
                                fcMember.LodestoneId,
                                defaultCategoryId,
                                freeCompany.FreeCompanyName);
                        }
                        else if (this.plugin.Configuration.CreateDynamicFCCategories && existingCategory)
                        {
                            if (player == null)
                            {
                                this.plugin.PlayerService.AddPlayer(
                                    fcMember.Name,
                                    fcMember.HomeWorldId,
                                    fcMember.LodestoneId,
                                    (int)categoryId!,
                                    freeCompany.FreeCompanyName);
                            }
                            else if (player.CategoryId != (int)categoryId! &&
                                     !(!this.plugin.Configuration.ReassignPlayersFromExistingCategory &&
                                       player.CategoryId != defaultCategoryId))
                            {
                                player.CategoryId = (int)categoryId!;
                                this.plugin.PlayerService.UpdatePlayerCategory(player, false);
                            }
                        }
                    }
                }

                // Rerun to add new members if new categories created
                if (newCategoryCreated)
                {
                    this.SyncWithFCNameColor();
                    this.plugin.PlayerService.ResetViewPlayers();
                }

                // Remove players no longer in FC (put them into default category)
                if (this.plugin.Configuration.CreateDynamicFCCategories)
                {
                    var fcCategories = this.plugin.CategoryService.GetCategories()
                                           .Where(pair => !string.IsNullOrEmpty(pair.Value.FCLodestoneId));
                    foreach (var (key, value) in fcCategories)
                    {
                        var freeCompany = freeCompanies.FirstOrDefault(company => company.FreeCompanyLodestoneId.Equals(value.FCLodestoneId));
                        if (freeCompany == null) continue;
                        var freeCompanyMembers = freeCompany.FreeCompanyMembers;
                        var playersInFC = this.plugin.PlayerService.GetPlayers()?
                                          .Where(pair => pair.Value.CategoryId == key);
                        if (playersInFC == null) continue;
                        foreach (var (_, player) in playersInFC)
                        {
                            if (!freeCompanyMembers.Any(member => member.LodestoneId.Equals(player.LodestoneId)))
                            {
                                player.CategoryId = defaultCategoryId;
                                this.plugin.PlayerService.UpdatePlayerCategory(player, false);
                            }
                        }
                    }
                }

                // Remove players from ignore list
                var ignoreList = this.fCNameColorConsumer.GetIgnoredPlayers();
                var ignoreListIds = new List<uint>();
                foreach (var ignored in ignoreList)
                {
                    var parts = ignored.Split(" ");
                    if (parts.Length != 3) continue;
                    var lodestoneId = Convert.ToUInt32(parts[0]);
                    var player = this.plugin.PlayerService.GetPlayerByLodestoneId(lodestoneId);
                    if (player == null || !this.plugin.PlayerService.GetPlayerOverrideFCNameColor(player))
                    {
                        this.fCNameColorConsumer.RemovePlayerFromIgnoredPlayers(parts[1] + " " + parts[2]);
                    }
                    else
                    {
                        ignoreListIds.Add(lodestoneId);
                    }
                }

                // Add players to ignore list
                var ignoredPlayers = this.plugin.PlayerService.GetPlayers(true);
                foreach (var (_, value) in ignoredPlayers)
                {
                    if (!ignoreListIds.Contains(value.LodestoneId))
                    {
                        this.fCNameColorConsumer.AddPlayerToIgnoredPlayers(value.LodestoneId.ToString(), value.Names.First());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to sync with FCNameColor.");
                this.IsFCNameColorAvailable = this.fCNameColorConsumer.IsAvailable();
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

        private void SyncTimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!this.plugin.Configuration.SyncWithVisibility) return;
            var newStatus = this.fCNameColorConsumer.IsAvailable();

            // do full sync if previously off
            if (!this.IsFCNameColorAvailable && newStatus)
            {
                this.SyncWithFCNameColor();
            }

            this.IsFCNameColorAvailable = newStatus;
        }

        private List<FreeCompany> GetFreeCompanies()
        {
            var freeCompanies = new List<FreeCompany>();

            // get local players
            var localPlayers = this.fCNameColorConsumer.GetLocalPlayers();
            foreach (var localPlayer in localPlayers)
            {
                var match = this.localPlayerRegex.Match(localPlayer);
                if (!match.Success) continue;
                freeCompanies.Add(new FreeCompany
                {
                    LocalPlayerLodestoneId = match.Groups["playerId"].Value,
                    HomeWorldId = (ushort)PlayerTrackPlugin.DataManager.WorldId(match.Groups["playerWorldName"].Value),
                });
            }

            // get free companies
            var playerFCs = this.fCNameColorConsumer.GetPlayerFCs();
            foreach (var playerFC in playerFCs)
            {
                var match = this.freeCompanyRegex.Match(playerFC);
                if (!match.Success) continue;
                var freeCompany = freeCompanies.FirstOrDefault(company => company.LocalPlayerLodestoneId.Equals(match.Groups["playerId"].Value));
                if (freeCompany == null) continue;
                freeCompany.FreeCompanyLodestoneId = match.Groups["freeCompanyId"].Value;
                freeCompany.FreeCompanyName = match.Groups["freeCompanyName"].Value;
            }

            // remove any FCs without IDs
            freeCompanies = freeCompanies.Where(company => !string.IsNullOrEmpty(company.FreeCompanyLodestoneId)).ToList();

            // get free company members
            foreach (var freeCompany in freeCompanies)
            {
                var fcMembers = this.fCNameColorConsumer.GetFCMembers(freeCompany.FreeCompanyLodestoneId);
                foreach (var fcMember in fcMembers)
                {
                    var parts = fcMember.Split(" ");
                    if (parts.Length != 3) continue;
                    freeCompany.FreeCompanyMembers.Add(new FreeCompanyMember
                    {
                        LodestoneId = Convert.ToUInt32(parts[0]),
                        Name = string.Concat(parts[1], " ", parts[2]),
                        HomeWorldId = freeCompany.HomeWorldId,
                    });
                }
            }

            // remove any FCs without members
            freeCompanies = freeCompanies.Where(company => company.FreeCompanyMembers.Count > 0).ToList();

            return freeCompanies;
        }
    }
}
