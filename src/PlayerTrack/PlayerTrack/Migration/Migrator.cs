#pragma warning disable 612

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Dalamud.DrunkenToad;
using Newtonsoft.Json;

namespace PlayerTrack
{
    /// <summary>
    /// Migrate schema to newer versions.
    /// </summary>
    public static class Migrator
    {
        /// <summary>
        /// Current messages from migration.
        /// </summary>
        // ReSharper disable once CollectionNeverQueried.Global
        public static List<string> Messages = new ();

        private static PlayerTrackPlugin plugin = null!;
        private static bool isJSONCompressed;

        /// <summary>
        /// Migrate schema.
        /// </summary>
        /// <param name="playerTrackPlugin">plugin.</param>
        /// <returns>indicator if migration successful.</returns>
        public static bool Migrate(PlayerTrackPlugin playerTrackPlugin)
        {
            try
            {
                plugin = playerTrackPlugin;
                if (MigrateJSONFormat())
                {
                    plugin.BaseRepository.SetVersion(3);
                    plugin.WindowManager.MigrationWindow.IsOpen = false;
                }
            }
            catch (Exception ex)
            {
                // clear messages and add error
                Messages.Clear();
                PrintAndLog($"Schema migration failed so stopping plugin. Please report on discord.");
                PrintAndLog(ex.Message + " Stack Trace:" + ex.StackTrace);

                // clean up from failed migration attempt
                plugin.PluginService.BackupManager.CreateBackup(
                    "upgrade/v" + plugin.Configuration.PluginVersion + "_failed_");
                File.Delete(plugin.PluginService.PluginFolder() + "/data/data.db");

                return false;
            }

            return true;
        }

        private static bool MigrateJSONFormat()
        {
            // check if json meta data file exists
            if (File.Exists(plugin.PluginService.PluginFolder() + "/data/data.meta"))
            {
                // read meta data file
                Logger.LogInfo("JSON Format detected.");
                var metaDataStr =
                    File.ReadAllText(plugin.PluginService.PluginFolder() + "/data/data.meta");
                if (string.IsNullOrEmpty(metaDataStr)) throw new Exception("Failed to read json meta data.");

                // read data by json version
                plugin.WindowManager.MigrationWindow.IsOpen = true;
                PrintAndLog("Starting schema migration.");
                PrintAndLog("Starting to load original data.");
                var metaData = JsonConvert.DeserializeObject<TrackMetaData>(metaDataStr);
                isJSONCompressed = metaData.Compressed;
                Logger.LogInfo("JSON compressed: " + isJSONCompressed);
                Dictionary<string, TrackPlayer> trackPlayers = new ();
                switch (metaData.SchemaVersion)
                {
                    case 1:
                        Logger.LogInfo("Detected Schema Version: 1");
                        trackPlayers = MigrateV1();
                        break;
                    case 2:
                        Logger.LogInfo("Detected Schema Version: 2");
                        trackPlayers = MigrateV2();
                        break;
                }

                var data = File.ReadAllText(plugin.PluginService.PluginFolder() + "/data/categories.dat");
                var trackCategories = JsonConvert.DeserializeObject<List<TrackCategory>>(data, SerializerUtil.CamelCaseJsonSerializer());
                PrintAndLog("Finished loading original data.");

                // start mapping
                PrintAndLog($"Starting to map data and load into database.");
                var categories = new List<Category>();
                var encounters = new List<Encounter>();
                var players = new List<Player>();

                // map categories
                Category? defaultCategory = null;
                var defaultColor = new Vector4(255, 255, 255, 1);
                for (var i = 0; i < trackCategories.Count; i++)
                {
                    Vector4? color = trackCategories[i].Color;
                    if (color == defaultColor)
                    {
                        color = null;
                    }

                    var category = new Category(trackCategories[i].Id)
                    {
                        Name = trackCategories[i].Name,
                        Icon = trackCategories[i].Icon,
                        ListColor = color,
                        NamePlateColor = color,
                        IsDefault = trackCategories[i].IsDefault,
                        IsAlertEnabled = trackCategories[i].EnableAlerts,
                        Rank = i,
                    };
                    if (category.IsDefault)
                    {
                        defaultCategory = category;
                    }
                    else
                    {
                        categories.Add(category);
                    }
                }

                foreach (var category in categories)
                {
                    plugin.CategoryService.AddCategory(category);
                }

                if (defaultCategory != null)
                {
                    var firstId = plugin.CategoryService.GetCategories().FirstOrDefault(pair => pair.Key == 1).Value?.Id;
                    defaultCategory.Id = firstId == null ? 1 : plugin.CategoryService.NextID();
                    plugin.CategoryService.AddCategory(defaultCategory);
                }

                // map players / encounters
                foreach (var trackPlayer in trackPlayers)
                {
                    encounters.AddRange(trackPlayer.Value.Encounters.Select(trackEncounter => new Encounter
                    {
                        PlayerKey = trackPlayer.Key,
                        Created = trackEncounter.Created,
                        Updated = trackEncounter.Updated,
                        TerritoryType = trackEncounter.Location.TerritoryType,
                        JobId = trackEncounter.Job.Id,
                        JobLvl = trackEncounter.Job.Lvl,
                    }));

                    var player = new Player
                    {
                        Key = trackPlayer.Key,
                        LodestoneId = trackPlayer.Value.Lodestone.Id,
                        LodestoneStatus = (LodestoneStatus)trackPlayer.Value.Lodestone.Status,
                        LodestoneLastUpdated = trackPlayer.Value.Lodestone.LastUpdated,
                        LodestoneFailureCount = trackPlayer.Value.Lodestone.FailureCount,
                        Names = trackPlayer.Value.Names,
                        LastTerritoryType = trackPlayer.Value.Encounters.Last().Location.TerritoryType,
                        Created = trackPlayer.Value.Created,
                        Updated = trackPlayer.Value.Encounters.Last().Updated,
                        SeenCount = trackPlayer.Value.Encounters.Count,
                        Icon = trackPlayer.Value.Icon,
                        ListColor = trackPlayer.Value.Color,
                        NamePlateColor = trackPlayer.Value.Color,
                        Notes = trackPlayer.Value.Notes,
                        SendNextAlert = trackPlayer.Value.Alert.LastSent + plugin.Configuration.AlertFrequency,
                        CategoryId = trackPlayer.Value.CategoryId,
                        IsAlertEnabled = trackPlayer.Value.Alert is { State: TrackAlertState.Enabled },
                        HomeWorlds = new List<KeyValuePair<uint, string>>(),
                    };

                    var contentId = plugin.PluginService.GameData.ContentId(player.LastTerritoryType);
                    player.FreeCompany = Player.DetermineFreeCompany(contentId, trackPlayer.Value.FreeCompany);
                    foreach (var world in trackPlayer.Value.HomeWorlds)
                    {
                        player.HomeWorlds.Add(new KeyValuePair<uint, string>(world.Id, world.Name));
                    }

                    if (player.CategoryId == 0)
                    {
                        if (defaultCategory != null) player.CategoryId = defaultCategory.Id;
                    }

                    plugin.PlayerService.SetDerivedFields(player);
                    players.Add(player);
                }

                // clear placeholder encounters for manually added players
                encounters = encounters.Where(encounter => encounter.JobId != 0).ToList();

                plugin.EncounterService.AddEncounters(encounters);
                plugin.PlayerService.AddPlayers(players);
                PrintAndLog($"Finished mapping data and loading into database.");

                // back up old files
                PrintAndLog("Starting to clean up old files.");
                plugin.PluginService.BackupManager.CreateBackup(
                    "upgrade/v" + plugin.Configuration.PluginVersion + "_");
                File.Delete(plugin.PluginService.PluginFolder() + "/data/categories.dat");
                File.Delete(plugin.PluginService.PluginFolder() + "/data/players.dat");
                File.Delete(plugin.PluginService.PluginFolder() + "/data/data.meta");
                PrintAndLog("Finished cleaning up old files.");

                // return success
                PrintAndLog($"Finished schema migration successfully.");
                return true;
            }

            return false;
        }

