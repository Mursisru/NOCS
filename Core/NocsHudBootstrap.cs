using System;
using NOCS.Util;
using UnityEngine;

namespace NOCS.Core
{
    internal static class NocsHudBootstrap
    {
        internal static void EnsureAttached()
        {
            try
            {
                GameObject? host = ResolveHostObject();
                if (host == null)
                    return;

                if (host.GetComponent<NocsHudRoot>() == null)
                    host.AddComponent<NocsHudRoot>();
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("NocsHudBootstrap.EnsureAttached", ex);
            }
        }

        private static GameObject? ResolveHostObject()
        {
            FlightHud? flightHud = SceneSingleton<FlightHud>.i;
            if (flightHud != null)
                return flightHud.gameObject;

            CombatHUD? combatHud = SceneSingleton<CombatHUD>.i;
            return combatHud != null ? combatHud.gameObject : null;
        }
    }
}
