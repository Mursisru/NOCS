using System.IO;
using BepInEx;
using BepInEx.Logging;
using NOCS.Config;

namespace NOCS
{
    [BepInPlugin(PluginGuid, PluginName, AppVersion.BepInSemVer)]
    public sealed class NocsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.at747.nocs.bepinex";
        public const string PluginName = "NO Countermeasures Supporter";

        internal static ManualLogSource? ModLogger { get; private set; }

        private void Awake()
        {
            ModLogger = base.Logger;

            NocsBepInConfig.Bind(Config);
            NocsConfigCache.BindFromBepIn(NocsBepInConfig.Snapshot());

            string? pluginDir = Path.GetDirectoryName(Info.Location);
            if (string.IsNullOrEmpty(pluginDir))
            {
                ModLogger.LogError("Could not resolve plugin directory.");
                return;
            }

            NocsHost.Ensure(pluginDir, ModLogger);
            ModLogger.LogInfo($"{PluginName} {AppVersion.DisplayVersion} loaded.");
        }

        private void Update()
        {
            NocsConfigurationManagerBridge.TryToggleFallbackHotkey();
        }
    }
}
