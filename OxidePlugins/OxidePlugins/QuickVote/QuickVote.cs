﻿using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("QuickVote", "MJSU", "0.0.1")]
    [Description("Voting with speed")]
    // ReSharper disable once UnusedMember.Global
    class QuickVote : RustPlugin
    {
        #region Class Fields
        private bool DEBUG = true;

        private StoredData _storedData;
        private PluginConfig _pluginConfig;

        private Timer _voteChecker;
        #endregion

        #region Loading & Setup
        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            _pluginConfig = Config.ReadObject<PluginConfig>();

            if(_pluginConfig.Prefix == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);
            
            _storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("QuickVote");

            LoadLang();
            HandleDataWipe();

            _voteChecker = timer.Every(_pluginConfig.CheckVoteTimerIntervalInMinutes * 60, () =>
            {
                foreach(ulong playerId in _storedData.Players.Keys)
                {
                    BasePlayer player = BasePlayer.FindByID(playerId);
                    HandleRewardCheck(player, false);
                }
                Interface.GetMod().DataFileSystem.WriteObject("QuickVote", _storedData);
            });
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Register the lang messages for the plugin
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void LoadLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ResponseError"] = "Error: {0} - Couldn't get an answer for {1}",
                ["ClaimReward"] = "You just received your vote reward(s). Enjoy!",
                ["EarnReward"] = "When you have voted. Type <color=cyan>/reward</color> to earn your reward(s)!",
                ["VoteEqual"] = "<color=cyan>Player reward, when voted </color><color=orange>{0}</color><color=cyan> timee.</color>",
                ["VoteEvery"] = "<color=cyan>Player reward, every </color><color=orange>{0}</color><color=cyan> vote(s).</color>",
                ["ThankYou"] = "Thank you for voting {0} time(s)",
                ["Received"] = "You have received {0}x {1}",
                ["NoRewards"] = " You do not have any new rewards avaliable \n Please type <color=yellow> /vote </color> and go to the website to vote and receive your reward",
                ["GlobalAnnouncment"] = "<color=yellow>{0}</color><color=cyan> has voted </color><color=yellow>{1}</color><color=cyan> time(s) and just received their rewards. Find out where to vote by typing</color><color=yellow> /vote</color>\n<color=cyan>To see a list of avaliable rewards type</color><color=yellow> /reward list</color>",
                ["money"] = "{0} has been deposited into your account",
                ["rp"] = "You have gained {0} reward points",
                ["addlvl"] = "You have gained {0} level(s)",
                ["addgroup"] = "You have been added to group {0} for {1}",
                ["grantperm"] = "You have been given permission {0} for {1}",
                ["zlvl-wc"] = "You have gained {0} woodcrafting level(s)",
                ["zlvl-mg"] = "You have gained {0} mining level(s)",
                ["zlvl-s"] = "You have gained {0} skinning level(s)",
                ["zlvl-c"] = "You have gained {0} crafting level(s)",
                ["TopVoter"] = "Congratulations on being one of the top voters for the month! You have been placed into group '{0}' for the next month!",
                ["VotesToClaim"] = "You currently have {0} vote you can claim!",
                ["Votes"] = "You have {0} vote(s) on record"
            }, this);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// load the default config for this plugin
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
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
                Prefix = config?.Prefix ?? "[<color=cyan>Quick Vote</color>]",
                Settings = config?.Settings ?? new List<VoteSite>
                {
                    new VoteSite
                    {
                        Id = "",
                        Key = "",
                        ClaimUrl = "http://rust-servers.net/api/?action=custom&object=plugin&element=reward&key={0}&steamid={1}",
                        VoteUrl = "http://rust-servers.net/server/{0}"
                    },
                    new VoteSite
                    {
                        Id = "",
                        Key = "",
                        ClaimUrl = "http://api.toprustservers.com/api/put?plugin=voter&key={0}&uid={1}",
                        VoteUrl = "http://toprustservers.com/server/{0}"
                    },
                    new VoteSite
                    {
                        Id = "",
                        Key = "",
                        ClaimUrl = "http://beancan.io/vote/put/{0}/{1}",
                        VoteUrl = "http://beancan.io/server/{0}"
                    }
                },
                Reward = config?.Reward ?? new Hash<int, Hash<string, string>>
                {
                    [-1] = new Hash<string, string>() { ["supply.signal"] = "1", ["money"] = "250" },
                    [3] = new Hash<string, string>() { ["money"] = "50" },
                    [-5] = new Hash<string, string>() { ["addlvl"] = "1", ["money"] = "50" }
                },
                Variables = config?.Variables ?? new Hash<string, string>
                {
                    ["money"] = "eco.c deposit {playerid} {value}",
                    ["rp"] = "sr add {playername} {value}",
                    ["addlvl"] = "xp addlvl {playername} {value}",
                    ["addgroup"] = "addgroup {playerid} {value} {value2}",
                    ["grantperm"] = "grantperm {playerid} {value} {value2}",
                    ["zlvl-wc"] = "zlvl {playername} WC +{value}",
                    ["zlvl-m"] = "zlvl {playername} M +{value}",
                    ["zlvl-s"] = "zlvl {playername} S +{value}",
                    ["zlvl-c"] = "zlvl {playername} C +{value}"
                },
                BroadcastVoteToAll = config?.BroadcastVoteToAll ?? false,
                WipeVoteDataOnNewMonth = config?.WipeVoteDataOnNewMonth ?? true,
                CheckVoteTimerIntervalInMinutes = config?.CheckVoteTimerIntervalInMinutes ?? 150f,
                EnableTopVoter = config?.EnableTopVoter ?? false,
                TopVoterGroups = config?.TopVoterGroups ?? new List<string> { "gold", "silver", "bronze" }
            };
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Check if it is time to wipe vote data. If it handle it.
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void HandleDataWipe()
        {
            if (_storedData.Month != DateTime.Now.Month)  //If it's a new month wipe the saved votes
            {
                if(_pluginConfig.EnableTopVoter) HandleTopVoter();
                if (_pluginConfig.WipeVoteDataOnNewMonth)
                {
                    StoredData oldData = _storedData;
                    PrintWarning("New month detected. Wiping user votes");
                    Interface.GetMod().DataFileSystem.WriteObject("QuickVote.bac", _storedData);
                    _storedData = new StoredData {TopVoters = oldData.TopVoters, NotifiedPlayers = oldData.NotifiedPlayers};
                    Interface.GetMod().DataFileSystem.WriteObject("QuickVote", _storedData); // Write wiped data
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            _voteChecker?.Destroy();
            Interface.Oxide.DataFileSystem.WriteObject("QuickVote", _storedData);
        }
        #endregion

        #region Chat Commands

        [ChatCommand("fake")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        void FakeChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            _storedData.Players[player.userID].TotalVotes++;
        }
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Player called the vote chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// ////////////////////////////////////////////////////////////////////////
        [ChatCommand("vote1")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        void VoteChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            if (args.Length == 0)
            {
                foreach (VoteSite site in _pluginConfig.Settings)
                {
                    if (!string.IsNullOrEmpty(site.Id) && !string.IsNullOrEmpty(site.Key))
                    {
                        Chat(player, site.VoteUrl, site.Id);
                    }
                }
                Chat(player, Lang("EarnReward", player.UserIDString));
                return;
            }

            switch(args[0].ToLower())
            {
                case "list":
                    HandleList(player);
                    break;

                default:
                    SendHelpText(player);
                break;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Player called the rewards chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// ////////////////////////////////////////////////////////////////////////
        [ChatCommand("claim")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        void RewardChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            if (args.Length == 0) HandleRewardCheck(player, true);
            else SendHelpText(player);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle displaying the rewards list to the player
        /// </summary>
        /// <param name="player"></param>
        /// ////////////////////////////////////////////////////////////////////////
        private void HandleList(BasePlayer player)
        {
            string message = $"{_pluginConfig.Prefix}:\n";
            foreach (KeyValuePair<int, Hash<string, string>> vote in _pluginConfig.Reward)
            {
                if(vote.Key < 0) message += $"{Lang("VoteEvery", player.UserIDString, vote.Key)}\n";
                else message += $"{Lang("VoteEqual", player.UserIDString, vote.Key)}\n";

                foreach (KeyValuePair<string, string> reward in vote.Value)
                {
                    if (_pluginConfig.Variables.ContainsKey(reward.Key))
                    {
                        message += $" - {reward.Key}: {reward.Value}";
                    }
                    else
                    {
                        ItemDefinition itemDef = ItemManager.FindItemDefinition(reward.Key);
                        if (itemDef == null)
                        {
                            PrintError($"{reward.Key} is not a shortname for any item");
                            continue;
                        }
                        message += $" - {itemDef.displayName.translated}: {reward.Value}\n";
                    }
                }
            }

            SendReply(player, message);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// handles the chat command for reward checking
        /// </summary>
        /// <param name="player">player who is checking</param>
        /// <param name="claim">should the player claim their reward</param>
        /// ////////////////////////////////////////////////////////////////////////
        private void HandleRewardCheck(BasePlayer player, bool claim)
        {
            foreach(var voteSite in _pluginConfig.Settings)
            {
                if(!string.IsNullOrEmpty(voteSite.Id) && !string.IsNullOrEmpty(voteSite.Key))
                {
                    string server = string.Format(voteSite.ClaimUrl, voteSite.Key, player.UserIDString);
                    webrequest.EnqueueGet(server, (code, response) => PlayerVoteHandlingCallback(player, code, response, claim), this, null, 5500f);
                    if (DEBUG) PrintWarning(server);
                }
            }
        }
        #endregion

        #region Callback
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Callback after checking players rewards
        /// </summary>
        /// <param name="player">player that was checked</param>
        /// <param name="code">http code</param>
        /// <param name="response">html response</param>
        /// <param name="claim">should the player claim his rewards</param>
        /// ////////////////////////////////////////////////////////////////////////
        void PlayerVoteHandlingCallback(BasePlayer player, int code, string response, bool claim)
        {
            if (DEBUG) Puts($"[DEBUG] {player.displayName} code: {code}");

            if (response == null || code != 200)
            {
                Puts(Lang("ResponseError", player.UserIDString, code, player.displayName));
                return;
            }

            _storedData.Players[player.userID] = _storedData.Players[player.userID] ?? new PlayerVoteData(player.userID, player.displayName);

            if (response == "1") _storedData.Players[player.userID].TotalVotes++;

            if (claim) RewardHandler(player);
        }
        #endregion

        #region Reward Handler
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle the rewards to give to the player
        /// </summary>
        /// <param name="player">player to receive rewards</param>
        /// ////////////////////////////////////////////////////////////////////////
        private void RewardHandler(BasePlayer player)
        {
            PlayerVoteData playerData = _storedData.Players[player.userID];
            if(playerData.ClaimedVotes == playerData.TotalVotes) //Players claimed and total votes are equal
            {
                Chat(player, Lang("NoRewards", player.UserIDString));
                Chat(player, Lang("Votes", player.UserIDString, playerData.TotalVotes));
                return;
            }

            if (DEBUG) Puts($"Saved Votes:{playerData.ClaimedVotes} Times Voted: {playerData.TotalVotes}");

            //Loop over each vote missed so the user get's all their rewards
            for (int timesVoted = playerData.ClaimedVotes + 1; timesVoted <= playerData.TotalVotes; timesVoted++)
            {
                string message = $"{Lang("ThankYou", player.UserIDString, timesVoted)}";

                foreach (KeyValuePair<int, Hash<string, string>> voteReward in _pluginConfig.Reward)
                {
                    if (DEBUG) Puts($"Reward Number: {voteReward.Key} vote: {timesVoted}");

                    // If player should not receive a reward for this vote continue the loop
                    // If the reward number is negative and the vote divides evenly into the reward number
                    // and if the reward number is positive and the vote and reward number are equal
                    // If both negate to true then we continue this loop and don't run the code below
                    if (!(voteReward.Key < 0 && timesVoted % Math.Abs(voteReward.Key) == 0) &&
                        !(voteReward.Key > 0 && timesVoted == voteReward.Key)) { continue; }

                    // Loop for all rewards.
                    foreach (KeyValuePair<string, string> reward in voteReward.Value)
                    {
                        // Checking variables and run console command.
                        // If variable not found, then try give item.
                        if (_pluginConfig.Variables.ContainsKey(reward.Key))
                        {
                            rust.RunServerCommand(GetCommand(player, reward.Key, reward.Value));
                            message += $"{Lang(reward.Key, player.UserIDString, reward.Value)}\n";
                            if (DEBUG) Puts($"{player.displayName} vote ran command {string.Format(reward.Key, reward.Value)}");
                        }
                        else
                        {
                            int amount;
                            if (!int.TryParse(reward.Value, out amount)) continue;
                            Item itemToReceive = ItemManager.CreateByName(reward.Key, amount);
                            if (itemToReceive == null)
                            {
                                PrintError($"{reward.Key} is not a shortname for any item");
                                continue;
                            }

                            if (DEBUG) Puts($"{player.displayName} received item {itemToReceive.info.displayName.translated} {reward.Value}");

                            player.Command("note.inv ", itemToReceive.info.itemid, itemToReceive.amount * 1f);
                            if (!player.inventory.GiveItem(itemToReceive, player.inventory.containerMain))  //If the item does not end up in the inventory. Drop it on the ground for them
                            {
                                itemToReceive.Drop(player.GetDropPosition(), player.GetDropVelocity());
                                player.Command("note.inv ", itemToReceive.info.itemid, itemToReceive.amount * -1f);
                            }

                            message += $"{Lang("Received", player.UserIDString, reward.Value, itemToReceive.info.displayName.translated)}\n";
                        }
                    }
                }

                playerData.ClaimedVotes = timesVoted;
                Chat(player, message);
            }

            if (_pluginConfig.BroadcastVoteToAll) //Broadcast to all users that player has voted
            {
                PrintToChat($"{Lang("GlobalAnnouncment", player.UserIDString, player.displayName, playerData.TotalVotes)}");
            }
            Interface.GetMod().DataFileSystem.WriteObject("QuickVote", _storedData);
        }
        #endregion

        #region Oxide Hooks
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// When a player wakes up if they are to be notifed on being a top voter notify them
        /// </summary>
        /// <param name="player">player to check if they're a top voter</param>
        /// ////////////////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (_storedData.NotifiedPlayers[player.userID] != null) // If player should be notified about being a top voter notify them
            {
                Chat(player, Lang("TopVoter", player.UserIDString, _storedData.NotifiedPlayers[player.userID]));
                _storedData.NotifiedPlayers.Remove(player.userID);
            }

            PlayerVoteData playerData = _storedData.Players[player.userID];

            if (playerData?.TotalVotes > playerData?.ClaimedVotes) //If the player has votes the claim notify them
            {
                Chat(player, Lang("VotesToClaim", player.UserIDString), playerData.TotalVotes - playerData.ClaimedVotes);
            }
        }
        #endregion

        #region Top Voter
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Remove the previous top voters from their groups
        /// Figure out the new top voters
        /// Add the new top voters the groups they earned
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void HandleTopVoter()
        {
            RemoveOldTopVotersFromGroup(); 
            _storedData.TopVoters = new Hash<string, List<ulong>>();
            _storedData.NotifiedPlayers = new Hash<ulong, string>();
            
            // Remove the old top voters
            List<PlayerVoteData> votedPlayer = _storedData.Players.Values.OrderByDescending(vote => vote.TotalVotes).ToList(); //Create a list order desceding by total votes
            if (votedPlayer.Count == 0) return; //No players voter this month lets return

            int index = 0; //Starting group index
            int currentTopVote = votedPlayer[0].TotalVotes; //What the current top vote is

            foreach (PlayerVoteData playerData in votedPlayer)
            {
                if (playerData.TotalVotes == currentTopVote) //Players share the same top vote put them in the same vote
                {
                    if (_storedData.TopVoters[_pluginConfig.TopVoterGroups[index]] == null) _storedData.TopVoters[_pluginConfig.TopVoterGroups[index]] = new List<ulong>();
                    _storedData.TopVoters[_pluginConfig.TopVoterGroups[index]].Add(playerData.Id);
                }
                else if (playerData.TotalVotes < currentTopVote) //Players vote is less than the current top. Put them in one lower group and set the current top vote
                {
                    currentTopVote = playerData.TotalVotes;
                    index++;
                    if (index >= _pluginConfig.TopVoterGroups.Count) return; //We have reached more than the total number of top groups
                    if (_storedData.TopVoters[_pluginConfig.TopVoterGroups[index]] == null) _storedData.TopVoters[_pluginConfig.TopVoterGroups[index]] = new List<ulong>();
                    _storedData.TopVoters[_pluginConfig.TopVoterGroups[index]].Add(playerData.Id);
                }
            }
            AddTopVotersToGroup();
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Remove all the previous top voters from the groups they are in
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void RemoveOldTopVotersFromGroup()
        {
            _storedData.NotifiedPlayers = new Hash<ulong, string>();
            foreach (KeyValuePair<string, List<ulong>> topPlayer in _storedData.TopVoters) //Loop over each group
            {
                foreach (ulong player in topPlayer.Value) //loop over each player id
                {
                    RemoveFromGroup(player.ToString(), topPlayer.Key);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Loop over all the top voters and add them to the group
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void AddTopVotersToGroup()
        {
            foreach (KeyValuePair<string, List<ulong>> topPlayer in _storedData.TopVoters) //Loop over each group
            {
                foreach (ulong player in topPlayer.Value) //Loop over each player id
                {
                    AddToGroup(player.ToString(), topPlayer.Key);
                    _storedData.NotifiedPlayers[player] = topPlayer.Key;
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Add the given player to the group
        /// </summary>
        /// <param name="playerId">player to be added</param>
        /// <param name="group">group to add player too</param>
        /// ////////////////////////////////////////////////////////////////////////
        private void AddToGroup(string playerId, string group)
        {
            if (permission.GroupExists(group)) //Make sure group exists
                permission.AddUserGroup(playerId, group);
            else
                PrintWarning($"Failed to add player to '{group}'. Make sure you have the right group name");
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Remove the given player from the group
        /// </summary>
        /// <param name="playerId">player to be removed</param>
        /// <param name="group">group to remove player from</param>
        /// ////////////////////////////////////////////////////////////////////////
        private void RemoveFromGroup(string playerId, string group)
        {
            if (permission.GetUserGroups(playerId).Contains(group)) permission.RemoveUserGroup(playerId, group);
            else PrintWarning($"Failed to remove player from '{group}'. Make sure you have the right group name");
        }
        #endregion

        #region Helper Methods
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Returns a message to the player with the plugin  prefix and the formated string
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// ////////////////////////////////////////////////////////////////////////
        private void Chat(BasePlayer player, string format, params object[] args) => PrintToChat(player, $"{_pluginConfig.Prefix} {format}", args);

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

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Gets the server command to execute
        /// </summary>
        /// <param name="player">player for the command to be executed on</param>
        /// <param name="commandKey">key to the hash of the command</param>
        /// <param name="commandValue">values to be passed to the command</param>
        /// ////////////////////////////////////////////////////////////////////////
        /// <returns></returns>
        private string GetCommand(BasePlayer player, string commandKey, string commandValue)
        {
            string[] strValues = commandValue.Split(' '); //Split all the values

            string output = _pluginConfig.Variables[commandKey] //place the player in
                    .Replace("{playerid}", player.UserIDString)
                    .Replace("{playername}", $"\"{player.displayName}\"");

            for (int index = 0; index < strValues.Length; index++) //Loop over each value and insert it's value
            {
                output = output.Replace($"{{value{index}}}", strValues[index]);
            }

            return output;
        }
        #endregion

        #region Classes

        #region Config Classes
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Containts the plugin config
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        class PluginConfig
        {
            public string Prefix { get; set; }
            public List<VoteSite> Settings { get; set; }
            public Hash<int, Hash<string, string>> Reward { get; set; }
            public Hash<string, string> Variables { get; set; }
            public bool BroadcastVoteToAll { get; set; }
            public bool WipeVoteDataOnNewMonth { get; set; }
            public float CheckVoteTimerIntervalInMinutes { get; set; }
            public bool EnableTopVoter { get; set; }
            public List<string> TopVoterGroups {get; set; }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Contains the information of a voting site
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        class VoteSite
        {
            public string Key { get; set; }
            public string Id { get; set; }
            public string VoteUrl { get; set; }
            public string ClaimUrl { get; set; }
        }
        #endregion

        #region Data Classes
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Contains all the data for the plugin
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        class StoredData
        {
            public Hash<ulong, PlayerVoteData> Players = new Hash<ulong, PlayerVoteData>();
            public int Month = DateTime.Now.Month;
            public Hash<string, List<ulong>> TopVoters = new Hash<string, List<ulong>>();
            public Hash<ulong, string> NotifiedPlayers = new Hash<ulong, string>();
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Contains vote information about a player
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        class PlayerVoteData
        {
            public ulong Id { get; }
            public string DisplayName { get; set; }
            public int TotalVotes { get; set; }
            public int ClaimedVotes { get; set; }

            public PlayerVoteData(ulong id, string displayName)
            {
                Id = id;
                DisplayName = displayName;
                TotalVotes = 0;
                ClaimedVotes = 0;
            }
        }
        #endregion
        #endregion

        #region Help Text
        private void SendHelpText(BasePlayer player)
        {
            Chat(player, " Help Text:\n" +
                "Quick vote makes voting quick and easy\n" +
                "<color=yellow>/vote</color> - see avaliable voting sites for this server\n" +
                "<color=yellow>/vote list</color> - see avaliable rewards",
                "<color=yellow>/claim</color> - claim your reward");
        }
        #endregion
    }
}