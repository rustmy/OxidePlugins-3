using System.Collections.Generic;
using System.Reflection;
using Oxide.Core;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    // ReSharper disable once UnusedMember.Global
    class AutoCodeLock : RustPlugin
    {
        private readonly FieldInfo _codelock = typeof(CodeLock).GetField("code", BindingFlags.NonPublic | BindingFlags.Instance);
        readonly FieldInfo _whitelistField = typeof(CodeLock).GetField("whitelistPlayers", BindingFlags.Instance | BindingFlags.NonPublic);

        private StoredData _storedData;
        private PluginConfig _pluginConfig; //Config File

        private const string _usePermission = "autocodelock.use";

        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoCodes");

            permission.RegisterPermission(_usePermission, this);
            LoadVersionedConfig();

            LoadLang();
        }

        private void LoadLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use that command"
            }, this);
        }

        private void LoadVersionedConfig()
        {
            _pluginConfig = Config.ReadObject<PluginConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(DefaultConfig(), true);
        }

        private PluginConfig DefaultConfig()
        {
            return new PluginConfig
            {
                Prefix = "[<color=yellow>AutoCode</color>]",
                UsePermission = false,
                UseCost = true,
                UseItemCost = true,
                ItemCostList = new List<Hash<string, int>> { new Hash<string, int> { ["lock.code"] = 1 }, new Hash<string, int> { ["wood"] = 400, ["metal.fragments"] = 100 } },
                ConfigVersion = Version.ToString()
            };
        }

        [ChatCommand("ac")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
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
                    _storedData.PlayerCodes[player.userID] = args[0];
                    PrintToChat(player, $"{_pluginConfig.Prefix} You have set your codelock code to {args[0]}");
                    break;
                default:
                    PrintToChat(player, $"{_pluginConfig.Prefix} To set your codelock code type /ac 1234");
                    break;
            }

            Interface.Oxide.DataFileSystem.WriteObject("AutoCodes", _storedData);
        }


        // ReSharper disable once UnusedMember.Local
        void OnEntitySpawned(BaseNetworkable entity)
        {
            Door door = entity as Door;
            if (door == null) return;

            BasePlayer player = BasePlayer.FindByID(door.OwnerID);

            if (_storedData.PlayerCodes[player.userID] == null) return;

            int index;
            if (!CanAfford(player, out index))
            {
                PrintToChat(player, "You can not afford to use AutoCodelock");
                return;
            }

            if (index != -1)
            {
                TakeCost(player, index);
            }

            AddLockToDoor(player, door);
        }

        private bool CanAfford(BasePlayer player, out int costIndex)
        {
            costIndex = -1;
            if (!_pluginConfig.UseCost) return true;

            if (_pluginConfig.UseItemCost)
            {
                bool canAfford = true;

                for (int index = 0; index < _pluginConfig.ItemCostList.Count; index++)
                {
                    canAfford = true;
                    foreach (KeyValuePair<string, int> item in _pluginConfig.ItemCostList[index])
                    {
                        Item playerItem = player.inventory.FindItemID(item.Key);

                        if (playerItem == null || playerItem.amount < item.Value)
                        {
                            canAfford = false;
                            break;
                        }
                    }

                    if (canAfford)
                    {
                        costIndex = index;
                        break;
                    }
                }

                if (!canAfford) return false;
            }

            return true;
        }

        private void TakeCost(BasePlayer player, int costIndex)
        {
            if (!_pluginConfig.UseCost) return;

            if (_pluginConfig.UseItemCost)
            {
                var items = _pluginConfig.ItemCostList[costIndex];
                List<Item> collection = new List<Item>();

                foreach (KeyValuePair<string, int> item in items)
                {
                    player.inventory.Take(collection, ItemManager.FindItemDefinition(item.Key).itemid, item.Value);
                }
            }
        }

        private void AddLockToDoor(BasePlayer player, Door door)
        {
            BaseEntity lockentity = GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab", Vector3.zero, new Quaternion());

            lockentity.OnDeployed(door);

            List<ulong> whitelist = (List<ulong>)_whitelistField.GetValue(lockentity);
            whitelist.Add(player.userID);
            _whitelistField.SetValue(lockentity, whitelist);

            string code = _storedData.PlayerCodes[player.userID];

            if (!string.IsNullOrEmpty(code))
            {
                CodeLock @lock = lockentity.GetComponent<CodeLock>();
                _codelock.SetValue(@lock, code);
                @lock.SetFlag(BaseEntity.Flags.Locked, true);
            }

            if (!lockentity) return;
            lockentity.gameObject.Identity();
            lockentity.SetParent(door, "lock");
            lockentity.Spawn();
            door.SetSlot(BaseEntity.Slot.Lock, lockentity);
        }

        private bool ValidCode(string code)
        {
            if (code.Length != 4) return false;
            int codeNum;
            if (!int.TryParse(code, out codeNum)) return false;
            if (codeNum < 0) return false;
            return true;


        }

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

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        // ReSharper disable once ClassNeverInstantiated.Local
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
        class StoredData
        {
            public Hash<ulong, string> PlayerCodes = new Hash<ulong, string>();
        }
    }
}
