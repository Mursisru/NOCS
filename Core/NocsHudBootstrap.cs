using UnityEngine;

namespace NOCS.Core
{
    internal static class NocsHudBootstrap
    {
        internal static void EnsureAttached()
        {
            GameObject? host = ResolveHostObject();
            if (host == null)
                return;

            if (host.GetComponent<NocsHudRoot>() == null)
                host.AddComponent<NocsHudRoot>();
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
