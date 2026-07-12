using NOCS.Config;
using NOCS.Core;
using NOCS.Util;
using UnityEngine;

namespace NOCS.HardKill
{
    internal static class HotTriggerGate
    {
        private const float FullOpenToleranceDeg = 180f;
        private const float MinCommittedLaunchDistM = 1f;

        internal static bool IsSalvoLaunchAllowed(
            Aircraft aircraft,
            Missile threat,
            int queueThreatCount,
            WeaponStation? defensiveStation)
        {
            return IsCommittedSalvoLaunchAllowed(aircraft, threat, defensiveStation);
        }

        internal static bool IsCommittedSalvoLaunchAllowed(
            Aircraft aircraft,
            Missile threat,
            WeaponStation? defensiveStation)
        {
            if (!NocsGuard.IsValidUnit(aircraft) || !NocsGuard.IsValidMissile(threat))
                return false;

            Rigidbody? aircraftRb = aircraft!.rb;
            Rigidbody? threatRb = threat!.rb;
            if (aircraftRb == null || threatRb == null)
                return false;

            float dist = (aircraftRb.position - threatRb.position).magnitude;
            if (dist < MinCommittedLaunchDistM)
                return false;

            if (defensiveStation == null)
                return true;

            return InterceptGeometry.IsInEnvelope(dist, defensiveStation);
        }

        internal static bool IsAutoLaunchAllowed(
            Aircraft aircraft,
            Missile threat,
            int queueThreatCount,
            WeaponStation? defensiveStation)
        {
            if (!NocsGuard.IsValidUnit(aircraft) || !NocsGuard.IsValidMissile(threat))
                return false;

            Rigidbody? aircraftRb = aircraft!.rb;
            Rigidbody? threatRb = threat!.rb;
            if (aircraftRb == null || threatRb == null)
                return false;

            Vector3 toAircraft = aircraftRb.position - threatRb.position;
            float dist = toAircraft.magnitude;
            if (dist < 1f)
                return false;

            float closure = ThreatKinematics.ResolveClosure(
                threatRb.velocity,
                aircraftRb.velocity,
                toAircraft / dist,
                previewMode: true);
            if (closure <= 0f)
                return false;

            int threatCount = queueThreatCount > 0 ? queueThreatCount : 1;
            if (!MwsThreatFilter.PassesMaxRangeGate(dist, closure, threatCount, defensiveStation))
                return false;

            if (defensiveStation == null)
                return true;

            return InterceptGeometry.IsInEnvelope(dist, defensiveStation);
        }

        internal static bool IsLaunchAllowed(
            in SwarmInterceptSample sample,
            WeaponStation station,
            Aircraft aircraft)
        {
            if (!sample.Valid || station == null || aircraft == null)
                return false;

            if (sample.ThreatCount <= 0)
                return false;

            if (!SwarmInterceptGeometry.AllEnvelopesInWeaponRange(station))
                return false;

            if (NocsConfigCache.AseGateToleranceAngle >= FullOpenToleranceDeg)
                return true;

            Vector2 gunCross = ResolveGunCrossScreenPos();
            if (gunCross.x < 0f)
                return false;

            float tolerancePx = ResolveTolerancePx();
            return SwarmInterceptGeometry.GunCrossInsideAllEnvelopes(gunCross, tolerancePx);
        }

        internal static bool CircleContains(in SwarmInterceptSample sample, Vector2 screenPoint)
        {
            if (!sample.Valid || sample.ThreatCount <= 0)
                return false;

            return SwarmInterceptGeometry.GunCrossInsideAllEnvelopes(screenPoint, ResolveTolerancePx());
        }

        internal static float ResolveTolerancePx()
        {
            float angleDeg = NocsConfigCache.AseGateToleranceAngle;
            if (angleDeg <= 0f)
                return 0f;

            if (angleDeg >= FullOpenToleranceDeg)
                return float.PositiveInfinity;

            return NocsScreenScale.RadiansToPx(angleDeg * Mathf.Deg2Rad);
        }

        private static Vector2 ResolveGunCrossScreenPos()
        {
            FlightHud? hud = SceneSingleton<FlightHud>.i;
            if (hud == null || hud.velocityVector == null)
                return new Vector2(-1f, -1f);

            Vector3 pos = hud.velocityVector.transform.position;
            return new Vector2(pos.x, pos.y);
        }
    }
}
