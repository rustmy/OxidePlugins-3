using System.Collections.Generic;
using ProtoBuf;
using Oxide.Core;
using System;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("CupboardInfo", "MJSU", "0.0.1")]
    [Description("Displays the changes to cupboards to the user")]
    // ReSharper disable once UnusedMember.Global
    class CupboardInfo : RustPlugin
    {
        private PluginConfig _pluginConfig;

        private const string UsePermission = "cupboardinfo.use";

        #region Plugin Loading & Initalizing
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugin is being loaded
        /// </summary>
        /// ///////////////////////////////////////////////////////////////
        private void Loaded()
        {
            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig, true);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["NoPermission"] = "You do not have permission to use this command",
                ["Cleared"] = "You have successfully cleared the cupboard list",
                ["Authorized"] = "List of users also authorized on this cupboard:",
                ["StillAuthoried"] = "List of users still authorized on this cupboard:"
            }, this);

            permission.RegisterPermission(UsePermission, this);
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
                Prefix = config?.Prefix ?? "[<color=yellow>Cupboard Info</color>]",
                UsePermission =  config?.UsePermission ?? false
            };
        }
        #endregion

        #region Oxide Hooks
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Called when the player clears a cupboard
        /// </summary>
        /// <param name="privilege"></param>
        /// <param name="player"></param>
        /// ///////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        void OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
        {
            if(CheckPermission(player, UsePermission, false))
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Cleared", player.UserIDString)}");
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Called when a player authorizes on a cupbaord
        /// </summary>
        /// <param name="privilege"></param>
        /// <param name="player"></param>
        /// ///////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        void OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege.authorizedPlayers.Count <= 0) return;
            if (!CheckPermission(player, UsePermission, false)) return;
            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Authorized", player.UserIDString)}");
            DisplayCupboardData(privilege, player);
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Called when a player defaulthorizes on a cupboard
        /// </summary>
        /// <param name="privilege"></param>
        /// <param name="player"></param>
        /// ///////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        void OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege.authorizedPlayers.Count <= 0) return;
            if (!CheckPermission(player, UsePermission, false)) return;
            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("StillAuthoried", player.UserIDString)}");
            DisplayCupboardData(privilege, player);
        }
        #endregion

        #region Display Data
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Displays the cupboard information to the player
        /// </summary>
        /// <param name="privilege"></param>
        /// <param name="player"></param>
        /// ///////////////////////////////////////////////////////////////
        private void DisplayCupboardData(BuildingPrivlidge privilege, BasePlayer player)
        {
            foreach (PlayerNameID user in privilege.authorizedPlayers)
            {
                if (user.userid != player.userID)
                {
                    PrintToChat(player, $" {user.userid} {user.username}");
                }
            }
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
        private string Lang(string key, string id = null, params object[] args)
            => string.Format(lang.GetMessage(key, this, id), args);

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

            if (showText) //player doesn't have permission. Should we show them a no permission message
            {
                PrintToChat(player, $"{Lang(_pluginConfig.Prefix)} {Lang("NoPermission", player.UserIDString)}");
            }

            return false;
        }
        #endregion

        #region Classes
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugins Config
        /// </summary>
        /// ///////////////////////////////////////////////////////////////
        class PluginConfig
        {
            public string Prefix { get; set; }
            public bool UsePermission { get; set; }
        }
        #endregion
    }
}
