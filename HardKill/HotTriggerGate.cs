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
        private const float PotentialHitScreenExtraPx = 28f;
        private const float PotentialHitWorldExtraDeg = 14f;
        private const float MinPotentialManeuverSec = 0.4f;

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

            if (dist < NocsConfigCache.MinLaunchRangeMeters)
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
            return IsShootWindowOpen(in sample, defensiveStation, aircraft, allowSalvoOnlyFallback: true);
        }

        /// <summary>
        /// Actionable pre-SHOOT cue: RWR threat, weapon reach, maneuver time left, aim approaching envelopes.
        /// </summary>
        internal static bool IsPotentialHitCueActive(
            in SwarmInterceptSample sample,
            WeaponStation? defensiveStation,
            Aircraft aircraft)
        {
            if (IsAseShootCueActive(in sample, defensiveStation, aircraft))
                return false;

            if (!MwsRwrGate.HasRwrPicture(aircraft))
                return false;

            if (!sample.Valid || sample.ThreatCount <= 0)
                return false;

            if (!HasAllocatableSalvoThreat(aircraft))
                return false;

            if (defensiveStation != null && !SwarmInterceptGeometry.AnyEnvelopeInWeaponRange(defensiveStation))
                return false;

            return IsApproachingShootGeometry(in sample, defensiveStation, aircraft);
        }

        /// <summary>
        /// Hotkey / auto-continue gate — identical to SHOOT cue.
        /// </summary>
        internal static bool IsSalvoTriggerAllowed(
            in SwarmInterceptSample sample,
            WeaponStation? defensiveStation,
            Aircraft aircraft)
        {
            return IsShootWindowOpen(in sample, defensiveStation, aircraft, allowSalvoOnlyFallback: true);
        }

        /// <summary>
        /// AutoEngage — strict ASE sample only; never salvo-only world-aim bypass.
        /// </summary>
        internal static bool IsAutoEngageTriggerAllowed(
            in SwarmInterceptSample sample,
            WeaponStation? defensiveStation,
            Aircraft aircraft)
        {
            if (!NocsConfigCache.AutoEngage)
                return false;

            return IsShootWindowOpen(in sample, defensiveStation, aircraft, allowSalvoOnlyFallback: false);
        }

        /// <summary>
        /// Manual hotkey salvo may continue when ASE preview drops a queued threat mid-session.
        /// </summary>
        internal static bool IsActiveSalvoContinueAllowed(Aircraft aircraft)
        {
            return NocsGuard.IsLocalPlayerAircraft(aircraft) && HasAllocatableSalvoThreat(aircraft);
        }

        internal static bool IsLaunchAllowed(
            in SwarmInterceptSample sample,
            WeaponStation station,
            Aircraft aircraft)
        {
            return IsShootWindowOpen(in sample, station, aircraft, allowSalvoOnlyFallback: true);
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
            Aircraft aircraft,
            bool allowSalvoOnlyFallback)
        {
            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
                return false;

            if (!MwsRwrGate.HasRwrPicture(aircraft))
                return false;

            if (!HasAllocatableSalvoThreat(aircraft))
                return false;

            if (sample.Valid && sample.ThreatCount > 0)
            {
                if (IsFireControlOpen())
                    return true;

                if (defensiveStation != null && !SwarmInterceptGeometry.AllEnvelopesInWeaponRange(defensiveStation))
                    return false;

                if (!IsAimInsideAllEnvelopes(aircraft))
                    return false;

                return true;
            }

            if (!allowSalvoOnlyFallback)
                return false;

            if (IsFireControlOpen())
                return true;

            return IsWorldAimSalvoOpen(aircraft);
        }

        private static bool IsFireControlOpen()
        {
            return NocsConfigCache.ManualLaunchAimTolerance >= FullOpenToleranceDeg;
        }

        private static bool IsAimInsideAllEnvelopes(Aircraft aircraft)
        {
            if (NocsCameraContext.PreferWorldShootAim())
                return IsWorldAimInsideAllEnvelopes(aircraft);

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

        private static bool IsApproachingShootGeometry(
            in SwarmInterceptSample sample,
            WeaponStation? defensiveStation,
            Aircraft aircraft)
        {
            if (sample.MinTimeToImpact < ResolveMinPotentialManeuverSec(defensiveStation))
                return false;

            Missile? urgent = sample.UrgentThreat;
            if (!NocsGuard.IsValidMissile(urgent))
                return false;

            PersistentID urgentId = urgent!.persistentID;
            if (ThreatEngagementLedger.WasEngaged(urgentId))
                return false;

            if (MwsThreatFilter.IsInsideArmDeadZone(urgent, aircraft, defensiveStation))
                return false;

            Rigidbody? aircraftRb = aircraft.rb;
            Rigidbody? threatRb = urgent!.rb;
            if (aircraftRb == null || threatRb == null)
                return false;

            float dist = (aircraftRb.position - threatRb.position).magnitude;
            if (dist < NocsConfigCache.MinLaunchRangeMeters)
                return false;

            float shootTolPx = ResolveTolerancePx();
            float potentialTolPx = shootTolPx + NocsScreenScale.Px(PotentialHitScreenExtraPx);

            if (NocsCameraContext.PreferWorldShootAim())
            {
                return IsWorldAimInsideExpandedEnvelopes(aircraft, shootTolPx, potentialTolPx)
                    && !IsWorldAimInsideAllEnvelopes(aircraft);
            }

            Vector2 gunCross = ResolveGunCrossScreenPos();
            if (gunCross.x >= 0f)
            {
                if (SwarmInterceptGeometry.GunCrossInsideAllEnvelopes(gunCross, shootTolPx))
                    return false;

                return SwarmInterceptGeometry.GunCrossInsideAllEnvelopes(gunCross, potentialTolPx);
            }

            if (NocsConfigCache.RequireAseScreenShoot)
                return false;

            return IsWorldAimInsideExpandedEnvelopes(aircraft, shootTolPx, potentialTolPx)
                && !IsWorldAimInsideAllEnvelopes(aircraft);
        }

        private static float ResolveMinPotentialManeuverSec(WeaponStation? defensiveStation)
        {
            MsnParams msn = SeekerParamCache.GetMsnParams(defensiveStation);
            return msn.ArmDelaySec + msn.GuidanceDelaySec + MinPotentialManeuverSec;
        }

        private static bool IsWorldAimInsideExpandedEnvelopes(
            Aircraft aircraft,
            float shootTolPx,
            float potentialTolPx)
        {
            if (!TryResolveWorldAimDirection(aircraft, out Vector3 aimDir))
                return false;

            float shootTolRad = NocsScreenScale.PxToRadians(shootTolPx);
            float potentialTolRad = NocsScreenScale.PxToRadians(potentialTolPx);
            float worldExtraRad = PotentialHitWorldExtraDeg * Mathf.Deg2Rad;
            bool found = false;

            for (int i = 0; i < SwarmInterceptGeometry.MaxThreats; i++)
            {
                if (!SwarmInterceptGeometry.TryGetEnvelope(i, out ThreatEnvelope envelope))
                    break;

                if (!envelope.Valid || !NocsGuard.IsValidMissile(envelope.Threat))
                    continue;

                found = true;
                if (IsWorldAimInsideThreatEnvelope(aircraft, envelope.Threat, aimDir, envelope, shootTolRad))
                    return false;

                if (!IsWorldAimInsideThreatEnvelope(
                        aircraft,
                        envelope.Threat,
                        aimDir,
                        envelope,
                        potentialTolRad + worldExtraRad))
                    return false;
            }

            return found;
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

        private static bool IsWorldAimSalvoOpen(Aircraft aircraft)
        {
            if (!MwsRwrGate.HasRwrPicture(aircraft))
                return false;

            if (NocsConfigCache.RequireAseScreenShoot)
                return false;

            if (!TryResolveWorldAimDirection(aircraft, out Vector3 aimDir))
                return false;

            float toleranceRad = ResolveWorldAimToleranceRad();
            IReadOnlyList<Missile> live = MwsThreatFilter.GetSalvoScratch(aircraft);
            bool found = false;
            for (int i = 0; i < live.Count; i++)
            {
                Missile threat = live[i];
                if (!NocsGuard.IsValidMissile(threat))
                    continue;

                if (ThreatEngagementLedger.WasEngaged(threat.persistentID))
                    continue;

                Rigidbody? aircraftRb = aircraft.rb;
                Rigidbody? threatRb = threat.rb;
                if (aircraftRb == null || threatRb == null)
                    return false;

                found = true;
                Vector3 toThreat = threatRb.position - aircraftRb.position;
                float dist = toThreat.magnitude;
                if (dist < MinCommittedLaunchDistM)
                    return false;

                Vector3 los = toThreat / dist;
                float aimErrorRad = Vector3.Angle(aimDir, los) * Mathf.Deg2Rad;
                if (aimErrorRad > toleranceRad)
                    return false;
            }

            return found;
        }

        private static float ResolveWorldAimToleranceRad()
        {
            float angleDeg = NocsConfigCache.ManualLaunchAimTolerance;
            if (angleDeg <= 0f)
                return 0f;

            if (angleDeg >= FullOpenToleranceDeg)
                return float.MaxValue;

            return angleDeg * Mathf.Deg2Rad;
        }

        private static bool HasAllocatableSalvoThreat(Aircraft aircraft)
        {
            if (!MwsRwrGate.HasRwrPicture(aircraft))
                return false;

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
            if (NocsCameraContext.PreferWorldShootAim())
                return new Vector2(-1f, -1f);

            FlightHud? hud = SceneSingleton<FlightHud>.i;
            if (hud == null || hud.velocityVector == null)
                return new Vector2(-1f, -1f);

            Vector3 pos = hud.velocityVector.transform.position;
            return new Vector2(pos.x, pos.y);
        }
    }
}
