using HarmonyLib;
using NOCS.Core;
using NOCS.HardKill;

namespace NOCS.HardKill.Patches
{
    [HarmonyPatch(typeof(CombatHUD), "SetAircraft")]
    internal static class CombatHUD_SetAircraft_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(CombatHUD __instance, Aircraft aircraft)
        {
            if (__instance == null)
                return;

            NocsHudBootstrap.EnsureAttached();
            NocsAircraftBinder.HandleSetAircraft(aircraft);
        }
    }
}
