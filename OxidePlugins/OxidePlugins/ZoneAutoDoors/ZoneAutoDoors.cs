using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("ZoneAutoDoors", "MJSU", "0.0.1")]
    [Description("Force autodoors in set zones")]
    class ZoneAutoDoors : RustPlugin
    {
        [PluginReference]
        Plugin ZoneManager;

        #region Class Fields
        private StoredData _storedData; //Plugin Data
        private PluginConfig _pluginConfig; //Plugin Config

        private const string UsePermission = "ZoneAutoDoors.use";
        #endregion

        #region Setup & Loading
        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            LoadLang();

            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig, true);

            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("ZoneAutoDoors");

            permission.RegisterPermission(UsePermission, this);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Register the lang messages
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void LoadLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command",
                ["InvalidSytax"] = "Invalid Sytax (/zad add/remove ZoneId seconds)",
                ["InvalidSeconds"] = "The time you set of {0} is not valid",
                ["Added"] = "You have added zone {0} with a delay of {1}",
                ["Removed"] = "You have removed zone {0}"
            }, this);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// load the default config for this plugin
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(ConfigOrDefault(null), true);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Uses the values passed in from config. If any values are null it updates them with default values
        /// </summary>
        /// <param name="config">Config that has been loaded or null</param>
        /// <returns>Config using values passed in from config default values</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private PluginConfig ConfigOrDefault(PluginConfig config)
        {
            return new PluginConfig
            {
                Prefix = config?.Prefix ?? "[<color=yellow>ZoneAutoDoors</color>]",
                UsePermission = config?.UsePermission ?? false,
            };
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            if (ZoneManager == null)
            {
                PrintWarning("ZoneAutoDoors could not find ZoneManager. ZoneAutoDoors will not work");
            }
        }
        #endregion

        #region Chat Command
        [ChatCommand("zad")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void ZoneAutoDoorsChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin() && !CheckPermission(player, UsePermission, true)) return;
            if (args.Length < 1)
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("InvalidSyntax", player.UserIDString)}");
                return;
            }

            switch (args[0].ToLower())
            {
                case "add":
                    if (args.Length != 3)
                    {
                        PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("InvalidSyntax", player.UserIDString)}");
                        return;
                    }

                    float time;
                    if (!float.TryParse(args[2], out time))
                    {
                        PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("InvalidSeconds", player.UserIDString, args[2])}");
                        return;
                    }

                    _storedData.ZoneTimes[args[1]] = time;
                    PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Added", player.UserIDString, args[1], args[2])}");
                    break;

                case "remove":
                    if (args.Length != 2)
                    {
                        PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("InvalidSyntax", player.UserIDString)}");
                        return;
                    }
                    _storedData.ZoneTimes.Remove(args[1]);
                    PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Removed", player.UserIDString, args[1])}");
                    break;

                default:
                    PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("InvalidSyntax", player.UserIDString)}");
                    break;
            }

            Interface.Oxide.DataFileSystem.WriteObject("ZoneAutoDoors", _storedData);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDoorOpened(Door door, BasePlayer player)
        {
            if (door == null || !door.IsOpen() || door.LookupPrefab().name.Contains("shutter")) return;

            float time = -1;
            foreach (KeyValuePair<string, float> zone in _storedData.ZoneTimes)
            {
                if (ZoneManager?.Call<bool>("isPlayerInZone", zone.Key, player) ?? false)
                {
                    time = zone.Value;
                    break;
                }
            }

            if ((int)time == -1) return;

            timer.Once(time, () =>
            {
                if (!door || !door.IsOpen()) return;

                door.SetFlag(BaseEntity.Flags.Open, false);
                door.SendNetworkUpdateImmediate();
            });
        }
        #endregion

        #region Helper Methods
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// @credit to Exel80 - code from EasyVote
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        ///  //////////////////////////////////////////////////////////////////////////////////////
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Checks if the user has the given permissions. Displays an error to the user if ShowText is true
        /// </summary>
        /// <param name="player">Player to be checked</param>
        /// <param name="perm">Permission to check for</param>
        /// <param name="showText">Should display no permission</param>
        /// <returns></returns>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private bool CheckPermission(BasePlayer player, string perm, bool showText)
        {
            if (!_pluginConfig.UsePermission || permission.UserHasPermission(player.UserIDString, perm))
            {
                return true;
            }
            else if (showText) //player doesn't have permission. Should we show them a no permission message
            {
                PrintToChat(player, $"{Lang(_pluginConfig.Prefix)} {Lang("NoPermission", player.UserIDString)}");
            }

            return false;
        }
        #endregion

        #region Classes
        class PluginConfig
        {
            public string Prefix { get; set; }
            public bool UsePermission { get; set; }
        }

        class StoredData
        {
            public Hash<string, float> ZoneTimes = new Hash<string, float>();
        }
        #endregion
    }
}
