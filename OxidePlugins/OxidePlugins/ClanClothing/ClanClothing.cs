using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;


// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("Clan Clothing", "MJSU", "0.0.1")]
    // ReSharper disable once UnusedMember.Global
    class ClanClothing : RustPlugin
    {
        [PluginReference]
        Plugin Clans; //Clans plugin reference
        [PluginReference]
        Plugin ServerRewards; //Server Rewards Plugin Reference
        [PluginReference]
        Plugin Economics; //Economics Plugin Reference

        #region Class Fields
        private StoredData _storedData; //Plugin Data
        private PluginConfig _pluginConfig; //Plugin Config

        private const string UsePermission = "clanclothing.use"; //Plugin use permission
        #endregion

        #region Loading & Setup
        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            LoadVersionConfig();
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("ClanClothing");

            LoadLang();

            //Add the chat commands from the config
            cmd.AddChatCommand(_pluginConfig.Commands["CheckCost"], this, CheckCostChatCommand);
            cmd.AddChatCommand(_pluginConfig.Commands["ViewClanClothing"], this, ViewClanClothingChatCommand);
            cmd.AddChatCommand(_pluginConfig.Commands["ClaimCommand"], this, RedeemChatCommand);
            cmd.AddChatCommand(_pluginConfig.Commands["AddCommand"], this, AddChatCommand);
            cmd.AddChatCommand(_pluginConfig.Commands["RemoveCommand"], this, RemoveChatCommand);

            permission.RegisterPermission(UsePermission, this);
        }

        /// <summary>
        /// As the config version changed update with default values
        /// </summary>
        private void LoadVersionConfig()
        {
            _pluginConfig = Config.ReadObject<PluginConfig>();
        }

        private void LoadLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use that command",
                ["ExcludedItem"] = "is on the excluded item list and will not be added",
                ["Claimed"] = "You have claimed your clans clothing",
                ["Add"] = "You have added your clans clothing",
                ["Remove"] = "You have removed your clans clothing",
                ["NotInClan"] = "You do not appear to be in a clan",
                ["ClansNotLoaded"] = "The Clans plugin is not loaded",
                ["NotAvaliable"] = "Clothing not avaliable - Your clan owner has not setup your clan clothing yet!",
                ["NotOwner"] = "Only the clan owner can use this command",
                ["SRProfileNotFound"] = " Could not find a ServerRewards Profile for you",
                ["SRCantAfford"] = "You do not have enough ServerRewards to claim your clans clothing.\n Have: {0} Requires: {1}",
                ["EProfileNotFound"] = "Could not get your Economics money",
                ["ECantAfford"] = "You do not have enough Economics money to claim your clans clothing.\n Have: {0} Requires: {1}",
                ["ItemCantAfford"] = "You do not have enough {0} Have: {1} Requires: {2}",
                ["PluginFailed"] = "Something went wrong while taking your {0}",
                ["PluginCost"] = "{0} cost - {1}",
                ["ItemListCost"] = "Item list cost:",
                ["CanAfford"] = "You <color=green>CAN</color> afford this",
                ["CanNotAfford"] = "You <color=red>CAN NOT</color> afford this",
                ["NoCost"] = "There is no cost to claim Clan Clothing",
                ["HowToClaim"] = "type <color=yellow>/{0}</color> to claim your clans clothing",
                ["HowToView"] = "type <color=yellow>/{0}</color> to view your clans clothing"
            }, this);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(DefaultConfig(), true);
        }

        // ReSharper disable once UnusedMember.Local
        private PluginConfig DefaultConfig()
        {
            PrintWarning("Loading Default Config");
            return new PluginConfig
            {
                ExcludedItems = new List<string> { "metal.facemask", "metal.plate.torso", "roadsign.jacket", "roadsign.kilt" },
                Prefix = "[<color=yellow>Clan Clothing</color>]",
                UsePermissions = false,
                WipeDataOnMapWipe = true,
                UseCost = false,
                UseServerRewards = false,
                ServerRewardsCost = 0,
                UseItems = false,
                ItemCostList = new Hash<string, int>
                {
                    ["wood"] = 100,
                    ["stones"] = 50,
                    ["metal.fragments"] = 25
                },
                UseEconomics = false,
                EconomicsCost = 0,
                Commands = new Hash<string, string>
                {
                    ["CheckCost"] = "cc",
                    ["ViewClanClothing"] = "cc_view",
                    ["ClaimCommand"] = "cc_claim",
                    ["AddCommand"] = "cc_add",
                    ["RemoveCommand"] = "cc_remove"
                },
                ConfigVersion = Version.ToString()
            };
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Wipes the Clan Clothing data on map wipe if WipeDataOnMapWipe is set to true on the config
        /// </summary>
        /// <param name="name"></param>
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnNewSave(string name)
        {
            if (_pluginConfig.WipeDataOnMapWipe)
            {
                PrintWarning("Map wipe detected - Wiping Clan Clothing Data");
                _storedData = new StoredData();
                Interface.Oxide.DataFileSystem.WriteObject("ClanClothing", _storedData);
            }
        }
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Determines if the Clans plugin loaded. If not displays an error
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            if (Clans == null)
            {
                PrintWarning($"{_pluginConfig.Prefix} Clans plugin failed to load");
            }

            if (_pluginConfig.UseCost) //Configs is set to use cost
            {
                if (_pluginConfig.UseServerRewards && ServerRewards == null) //Server Rewards set to use but plugin is not present
                {
                    PrintWarning($"{_pluginConfig.Prefix} ServerRewards set to use but failed to load ServerRewards");
                }

                if (_pluginConfig.UseEconomics && Economics == null) //Economics set to use but plugin is not present
                {
                    PrintWarning($"{_pluginConfig.Prefix} Economics set to use but failed to load Economics");
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("ClanClothing", _storedData);
        }
        #endregion

        #region Chat Commands
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Allows players in a clan to claim their clan clothing
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void RedeemChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!CheckPermission(player, UsePermission, true)) return; //Make sure player has permission

            string playerClanTag = GetPlayerClanTag(player);
            if (playerClanTag == null) return; //Failed to get Clan Tag for player

            List<ClothingItem> itemsToGive = _storedData.ClanClothing[playerClanTag]; //Get's the Clothing for the Players Clan

            if (itemsToGive == null || itemsToGive.Count == 0) //No clan clothing is configured or contains no items
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("NotAvaliable", player.UserIDString)}");
                return;
            }

            if (!CanPlayerAfford(player)) return; //Player can't afford the clan clothing    
            if (!TakeCostFromPlayer(player)) return; //Failed to apply the cost to the player

            foreach (ClothingItem item in itemsToGive) //Give all the items to the player
            {
                player.inventory.GiveItem(ItemManager.CreateByItemID(item.ItemId, 1, item.SkinId), player.inventory.containerWear);
            }

            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Claimed", player.UserIDString)}");
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Allows the owner of a clan to add their clan clothing
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void AddChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!CheckPermission(player, UsePermission, true)) return; //Make sure player has permission

            string playerClanTag = GetPlayerClanTag(player);
            if (playerClanTag == null) return; //Failed to get Clan Tag for player

            if (!IsPlayerClanOwner(player, playerClanTag)) return; //Player is not the clan owner

            _storedData.ClanClothing[playerClanTag] = new List<ClothingItem>();
            foreach (Item item in player.inventory.containerWear.itemList) //Add the items from the owners wear container to the clans clothing list
            {
                if (_pluginConfig.ExcludedItems.Contains(item.info.shortname)) //If and item is on the excluded item list
                {
                    PrintToChat($"{_pluginConfig.Prefix} {ItemManager.FindItemDefinition(item.info.shortname).displayName.translated} {Lang("ExcludedItem", player.UserIDString)}");
                }
                else //item gets added to the clan clothing list
                {
                    _storedData.ClanClothing[playerClanTag].Add(new ClothingItem(item.info.itemid, item.skin));
                }
            }

            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Add", player.UserIDString)}");
            Interface.Oxide.DataFileSystem.WriteObject("ClanClothing", _storedData);
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Allows the owner of a clan to remove their clan clothing
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void RemoveChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!CheckPermission(player, UsePermission, true)) return;

            string playerClanTag = GetPlayerClanTag(player);
            if (playerClanTag == null) return; //Failed to get Clan Tag for player

            if (!IsPlayerClanOwner(player, playerClanTag)) return; //Player is not the clan owner

            _storedData.ClanClothing[playerClanTag] = null; //Remove the clan from the plugin data
            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Remove", player.UserIDString)}");
        }

        /// <summary>
        /// Chat Command for play to check how much Clan Clothing costs
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void CheckCostChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!CheckPermission(player, UsePermission, true)) return; //Player does not have permission

            string message = _pluginConfig.Prefix + "\n"; //Put the plugin prefix at the begining
            if (_pluginConfig.UseCost) //Use cost is true
            {
                if (_pluginConfig.UseServerRewards) //Use server rewards is true
                {
                    message += $"{Lang("PluginCost", player.UserIDString, "ServerReward", _pluginConfig.ServerRewardsCost)}\n";
                }

                if (_pluginConfig.UseEconomics) //Use economics is true
                {
                    message += $"{Lang("PluginCost", player.UserIDString, "Economics", _pluginConfig.EconomicsCost)}\n";
                }

                if (_pluginConfig.UseItems) //Use items is true
                {
                    message += $"{Lang("ItemListCost", player.UserIDString)}\n ";

                    foreach (KeyValuePair<string, int> item in _pluginConfig.ItemCostList)
                    {
                        message += $"   {ItemManager.FindItemDefinition(item.Key).displayName.translated} - {item.Value}\n";
                    }
                }

                //Puts whether the player can afford the clan clothing
                message += CanPlayerAfford(player) ? Lang("CanAfford", player.UserIDString) : Lang("CanNotAfford", player.UserIDString);
            }
            else //Use cost is false clan clothing cost nothing
            {
                message += Lang("NoCost", player.UserIDString);
            }

            PrintToChat(player, message);
            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("HowToClaim", player.UserIDString, _pluginConfig.Commands["ClaimCommand"])}");
            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("HowToView", player.UserIDString, _pluginConfig.Commands["ViewClanClothing"])}");
        }

        /// <summary>
        /// Player command to see their clans clothing
        /// If a skin is used will show the skin name
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void ViewClanClothingChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!CheckPermission(player, UsePermission, true)) return; //Player does not have permission

            string playerClanTag = GetPlayerClanTag(player);
            if (playerClanTag == null) return; //Failed to get Clan Tag for player

            if(_storedData.ClanClothing[playerClanTag] == null) //Players clan has not setup clan clothing
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("NotAvaliable", player.UserIDString)}");
                return;
            }

            //Prepare the message to the player to display their clans clothing
            string message = _pluginConfig.Prefix + "\n";
            foreach(ClothingItem clothingItem in _storedData.ClanClothing[playerClanTag])
            {
                ItemDefinition item = ItemManager.FindItemDefinition(clothingItem.ItemId); //Information about the clothing item

                if (clothingItem.SkinId == 0) //No skin is set
                {
                    message += $"   {item.displayName.translated}\n";
                }
                else //skin is set
                {
                    foreach(var skin in ItemSkinDirectory.ForItem(item)) //Loop over all the skins for the item
                    {
                        if(skin.id == clothingItem.SkinId) //If the skin id's match add the skin display name to the message
                        {
                            message += $"   {skin.invItem.displayName.translated}\n";
                            break;
                        }
                    }
                }
            }

            PrintToChat(player, message);
        }

        #endregion

        #region Can Afford & Take Items and Points
        private bool CanPlayerAfford(BasePlayer player)
        {
            if (!_pluginConfig.UseCost) return true; //Use cost is false

            if (_pluginConfig.UseServerRewards) //Use server rewards
            {
                if (ServerRewards != null) //Server rewards is loaded
                {
                    object playerPoints = ServerRewards?.Call("CheckPoints", player.userID); //get the player server rewards points

                    if (playerPoints == null) //failed to retrieve the player server reward points
                    {
                        PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("SRProfileNotFound", player.UserIDString)}");
                        return false;
                    }

                    if ((int)(playerPoints) < _pluginConfig.ServerRewardsCost) //The player cannot afford the cost in server rewards
                    {
                        PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("SRCantAfford", player.UserIDString, (int)(playerPoints), _pluginConfig.ServerRewardsCost)}");
                        return false;
                    }
                }
                else //Server rewards is set to true but failed to load
                {
                    PrintWarning("UseServerRewards set to true but ServerRewards was not loaded");
                }
            }

            if (_pluginConfig.UseEconomics) //Use Economics
            {
                if (Economics != null) //Economics is loaded
                {
                    // ReSharper disable once ConstantNullCoalescingCondition
                    double playerMoney = Economics?.Call<double>("GetPlayerMoney", player.userID) ?? 0; //Get the player economics points

                    if (playerMoney < _pluginConfig.EconomicsCost) //player cannot afford the economics money
                    {
                        PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("ECantAfford", player.UserIDString, playerMoney, _pluginConfig.EconomicsCost)}");
                        return false;
                    }
                }
                else //Economics is set to true but failed to load
                {
                    PrintWarning("Economics set to true but ServerRewards was not loaded");
                }
            }

            if (_pluginConfig.UseItems) //Use items is set to true
            {
                bool canItemAfford = true;
                string message = _pluginConfig.Prefix + ":\n";
                foreach (KeyValuePair<string, int> item in _pluginConfig.ItemCostList) //Loop over the ItemCostList set in the config
                {
                    Item playerInventoryItem = player?.inventory?.FindItemID(item.Key); //Find the item in the players inventory

                    if (playerInventoryItem == null || playerInventoryItem.amount < item.Value) //Player does not have the item or they do not have enough
                    {
                        PrintToChat(player, $"   {Lang("ItemCantAfford", player?.UserIDString, ItemManager.FindItemDefinition(item.Key).displayName.translated, playerInventoryItem?.amount ?? 0, item.Value)}");
                        canItemAfford = false;
                    }
                }

                if (!canItemAfford) return false;
            }

            return true; //Player can afford
        }

        private bool TakeCostFromPlayer(BasePlayer player)
        {
            if (!_pluginConfig.UseCost) return true; //Use cost is false

            if (_pluginConfig.UseServerRewards && ServerRewards != null) //Use ServerRewards and Server Rewards is loaded
            {
                object success = ServerRewards?.Call("TakePoints", player, _pluginConfig.ServerRewardsCost); //Take the server reward points from the player

                if (success == null) //Player does not have any server rewards or failed to call plugin
                {
                    PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("PluginFailed", player.UserIDString, "ServerRewards")}");
                    return false;
                }
            }

            if (_pluginConfig.UseEconomics && Economics != null) //Use Economics and Economics is loaded
            {
                object success = Economics?.Call("Withdraw", player, _pluginConfig.EconomicsCost); //Take the economics money from the player

                if (success == null) //Failed to call Withdraw on the economics plugin
                {
                    PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("PluginFailed", player.UserIDString, "Economics money")}");
                    return false;
                }
            }

            if (_pluginConfig.UseItems) //Use items is true
            {
                foreach (KeyValuePair<string, int> item in _pluginConfig.ItemCostList)
                {
                    List<Item> collection = new List<Item>();
                    player.inventory.Take(collection, ItemManager.FindItemDefinition(item.Key).itemid, item.Value);
                }
            }

            return true; //Successfully removed all the necessary costs from the player
        }

        private string GetPlayerClanTag(BasePlayer player)
        {
            if (Clans == null) //No Clans plugin loaded
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("ClansNotLoaded", player.UserIDString)}");
                return null;
            }

            var playerClanTag = Clans?.Call<string>("GetClanOf", player.userID);
            if (playerClanTag == null) //Player is not in a clan
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("NotInClan", player.UserIDString)}");
                return null;
            }

            return playerClanTag;
        }

        private bool IsPlayerClanOwner(BasePlayer player, string playerClanTag)
        {
            JObject playerClanData = Clans?.Call<JObject>("GetClan", playerClanTag); //Gets the players Clan Information
            if (playerClanData == null) return false;

            ulong playerClanOwnerId;
            if (!ulong.TryParse(playerClanData["owner"].ToString(), out playerClanOwnerId)) return false; //If it failed to parse the owner to a ulong

            if (player.userID != playerClanOwnerId) //Player does not own the clan
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("NotOwner", player.UserIDString)}");
                return false;
            }

            return true;
        }
        #endregion

        #region Helper Methods
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// @credit to Exel80 - code from EasyVote
        /// Gets the registered lang messages
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
            if (!_pluginConfig.UsePermissions || permission.UserHasPermission(player.UserIDString, perm)) //Use permission is false or player has permission
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
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Clan Clothing Plugin Config
        /// </summary>
        private class PluginConfig
        {
            public List<string> ExcludedItems;
            public string Prefix;
            public bool UsePermissions;
            public bool WipeDataOnMapWipe;
            public bool UseCost;
            public bool UseServerRewards;
            public int ServerRewardsCost;
            public bool UseItems;
            public Hash<string, int> ItemCostList;
            public bool UseEconomics;
            public double EconomicsCost;
            public Hash<string, string> Commands;
            public string ConfigVersion;
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Clan Clothing Plugin Data
        /// </summary>
        private class StoredData
        {
            public Hash<string, List<ClothingItem>> ClanClothing = new Hash<string, List<ClothingItem>>();
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Represents a clothing item for a clan
        /// </summary>
        private class ClothingItem
        {
            public int ItemId;
            public int SkinId;

            public ClothingItem(int itemId, int skinId)
            {
                ItemId = itemId;
                SkinId = skinId;
            }
        }
        #endregion

        private void SendHelpText(BasePlayer player)
        {
            PrintToChat(player, @"<color=yellow>/cc</color> - Check if you can afford clan clothing\n
                                <color=yellow>/cc_claim</color> - Claim your clans clothing\n
                                <color=yellow>/cc_add</color> - Allows the clan owner to set their current clothing as the clan clothing\n
                                <color=yellow>/cc_remove</color> - Allows the clan owner to remove their current clan clothing");
        }
    }
}
