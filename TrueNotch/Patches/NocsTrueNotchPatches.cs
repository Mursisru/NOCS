using HarmonyLib;
using NOCS.Core;
using NOCS.HardKill;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.TrueNotch.Patches
{
    [HarmonyPatch(typeof(ThreatItem), "AnimateItem")]
    internal static class ThreatItem_AnimateItem_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ThreatItem __instance)
        {
            if (__instance == null)
                return;

            Missile? missile = ThreatItemReflection.GetMissile(__instance);
            if (!NocsGuard.IsValidMissile(missile))
                return;

            Missile threat = missile!;
            CombatHUD? combatHud = SceneSingleton<CombatHUD>.i;
            Aircraft? aircraft = combatHud?.aircraft;
            if (!NocsGuard.IsLocalPlayerAircraft(aircraft) || aircraft!.rb == null)
                return;

            WarningTtiLabel.Apply(__instance, threat, aircraft);

            if (!SeekerParamCache.IsRadarSeeker(SeekerParamCache.ResolveSeekerKind(threat)))
                return;

            Image? notchBox = ThreatItemReflection.GetNotchIndicatorBox(__instance);
            if (notchBox == null)
                return;

            Vector3 evasionVector = threat.GetEvasionPoint() - aircraft.GlobalPosition();
            NotchTelemetryBridge.Publish(new NotchTelemetrySample
            {
                Valid = true,
                EvasionVector = evasionVector,
                MissileId = threat.persistentID,
            }, notchBox);

            TrueNotchHudDriver.ApplyLive(aircraft, threat, notchBox, evasionVector, Time.deltaTime);
        }
    }
}
