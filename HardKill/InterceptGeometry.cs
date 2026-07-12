using NOCS.Util;
using NOCS.Config;
using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    /// <summary>
    /// ASE envelope around the threat HUD marker.
    /// Velocity-vector inside the circle → intercept still guaranteed.
    /// Outside → defensive missile cannot recover the miss before impact.
    /// Size follows terminal maneuver budget: shrinks only when remaining
    /// guidance time falls below MaxManeuverWindow.
    /// </summary>
    internal static class InterceptGeometry
    {
        private const float GravityMps2 = 9.81f;
        private const float OperationalGHeadroom = 1.35f;
        private const float TurnRateDegPerSec = 35f;
        private const float MaxRadiusScreenFrac = 0.28f;
        private const float MinScreenDiameterRefPx = 72f;
        private const float MergeFloorDiameterRefPx = 24f;
        private const float ConfidenceSlackGain = 8f;
        private const float LeadSlackFactor = 0.85f;
        private const float TurnSlackFactor = 0.85f;
        private const float DefensiveReachSpeedFactor = 0.65f;
        private const float DefensiveReachTimeSlackSec = 0.5f;

        internal struct InterceptSample
        {
            internal bool Valid;
            internal float TimeToImpact;
            internal float ClosureMps;
            internal float DistanceMeters;
            internal float MissBudgetMeters;
            internal float ScreenRadiusPx;
            internal float ScreenDiameterPx;
            internal Vector2 ScreenCenter;
        }

        internal static InterceptSample Compute(
            Aircraft aircraft,
            Missile threat,
            WeaponStation? defensiveStation)
        {
            return ComputeRaw(aircraft, threat, defensiveStation, applyMinDiameter: true);
        }

        internal static InterceptSample ComputeRaw(
            Aircraft aircraft,
            Missile threat,
            WeaponStation? defensiveStation,
            bool applyMinDiameter)
        {
            InterceptSample sample = default;
            if (!NocsGuard.IsValidUnit(aircraft) || !NocsGuard.IsValidMissile(threat))
                return sample;

            if (aircraft!.rb == null || threat!.rb == null)
                return sample;

            Vector3 acPos = aircraft.transform.position;
            Vector3 thPos = threat!.transform.position;
            Vector3 toAircraft = acPos - thPos;
            float dist = toAircraft.magnitude;
            sample.DistanceMeters = dist;
            if (dist < 1f)
                return sample;

            Vector3 los = toAircraft / dist;
            float closure = ThreatKinematics.ResolvePreviewClosure(
                threat,
                threat.rb.velocity,
                aircraft.rb.velocity,
                los);
            sample.ClosureMps = closure;
            if (closure <= 0f)
                return sample;

            sample.TimeToImpact = dist / closure;

            float tArm = defensiveStation != null
                ? SeekerParamCache.GetArmDelay(defensiveStation)
                : 1f;
            float guidanceDelay = defensiveStation != null
                ? SeekerParamCache.GetGuidanceDelay(defensiveStation)
                : 0.5f;
            float maxLead = defensiveStation != null
                ? SeekerParamCache.GetMaxLead(defensiveStation)
                : 5f;
            float gMax = defensiveStation != null
                ? SeekerParamCache.GetMaxTurnRateG(defensiveStation)
                : NocsConfigCache.DefaultMaxTurnG;
            float trackAngleDeg = defensiveStation != null
                ? SeekerParamCache.GetMaxTrackingAngleDeg(defensiveStation)
                : 60f;
            float defSpeed = defensiveStation != null
                ? SeekerParamCache.GetMaxSpeedMps(defensiveStation)
                : 800f;

            float tReach = dist / Mathf.Max(defSpeed * DefensiveReachSpeedFactor, 150f);
            float tGuidance = Mathf.Max(0f, sample.TimeToImpact - tArm - guidanceDelay);
            float tFly = Mathf.Min(tGuidance, tReach + DefensiveReachTimeSlackSec);
            float tEffective = Mathf.Min(tFly, NocsConfigCache.MaxManeuverWindow);
            float minDiameterPx = applyMinDiameter ? ResolveMinDiameterPx() : 0f;

            if (tEffective <= 0f)
            {
                if (!applyMinDiameter)
                    return BuildMinimumSample(threat, sample, ResolveMergeFloorDiameterPx());

                return BuildMinimumSample(threat, sample, minDiameterPx);
            }

            float gEff = Mathf.Max(gMax, NocsConfigCache.DefaultMaxTurnG) * OperationalGHeadroom;
            float lateral = 0.5f * gEff * GravityMps2 * tEffective * tEffective;
            sample.MissBudgetMeters = lateral;

            float leadSec = Mathf.Min(maxLead, tEffective);
            float thetaKin = lateral / dist;
            float thetaLead = Mathf.Atan(closure * leadSec / dist);
            float thetaTurn = Mathf.Min(trackAngleDeg, TurnRateDegPerSec * tEffective) * Mathf.Deg2Rad;
            float thetaTrack = trackAngleDeg * Mathf.Deg2Rad;

            float thetaEnvelope = Mathf.Max(
                thetaKin,
                thetaLead * LeadSlackFactor,
                thetaTurn * TurnSlackFactor);
            thetaEnvelope = Mathf.Min(thetaEnvelope, thetaTrack);

            float thetaLimit = thetaEnvelope
                * NocsConfigCache.AseSensitivityBias
                * ResolveConfidenceScale(NocsConfigCache.AseInterceptConfidence);

            float radiusPx = NocsScreenScale.RadiansToPx(thetaLimit);
            float maxRadiusPx = Screen.width * MaxRadiusScreenFrac;
            if (radiusPx > maxRadiusPx)
                radiusPx = maxRadiusPx;

            float diameterPx = radiusPx * 2f;
            if (applyMinDiameter && diameterPx < minDiameterPx)
                diameterPx = minDiameterPx;

            sample.ScreenRadiusPx = diameterPx * 0.5f;
            sample.ScreenDiameterPx = diameterPx;

            return FinalizeSample(threat, sample);
        }

        private static float ResolveMinDiameterPx()
        {
            return NocsScreenScale.Px(MinScreenDiameterRefPx);
        }

        private static float ResolveMergeFloorDiameterPx()
        {
            return NocsScreenScale.Px(MergeFloorDiameterRefPx);
        }

        private static InterceptSample BuildMinimumSample(
            Missile threat,
            InterceptSample sample,
            float minDiameterPx)
        {
            sample.MissBudgetMeters = 0f;
            sample.ScreenDiameterPx = minDiameterPx;
            sample.ScreenRadiusPx = minDiameterPx * 0.5f;
            return FinalizeSample(threat, sample);
        }

        private static InterceptSample FinalizeSample(Missile threat, InterceptSample sample)
        {
            if (!TryResolveThreatScreenCenter(threat, out sample.ScreenCenter))
                return sample;

            sample.Valid = sample.ScreenDiameterPx > 0f;
            return sample;
        }

        internal static bool IsInEnvelope(in InterceptSample sample, WeaponStation station)
        {
            if (!sample.Valid || station == null)
                return false;

            WeaponInfo? info = station.WeaponInfo;
            if (info == null)
                return false;

            return sample.DistanceMeters <= info.targetRequirements.maxRange;
        }

        internal static bool IsInEnvelope(float distanceMeters, WeaponStation station)
        {
            if (station == null)
                return false;

            WeaponInfo? info = station.WeaponInfo;
            if (info == null)
                return false;

            return distanceMeters <= info.targetRequirements.maxRange;
        }

        private static bool TryResolveThreatScreenCenter(Missile threat, out Vector2 screenCenter)
        {
            if (CombatHudMarkerPlacement.TryGetThreatMarkerScreenCenter(threat, out screenCenter))
                return true;

            Camera? cam = SceneSingleton<CameraStateManager>.i?.mainCamera;
            if (cam == null)
            {
                screenCenter = default;
                return false;
            }

            Vector3 world = cam.WorldToScreenPoint(threat.transform.position);
            if (world.z <= 0f)
            {
                screenCenter = default;
                return false;
            }

            screenCenter = new Vector2(world.x, world.y);
            return true;
        }

        private static float ResolveConfidenceScale(float interceptConfidence)
        {
            float p = Mathf.Clamp(interceptConfidence, 0.5f, 0.999f);
            return 1f + (1f - p) * ConfidenceSlackGain;
        }
    }
}
