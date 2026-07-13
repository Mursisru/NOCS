using System;
using System.Collections;
using BepInEx.Logging;
using HarmonyLib;
using NOCS.Config;
using NOCS.Core;
using NOCS.HardKill;
using NOCS.TrueNotch;
using NOCS.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NOCS
{
    internal sealed class NocsHost : MonoBehaviour
    {
        private static NocsHost? _instance;
        private Harmony? _harmony;
        private string _pluginDir = string.Empty;
        private ManualLogSource? _logger;
        private bool _missionReady;
        private bool _startupScheduled;

        internal static void Ensure(string pluginDir, ManualLogSource logger)
        {
            if (_instance != null)
                return;

            var go = new GameObject("NOCS.Host");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _instance = go.AddComponent<NocsHost>();
            _instance._pluginDir = pluginDir;
            _instance._logger = logger;
            SceneManager.sceneLoaded += _instance.OnSceneLoaded;
            _instance.TryBootstrapCurrentScene();
        }

        private void OnDestroy()
        {
            try
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                NocsAircraftBinder.SafeUnbind();
                HardKillController.DisposeViews();
                TrueNotchHudDriver.DisposeViews();
                _harmony?.UnpatchSelf();
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("NocsHost.OnDestroy", ex);
            }

            _harmony = null;
            if (_instance == this)
                _instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (_missionReady)
                    return;

                if (IsMenuOrSystemScene(scene.path))
                    return;

                ScheduleMissionStartup(scene.path);
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("NocsHost.OnSceneLoaded", ex);
            }
        }

        private void TryBootstrapCurrentScene()
        {
            try
            {
                Scene scene = SceneManager.GetActiveScene();
                if (!IsMenuOrSystemScene(scene.path))
                    ScheduleMissionStartup(scene.path);
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("NocsHost.TryBootstrapCurrentScene", ex);
            }
        }

        private void ScheduleMissionStartup(string scenePath)
        {
            if (_missionReady || _startupScheduled)
                return;

            _startupScheduled = true;
            StartCoroutine(DeferredMissionStartup(scenePath));
        }

        private IEnumerator DeferredMissionStartup(string scenePath)
        {
            yield return null;
            if (_missionReady)
                yield break;

            _missionReady = true;
            StartupMission(scenePath);
        }

        private void LateUpdate()
        {
            if (!_missionReady)
                return;

            try
            {
                NocsHudBootstrap.EnsureAttached();

                NocsHudRoot? root = NocsHudRoot.Instance;
                if (root != null)
                {
                    root.RunTick(Time.deltaTime);
                    return;
                }

                TrueNotchHudDriver.RunTick(Time.deltaTime);
                HardKillController.RunTick(Time.deltaTime);
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("NocsHost.LateUpdate", ex);
            }
        }

        private void StartupMission(string scenePath)
        {
            try
            {
                NocsConfigCache.RefreshFromBepIn();
                ApplyHarmonyPatches();
                NocsHudBootstrap.EnsureAttached();
                _logger?.LogInfo("NOCS host ready (mission) v" + AppVersion.DisplayVersion + " scene=" + scenePath);
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("NocsHost.StartupMission", ex);
                _logger?.LogError("NOCS mission startup failed: " + ex.Message);
            }
        }

        private void ApplyHarmonyPatches()
        {
            if (_harmony != null)
                return;

            _harmony = new Harmony(NocsPlugin.PluginGuid);
            _harmony.PatchAll(typeof(NocsPlugin).Assembly);
            _logger?.LogInfo("Harmony patches applied.");
        }

        private static bool IsMenuOrSystemScene(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            string lower = path.ToLowerInvariant();
            return lower.Contains("mainmenu")
                || lower.Contains("boot")
                || lower.Contains("loading")
                || lower.Contains("splash");
        }
    }
}
