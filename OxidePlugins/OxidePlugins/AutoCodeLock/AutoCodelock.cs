using System.Collections.Generic;
using System.Reflection;
using Oxide.Core;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    // ReSharper disable once UnusedMember.Global
    [Info("AutoCodeLock", "MJSU", "0.0.1")]
    [Description("Auto adds a codelock to a placed door and set the code")]
    class AutoCodeLock : RustPlugin
    {
        private readonly FieldInfo _codelock = typeof(CodeLock).GetField("code", BindingFlags.NonPublic | BindingFlags.Instance);
        readonly FieldInfo _whitelistField = typeof(CodeLock).GetField("whitelistPlayers", BindingFlags.Instance | BindingFlags.NonPublic);

        private StoredData _storedData;
        private PluginConfig _pluginConfig; //Config File

        private const string _usePermission = "autocodelock.use";
        private bool _serverInitialized = false;

        // ReSharper disable once UnusedMember.Local
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugin has been loaded
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void Loaded()
        {
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoCodes");

            permission.RegisterPermission(_usePermission, this);
            LoadVersionedConfig();

            LoadLang();
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
                ["NoPermission"] = "You do not have permission to use that command"
            }, this);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Used to load a versioned config
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void LoadVersionedConfig()
        {
            _pluginConfig = Config.ReadObject<PluginConfig>();
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// load the default config for this plugin
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        protected override void LoadDefaultConfig()
        {
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
                Prefix = "[<color=yellow>Auto CodeLock</color>]",
                UsePermission = false,
                UseCost = true,
                UseItemCost = true,
                ItemCostList = new List<Hash<string, int>> { new Hash<string, int> { ["lock.code"] = 1 }, new Hash<string, int> { ["wood"] = 400, ["metal.fragments"] = 100 } },
                ConfigVersion = Version.ToString()
            };
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Used to mark when the server has finished loading
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void OnServerInitialized()
        {
            _serverInitialized = true;
        }


        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Chat command to set / remove the players codelock code
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// ////////////////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        [ChatCommand("ac")]
        private void SurveyInfoChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!CheckPermission(player, _usePermission, true)) return;

            switch (args.Length)
            {
                case 0:
                    _storedData.PlayerCodes[player.userID] = null;
                    PrintToChat(player, $"{_pluginConfig.Prefix} You have disabled AutoCode\n To set again type /ac code");
                    break;

                case 1:
                    if (!ValidCode(args[0]))
                    {
                        PrintToChat(player, $"{_pluginConfig.Prefix} Your code '{args[0]}' is not valid");
                        return;
                    }
                    _storedData.PlayerCodes[player.userID] = ParseToSaveFormat(args[0]);
                    PrintToChat(player, $"{_pluginConfig.Prefix} You have set your codelock code to {args[0]}");
                    break;

                default:
                    PrintToChat(player, $"{_pluginConfig.Prefix} To set your codelock code type /ac 1234");
                    break;
            }

            Interface.Oxide.DataFileSystem.WriteObject("AutoCodes", _storedData);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Oxide hook to detect when a door is placed
        /// </summary>
        /// <param name="entity"></param>
        /// ////////////////////////////////////////////////////////////////////////
        /// // ReSharper disable once UnusedMember.Local
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!_serverInitialized) return; //Server has not finished starting yet. Used to prevent this code from running when the server is starting up

            Door door = entity as Door;
            if (door == null) return; //Entity spawned is not a door

            BasePlayer player = BasePlayer.FindByID(door.OwnerID);
            if (player == null) return; //Failed to get the owner of the door

            if (_storedData.PlayerCodes[player.userID] == null) return; //Player has not set their autocodelock code yet

            int index;
            if (!CanAfford(player, out index)) //Player cannot afford
            {
                PrintToChat(player, "You can not afford to use AutoCodelock");
                return;
            }

            if (index != -1) //The code lock costs items to place
            {
                TakeCost(player, index);
            }

            AddLockToDoor(player, door); //Add the lock to the door
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Checks if the player can afford to have the codelock added
        /// </summary>
        /// <param name="player">player to check cost for</param>
        /// <param name="costIndex">the index of the list of costs the player can afford</param>
        /// <returns>true if the player can afford, false otherwise</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private bool CanAfford(BasePlayer player, out int costIndex)
        {
            costIndex = -1;
            if (!_pluginConfig.UseCost) return true; //Plugin doesn't use cost

            if (_pluginConfig.UseItemCost) //Plugin uses cost
            {
                bool canAfford = true; //Determines if the player can afford

                for (int index = 0; index < _pluginConfig.ItemCostList.Count; index++)
                {
                    canAfford = true; //Set to true every look
                    foreach (KeyValuePair<string, int> item in _pluginConfig.ItemCostList[index])
                    {
                        Item playerItem = player.inventory.FindItemID(item.Key);

                        if (playerItem == null || playerItem.amount < item.Value) //If the player cannot afford
                        {
                            canAfford = false; 
                            break; //break out of this loop
                        }
                    }

                    if (canAfford) //Made it through a group of costs and has the requirments
                    {
                        costIndex = index; //index the player can afford
                        break; //break out of the outer loop
                    }
                }

                if (!canAfford) return false; //Player could not afford the items
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Take the cost of the CodeLock from the player
        /// </summary>
        /// <param name="player">player to take the coset from</param>
        /// <param name="costIndex">index in the item list the player can afford and to take the items from</param>
        /// ////////////////////////////////////////////////////////////////////////
        private void TakeCost(BasePlayer player, int costIndex)
        {
            if (!_pluginConfig.UseCost) return; //Plugin is not using cost

            if (_pluginConfig.UseItemCost) //Plugin is using items
            {
                Hash<string,int> items = _pluginConfig.ItemCostList[costIndex]; //Hash of the items the player can afford
                List<Item> collection = new List<Item>();

                foreach (KeyValuePair<string, int> item in items) //Loop over the items and collect them from the player
                {
                    player.inventory.Take(collection, ItemManager.FindItemDefinition(item.Key).itemid, item.Value);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Create a CodeLock add the players code to it and add them to the whitelist. Then place the codelock on the door
        /// </summary>
        /// <param name="player">player who placed the door</param>
        /// <param name="door">the door the codelock is going to be attached too</param>
        /// ////////////////////////////////////////////////////////////////////////
        private void AddLockToDoor(BasePlayer player, Door door)
        {
            //Create a CodeLock
            BaseEntity lockentity = GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab", Vector3.zero, new Quaternion());

            lockentity.OnDeployed(door);

            //Add the player to the codelock whitelist
            List<ulong> whitelist = (List<ulong>)_whitelistField.GetValue(lockentity);
            whitelist.Add(player.userID);
            _whitelistField.SetValue(lockentity, whitelist);

            //Retreive the code for the player and set it on the codelock
            string code = SaveFormatToCode(_storedData.PlayerCodes[player.userID]);
            if (!string.IsNullOrEmpty(code))
            {
                CodeLock @lock = lockentity.GetComponent<CodeLock>();
                _codelock.SetValue(@lock, code);
                @lock.SetFlag(BaseEntity.Flags.Locked, true);
            }

            //Add the codelock to the door
            if (!lockentity) return;
            lockentity.gameObject.Identity();
            lockentity.SetParent(door, "lock");
            lockentity.Spawn();
            door.SetSlot(BaseEntity.Slot.Lock, lockentity);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Makes sure the code entered by the player is a valid CodeLock code
        /// </summary>
        /// <param name="code">code passed in by the player</param>
        /// <returns>true if the code is valid, false otherwise</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private bool ValidCode(string code)
        {
            if (code.Length != 4) return false; //Code can only be length of 4
            int codeNum;
            if (!int.TryParse(code, out codeNum)) return false; //try to parse the code to an int
            if (codeNum < 0) return false; //make sure the code is not negative
            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Converts the code to the save format. Use to make it hard for server owners to see players codes
        /// </summary>
        /// <param name="code">code to be converted</param>
        /// <returns>code in the save format</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private string ParseToSaveFormat(string code)
        {
            return string.Format("{0:X}", code);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// converts the code from the save format to the code for the player
        /// </summary>
        /// <param name="code">encoded code</param>
        /// <returns>code in string format</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private string SaveFormatToCode(string code)
        {
            int val;
            if(int.TryParse(code , System.Globalization.NumberStyles.HexNumber, null, out val))
            {
                return val.ToString();
            }
            return null;
        }

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

        // ReSharper disable once ClassNeverInstantiated.Local
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugin Config
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        class PluginConfig
        {
            public string Prefix;
            public bool UsePermission;
            public bool UseCost;
            public bool UseItemCost;
            public List<Hash<string, int>> ItemCostList;
            public string ConfigVersion;
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Data File
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        class StoredData
        {
            public Hash<ulong, string> PlayerCodes = new Hash<ulong, string>();
        }
    }
}
