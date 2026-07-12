using System.Collections.Generic;
using NOCS.Config;
using NOCS.Core;
using NOCS.Util;
using UnityEngine;

namespace NOCS.HardKill
{
    /// <summary>
    /// Single fire-control authority: SHOOT window math is shared by HUD cue and salvo trigger.
    /// Committed per-missile launch stays soft (already gated by the shoot window).
    /// </summary>
    internal static class HotTriggerGate
    {
        private const float FullOpenToleranceDeg = 180f;
        private const float MinCommittedLaunchDistM = 1f;
        private const float MinWorldAimSpeedSqr = 1f;

        internal static bool IsSalvoLaunchAllowed(
            Aircraft aircraft,
            Missile threat,
            int queueThreatCount,
            WeaponStation? defensiveStation)
        {
            return IsCommittedSalvoLaunchAllowed(aircraft, threat, defensiveStation);
        }

        /// <summary>
        /// Soft per-threat gate after the swarm shoot window already passed.
        /// Does not re-apply ASE aim / arm / absolute min-range (those caused silent no-fires).
        /// </summary>
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

            if (defensiveStation != null && !InterceptGeometry.IsInEnvelope(dist, defensiveStation))
                return false;

            return true;
        }

        internal static bool IsAutoLaunchAllowed(
            Aircraft aircraft,
            Missile threat,
            int queueThreatCount,
            WeaponStation? defensiveStation)
        {
            if (!IsCommittedSalvoLaunchAllowed(aircraft, threat, defensiveStation))
                return false;

            Rigidbody aircraftRb = aircraft!.rb!;
            Rigidbody threatRb = threat!.rb!;
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
            return MwsThreatFilter.PassesMaxRangeGate(dist, closure, threatCount, defensiveStation);
        }

        /// <summary>
        /// HUD SHOOT cue — identical geometry to the salvo trigger.
        /// </summary>
        internal static bool IsAseShootCueActive(
            in SwarmInterceptSample sample,
            WeaponStation? defensiveStation,
            Aircraft aircraft)
        {
            return IsShootWindowOpen(in sample, defensiveStation, aircraft);
        }

        /// <summary>
        /// Hotkey / auto-continue gate — identical to SHOOT cue.
        /// </summary>
        internal static bool IsSalvoTriggerAllowed(
            in SwarmInterceptSample sample,
            WeaponStation? defensiveStation,
            Aircraft aircraft)
        {
            return IsShootWindowOpen(in sample, defensiveStation, aircraft);
        }

        internal static bool IsLaunchAllowed(
            in SwarmInterceptSample sample,
            WeaponStation station,
            Aircraft aircraft)
        {
            return IsShootWindowOpen(in sample, station, aircraft);
        }

        internal static bool CircleContains(in SwarmInterceptSample sample, Vector2 screenPoint)
        {
            if (!sample.Valid || sample.ThreatCount <= 0)
                return false;

            return SwarmInterceptGeometry.GunCrossInsideAllEnvelopes(screenPoint, ResolveTolerancePx());
        }

