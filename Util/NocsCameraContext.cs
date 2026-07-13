using NOCS.Config;
using UnityEngine;

namespace NOCS.Util
{
    internal static class NocsCameraContext
    {
        internal static bool IsCockpitView()
        {
            CameraStateManager? mgr = SceneSingleton<CameraStateManager>.i;
            return mgr != null && mgr.currentState == mgr.cockpitState;
        }

        /// <summary>
        /// Third-person / chase / orbit: screen gun-cross is stale or misaligned (zoom FOV).
        /// Use world velocity/nose aim unless RequireAseScreenShoot forces HUD gate.
        /// </summary>
        internal static bool PreferWorldShootAim()
        {
            if (NocsConfigCache.RequireAseScreenShoot)
                return false;

            return !IsCockpitView();
        }
    }
}
