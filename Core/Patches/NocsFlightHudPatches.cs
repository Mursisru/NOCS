using System;
using HarmonyLib;
using NOCS.Util;

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

        [HarmonyFinalizer]
        private static Exception? Finalizer(Exception? __exception)
        {
            if (__exception != null)
                NocsDiagLog.ExceptionOnce("FlightHud.Awake", __exception);
            return null;
        }
    }
}