        internal static float ResolveTolerancePx()
        {
            float angleDeg = NocsConfigCache.ManualLaunchAimTolerance;
            if (angleDeg <= 0f)
                return 0f;

            if (angleDeg >= FullOpenToleranceDeg)
                return float.PositiveInfinity;

            return NocsScreenScale.RadiansToPx(angleDeg * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Canonical SHOOT window: valid ASE sample, weapon reach, aim inside all envelopes
        /// (screen gun-cross, or world velocity/nose when screen aim is unavailable),
        /// and at least one unengaged salvo threat remaining.
        /// Independent of whether the HUD cue is rendered.
        /// </summary>
        private static bool IsShootWindowOpen(
            in SwarmInterceptSample sample,
            WeaponStation? defensiveStation,
            Aircraft aircraft)
        {
            if (!sample.Valid || sample.ThreatCount <= 0)
                return false;

            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
                return false;

            if (IsFireControlOpen())
                return HasAllocatableSalvoThreat(aircraft);

            if (defensiveStation != null && !SwarmInterceptGeometry.AllEnvelopesInWeaponRange(defensiveStation))
                return false;

            if (!IsAimInsideAllEnvelopes(aircraft))
                return false;

            return HasAllocatableSalvoThreat(aircraft);
        }

        private static bool IsFireControlOpen()
        {
            return NocsConfigCache.ManualLaunchAimTolerance >= FullOpenToleranceDeg;
        }

        private static bool IsAimInsideAllEnvelopes(Aircraft aircraft)
        {
            Vector2 gunCross = ResolveGunCrossScreenPos();
            if (gunCross.x >= 0f)
            {
                if (SwarmInterceptGeometry.GunCrossInsideAllEnvelopes(gunCross, ResolveTolerancePx()))
                    return true;

                // Screen aim present but outside envelopes → not SHOOT (no world override).
                return false;
            }

            if (NocsConfigCache.RequireAseScreenShoot)
                return false;

            return IsWorldAimInsideAllEnvelopes(aircraft);
        }

        private static bool IsWorldAimInsideAllEnvelopes(Aircraft aircraft)
        {
            if (!TryResolveWorldAimDirection(aircraft, out Vector3 aimDir))
                return false;

            float toleranceRad = NocsScreenScale.PxToRadians(ResolveTolerancePx());
            bool found = false;
            for (int i = 0; i < SwarmInterceptGeometry.MaxThreats; i++)
            {
                if (!SwarmInterceptGeometry.TryGetEnvelope(i, out ThreatEnvelope envelope))
                    break;

                if (!envelope.Valid || !NocsGuard.IsValidMissile(envelope.Threat))
                    continue;

                found = true;
                if (!IsWorldAimInsideThreatEnvelope(aircraft, envelope.Threat, aimDir, envelope, toleranceRad))
                    return false;
            }

            return found;
        }

        private static bool IsWorldAimInsideThreatEnvelope(
            Aircraft aircraft,
            Missile threat,
            Vector3 aimDir,
            in ThreatEnvelope envelope,
            float toleranceRad)
        {
            Rigidbody? aircraftRb = aircraft.rb;
            Rigidbody? threatRb = threat.rb;
            if (aircraftRb == null || threatRb == null)
                return false;

            Vector3 toThreat = threatRb.position - aircraftRb.position;
            float dist = toThreat.magnitude;
            if (dist < MinCommittedLaunchDistM)
                return false;

            Vector3 los = toThreat / dist;
            float aimErrorRad = Vector3.Angle(aimDir, los) * Mathf.Deg2Rad;
            float envelopeRad = NocsScreenScale.PxToRadians(envelope.ScreenRadiusPx) + toleranceRad;
            return aimErrorRad <= envelopeRad;
        }

        private static bool HasAllocatableSalvoThreat(Aircraft aircraft)
        {
            IReadOnlyList<Missile> live = MwsThreatFilter.GetSalvoScratch(aircraft);
            for (int i = 0; i < live.Count; i++)
            {
                Missile threat = live[i];
                if (!NocsGuard.IsValidMissile(threat))
                    continue;

                if (!ThreatEngagementLedger.WasEngaged(threat.persistentID))
                    return true;
            }

            return false;
        }

        private static bool TryResolveWorldAimDirection(Aircraft aircraft, out Vector3 aimDir)
        {
            aimDir = Vector3.zero;
            if (!NocsGuard.IsValidUnit(aircraft))
                return false;

            Rigidbody? rb = aircraft!.rb;
            if (rb != null && rb.velocity.sqrMagnitude >= MinWorldAimSpeedSqr)
            {
                aimDir = rb.velocity.normalized;
                return true;
            }

            aimDir = aircraft.transform.forward;
            return aimDir.sqrMagnitude > 0.0001f;
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
