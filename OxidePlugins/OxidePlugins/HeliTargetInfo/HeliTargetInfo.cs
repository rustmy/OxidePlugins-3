using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("MJSU", "MJSU", "0.0.1")]
    [Description("Displays who the heli locks onto")]
    // ReSharper disable once UnusedMember.Global
    class HeliTargetInfo : RustPlugin
    {
        private PluginConfig _pluginConfig;

        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            _pluginConfig = Config.ReadObject<PluginConfig>();

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Locked"] = "<color=yellow>{0}</color> the heli has locked onto you."
            }, this);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(DefaultConfig(), true);
        }

        private PluginConfig DefaultConfig()
        {
            return new PluginConfig
            {
                Prefix = "[<color=yellow>Cupboard Info</color>]",
                Cooldown = new TimeSpan(0, 0, 5, 0)
            };
        }

        private readonly Hash<ulong, DateTime> _heliTargetCooldown = new Hash<ulong, DateTime>();
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        void OnHelicopterTarget(HelicopterTurret turret, BaseCombatEntity entity)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null) return;
            if (!_heliTargetCooldown.ContainsKey(player.userID) || (_heliTargetCooldown.ContainsKey(player.userID) && _heliTargetCooldown[player.userID] < DateTime.Now))
            {
                PrintToChat($"{_pluginConfig.Prefix}  {Lang("Locked", player.UserIDString, player.displayName)}");
                _heliTargetCooldown[player.userID] = DateTime.Now + _pluginConfig.Cooldown;
            }
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        class PluginConfig
        {
            public string Prefix;
            public TimeSpan Cooldown;
        }
    }
}
