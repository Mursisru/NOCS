using System;
using HarmonyLib;
using NOCS.Core;
using NOCS.Util;

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

        [HarmonyFinalizer]
        private static Exception? Finalizer(Exception? __exception)
        {
            if (__exception != null)
                NocsDiagLog.ExceptionOnce("CombatHUD.SetAircraft", __exception);
            return null;
        }
    }
}
