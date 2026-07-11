using HarmonyLib;
using NOCS.Core;

namespace NOCS.Core.Patches
{
    [HarmonyPatch(typeof(FlightHud), "Awake")]
    internal static class FlightHud_Awake_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(FlightHud __instance)
        {
            if (__instance?.gameObject == null)
                return;

            NocsHudBootstrap.EnsureAttached();
        }
    }
}
