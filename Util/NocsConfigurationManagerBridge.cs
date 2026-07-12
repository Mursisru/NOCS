using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using NOCS.Util;
using UnityEngine;

namespace NOCS
{
    internal static class NocsConfigurationManagerBridge
    {
        private const string ConfigurationManagerGuid = "com.bepis.bepinex.configurationmanager";

        private static object? _instance;
        private static PropertyInfo? _displayingWindow;

        internal static void TryToggleFallbackHotkey()
        {
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                return;

            if (!Input.GetKeyDown(KeyCode.F10))
                return;

            if (!TryEnsureInstance())
                return;

            bool current = (bool)_displayingWindow!.GetValue(_instance, null)!;
            _displayingWindow.SetValue(_instance, !current, null);
            NocsDiagLog.Info($"Configuration Manager toggled via Ctrl+F10 -> {!current}");
        }

        private static bool TryEnsureInstance()
        {
            if (_instance != null && _displayingWindow != null)
                return true;

            if (Chainloader.PluginInfos == null)
                return false;

            foreach (PluginInfo pluginInfo in Chainloader.PluginInfos.Values)
            {
                if (pluginInfo.Metadata.GUID != ConfigurationManagerGuid)
                    continue;

                _instance = pluginInfo.Instance;
                if (_instance == null)
                    return false;

                _displayingWindow = _instance.GetType().GetProperty("DisplayingWindow");
                return _displayingWindow != null;
            }

            return false;
        }
    }
}
