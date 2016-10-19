using System.IO;
using Oxide.Game.Rust.Cui;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("ServerNameGui", "MJSU", "0.0.1")]
    [Description("Displays the server name in a gui on the players screen")]
    class ServerNameGui : RustPlugin
    {
        #region Class Fields
        private PluginConfig _pluginConfig; //Plugin Config
        private const string GuiContainerName = "ServerNameGui_Container";
        private CuiElementContainer _container;
        private uint _iconId;
        #endregion

        #region Setup & Loading
        private void Loaded()
        {
            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig, true);

            LoadImage();
            CreateServerGui();
            LoadUiForConnectedPlayers();
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Creates the UI to be sent to all players
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void CreateServerGui()
        {
            _container = UICreator.CreateElementContainer(GuiContainerName, "1 0.95 0.875 0.025", ".0095 .1", ".1214 .135", 0f, 0f);
            if(_iconId != 0) UICreator.LoadImage(ref _container, GuiContainerName, $"{_iconId}", ".02 .10", ".125 .8");
            UICreator.CreateLabel(ref _container, GuiContainerName, ".8 .8 .8 .8", _pluginConfig.DisplayName, 16, ".155 0", "1 1");
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Loops over the players and adds the UI to their screen
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void LoadUiForConnectedPlayers()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.AddUi(player, _container);
            }
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
                DisplayName = config?.DisplayName ?? $"{ConVar.Server.hostname}",
            };
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, GuiContainerName);
            }
            FileStorage.server.RemoveEntityNum(_iconId, _iconId);
        }
        #endregion

        [ChatCommand("abc")]
        private void stuff(BasePlayer player, string command, string[] args)
        {
            CuiHelper.AddUi(player, _container);
        }

        #region Oxide Hooks
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// When player connects add the GUI to their screen
        /// </summary>
        /// <param name="player"></param>
        /// ////////////////////////////////////////////////////////////////////////
        void OnPlayerInit(BasePlayer player)
        {
            CuiHelper.AddUi(player, _container);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// When plays disconnects remove the GUI from their screen
        /// </summary>
        /// <param name="player"></param>
        /// ////////////////////////////////////////////////////////////////////////
        private void OnPlayerDisconnected(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, GuiContainerName);
        }
        #endregion

        private void LoadImage()
        {
            using (var www = new WWW("http://www.newdesignfile.com/postpic/2010/03/home-icon-white_338306.png"))
            {
                while (!www.isDone) { }

                if (string.IsNullOrEmpty(www.error))
                {
                    var stream = new MemoryStream();
                    stream.Write(www.bytes, 0, www.bytes.Length);
                    _iconId = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue-1001);
                }
                else
                {
                    Debug.Log("Error downloading img");
                    ConsoleSystem.Run.Server.Normal("oxide.unload ServerNameGui");
                }
            }
        }

        #region Classes
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugin Config class
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        class PluginConfig
        {
            public string DisplayName { get; set; }
        }
        #endregion

        #region UICreator Class
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once ClassNeverInstantiated.Local
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// @credit to k1lly0u - code from ServerRewards
        /// UI class for SurveyInfo
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private class UICreator
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, float fadeIn, float fadeOut)
            {
                var newElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color , FadeIn = fadeIn},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = false,
                            FadeOut = fadeOut
                        },
                        new CuiElement().Parent = "Hud",
                        panelName
                    }
                };
                return newElement;
            }
            public static void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel, CuiHelper.GetGuid());

            }
            public static void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
        }
        #endregion
    }
}
