using System;
using NOCS.HardKill;
using NOCS.TrueNotch;
using NOCS.Util;
using UnityEngine;

namespace NOCS.Core
{
    internal sealed class NocsHudRoot : MonoBehaviour
    {
        internal static NocsHudRoot? Instance { get; private set; }

        private int _lastTickFrame = -1;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            try
            {
                TrueNotchHudDriver.DisposeViews();
                HardKillController.DisposeViews();
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("NocsHudRoot.OnDestroy", ex);
            }
        }

        private void LateUpdate()
        {
            RunTick(Time.deltaTime);
        }

        internal void RunTick(float dt)
        {
            int frame = Time.frameCount;
            if (frame == _lastTickFrame)
                return;
            _lastTickFrame = frame;

            // Isolate subsystems — one failure must not skip the other.
            try
            {
                TrueNotchHudDriver.RunTick(dt);
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("NocsHudRoot.TrueNotch", ex);
            }

            try
            {
                HardKillController.RunTick(dt);
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("NocsHudRoot.HardKill", ex);
            }
        }
    }
}
