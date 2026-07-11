using NOCS.HardKill;
using NOCS.TrueNotch;
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

            TrueNotchHudDriver.DisposeViews();
            HardKillController.DisposeViews();
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

            TrueNotchHudDriver.RunTick(dt);
            HardKillController.RunTick(dt);
        }
    }
}