        private static Dictionary<string, TrackPlayer> MigrateV1()
        {
            var data = File.ReadAllText(plugin.PluginService.PluginFolder() + "/data/players.dat");
            if (isJSONCompressed) data = data.Decompress();

            var players = JsonConvert.DeserializeObject<Dictionary<string, TrackPlayer>>(
                data,
                SerializerUtil.CamelCaseJsonSerializer());
            return EnrichJSONData(players);
        }

        private static Dictionary<string, TrackPlayer> MigrateV2()
        {
            var data = new List<string> { string.Empty };
            using (var sr = new StreamReader(plugin.PluginService.PluginFolder() + "/data/players.dat"))
            {
                string line;

                // ReSharper disable once AssignNullToNotNullAttribute
                while ((line = sr.ReadLine()) != null) data.Add(line);
            }

            data = data.Where(s => !string.IsNullOrEmpty(s)).ToList();
            var players = new Dictionary<string, TrackPlayer>();
            var serializer = SerializerUtil.CamelCaseJsonSerializer();
            if (data.Count > 0)
            {
                if (isJSONCompressed)
                {
                    foreach (var entry in data)
                    {
                        var player = JsonConvert.DeserializeObject<KeyValuePair<string, TrackPlayer>>(
                            entry.Decompress(), serializer);
                        players.Add(player.Key, player.Value);
                    }
                }
                else
                {
                    foreach (var entry in data)
                    {
                        if (string.IsNullOrEmpty(entry)) continue;
                        var player = JsonConvert.DeserializeObject<KeyValuePair<string, TrackPlayer>>(
                            entry,
                            SerializerUtil.CamelCaseJsonSerializer());
                        players.Add(player.Key, player.Value);
                    }
                }
            }

            return EnrichJSONData(players);
        }

        private static Dictionary<string, TrackPlayer> EnrichJSONData(Dictionary<string, TrackPlayer> players)
        {
            foreach (var playerEntry in players)
            {
                var player = playerEntry.Value;

                // homeworlds
                foreach (var world in player.HomeWorlds)
                {
                    world.Name = plugin.PluginService.GameData.WorldName(world.Id);
                }

                // encounters
                var encounters = player.Encounters.ToList();
                foreach (var encounter in player.Encounters)
                {
                    encounter.Location.PlaceName =
                        plugin.PluginService.GameData.PlaceName(encounter.Location.TerritoryType);
                    encounter.Location.ContentName =
                        plugin.PluginService.GameData.ContentName(
                            plugin.PluginService.GameData.ContentId(encounter.Location.TerritoryType));
                    encounter.Job.Code = plugin.PluginService.GameData.ClassJobCode(encounter.Job.Id);
                }

                player.Encounters = encounters;

                // lodestone status
                if (player.Lodestone.Status != TrackLodestoneStatus.Verified &&
                    player.Lodestone.Status != TrackLodestoneStatus.Failed)
                {
                    player.Lodestone.Status = TrackLodestoneStatus.Unverified;
                }
            }

            return players;
        }

        private static void PrintAndLog(string message)
        {
            Logger.LogInfo(message);
            Messages.Add($"[{DateTime.Now.ToShortTimeString()}] {message}");
        }
    }
}

#pragma warning restore 612
