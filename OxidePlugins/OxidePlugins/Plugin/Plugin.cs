using Oxide.Core;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Plugin", "MJSU", "0.0.1")]
    [Description("Is a plugin")]
    class Plugin : RustPlugin
    {
        #region Class Fields
        private StoredData _storedData; //Plugin Data
        private PluginConfig _pluginConfig; //Plugin Config

        private const string UsePermission = "plugin.use";
        #endregion

        #region Setup & Loading
        private void Loaded()
        {
            LoadVersionedConfig();
            LoadDataFile();
            LoadLang();

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
                ["NoPermission"] = "You do not have permission to use this command"
            }, this);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Used to load a versioned config
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void LoadVersionedConfig()
        {
            try
            {
                _pluginConfig = Config.ReadObject<PluginConfig>();

                if (_pluginConfig.ConfigVersion == null)
                {
                    PrintWarning("Config failed to load correctly. Backing up to AutoCodeLock.error.json and using default config");
                    Config.WriteObject(_pluginConfig, true, Interface.Oxide.ConfigDirectory + "/AutoCodeLock.error.json");
                    _pluginConfig = DefaultConfig();
                }
            }
            catch
            {
                _pluginConfig = DefaultConfig();
            }

            Config.WriteObject(_pluginConfig, true);
        }

        private void LoadDataFile()
        {
            try
            {
                _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("Plugin");
            }
            catch
            {
                PrintWarning("Data File could not be loaded. Creating new File");
                _storedData = new StoredData();
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// load the plugins config
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
            Config.WriteObject(DefaultConfig(), true);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Default config for this plugin
        /// </summary>
        /// <returns></returns>
        /// ////////////////////////////////////////////////////////////////////////
        private PluginConfig DefaultConfig()
        {
            return new PluginConfig
            {
                Prefix = "[<color=yellow>Plugin</color>]",
                UsePermission = false,
                ConfigVersion = Version.ToString()
            };
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

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
        #endregion

        #region Classes
        class PluginConfig
        {
            public string Prefix {get; set; }
            public bool UsePermission {get; set; }
            public string ConfigVersion {get; set; }
        }

        class StoredData
        {

        }
        #endregion
    }
}
