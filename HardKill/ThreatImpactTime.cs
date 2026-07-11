using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    internal static class ThreatImpactTime
    {
        private const float GameMinClosingMps = 10f;
        private const float BoostPhaseSec = 3f;
        private const float MinSeekerSpeedMps = 200f;
        private const float MaxDisplaySec = 999.9f;
        private const float RawClosureFloorMps = 0.1f;
        private const float LaunchSpeedBlendSec = 1.25f;

        internal static bool TryEstimateSmoothed(
            Missile missile,
            Aircraft aircraft,
            float dt,
            out float ttiSec,
            out bool impact)
        {
            ttiSec = 0f;
            impact = false;
            if (!TryComputeRawTti(missile, aircraft, out float rawTti))
                return false;

            TtiSmoothingTracker.EnsurePruned();
            return TtiSmoothingTracker.TryFilter(missile.persistentID, rawTti, dt, out ttiSec, out impact);
        }

        internal static bool TryEstimate(Missile missile, Aircraft aircraft, out float ttiSec)
        {
            return TryComputeRawTti(missile, aircraft, out ttiSec);
        }

        private static bool TryComputeRawTti(Missile missile, Aircraft aircraft, out float rawTti)
        {
            rawTti = 0f;
            if (!NocsGuard.IsValidMissile(missile) || !NocsGuard.IsValidUnit(aircraft))
                return false;

            Rigidbody? missileRb = missile!.rb;
            Rigidbody? aircraftRb = aircraft!.rb;
            if (missileRb == null || aircraftRb == null)
                return false;

            Vector3 toAircraft = aircraftRb.position - missileRb.position;
            float dist = toAircraft.magnitude;
            if (dist < 1f)
            {
                rawTti = 0f;
                return true;
            }

            Vector3 los = toAircraft / dist;
            Vector3 mVel = missileRb.velocity;
            Vector3 aVel = aircraftRb.velocity;

            float missileInbound = Vector3.Dot(mVel, los);
            float relativeClosure = Vector3.Dot(mVel - aVel, los);
            float topSpeed = ResolveTopSpeedMps(missile, aircraft);
            float closing = ResolveClosingMps(
                missile,
                dist,
                los,
                mVel,
                aVel,
                missileInbound,
                relativeClosure,
                topSpeed);

            if (closing <= 0f)
                return false;

            rawTti = dist / Mathf.Max(closing, RawClosureFloorMps);
            if (rawTti > MaxDisplaySec)
                rawTti = MaxDisplaySec;

            return true;
        }

        internal static float EstimateOrMax(Missile missile, Aircraft aircraft, Vector3 acPos)
        {
            if (!NocsGuard.IsValidMissile(missile) || !NocsGuard.IsValidUnit(aircraft) || missile!.rb == null)
                return float.MaxValue;

            Vector3 toAircraft = acPos - missile!.transform.position;
            float dist = toAircraft.magnitude;
            if (dist < 1f)
                return 0f;

            Vector3 los = toAircraft / dist;
            Vector3 mVel = missile.rb.velocity;
            Vector3 aVel = aircraft!.rb != null ? aircraft.rb.velocity : Vector3.zero;
            float missileInbound = Vector3.Dot(mVel, los);
            float relativeClosure = Vector3.Dot(mVel - aVel, los);
            float topSpeed = ResolveTopSpeedMps(missile, aircraft);
            float closing = ResolveClosingMps(
                missile,
                dist,
                los,
                mVel,
                aVel,
                missileInbound,
                relativeClosure,
                topSpeed);

            if (closing <= GameMinClosingMps * 0.5f)
                return float.MaxValue;

            return dist / closing;
        }

        private static float ResolveClosingMps(
            Missile missile,
            float dist,
            Vector3 los,
            Vector3 mVel,
            Vector3 aVel,
            float missileInbound,
            float relativeClosure,
            float topSpeed)
        {
            float speedNow = Mathf.Max(missile.speed, mVel.magnitude);
            float headOn = ResolveHeadOnFactor(los, mVel, speedNow);
            float targetApproach = Vector3.Dot(aVel, -los);
            float remainingDv = missile.GetRemainingDeltaV();
            bool motorActive = missile.EngineOn() || remainingDv > 25f;

            float closing = relativeClosure;
            if (closing < GameMinClosingMps)
                closing = Mathf.Max(missileInbound, closing);

            float projectedInbound = Mathf.Max(missileInbound, speedNow * headOn);
            if (motorActive)
                closing = Mathf.Max(closing, projectedInbound);

            if (missile.timeSinceSpawn < BoostPhaseSec && motorActive)
            {
                float boostBlend = Mathf.Clamp01(missile.timeSinceSpawn / BoostPhaseSec);
                float boostClosing = Mathf.Max(
                    projectedInbound,
                    Mathf.Lerp(speedNow, topSpeed, boostBlend) * Mathf.Max(headOn, 0.35f),
                    dist / Mathf.Max(topSpeed, GameMinClosingMps));

                if (missile.timeSinceSpawn < LaunchSpeedBlendSec)
                    boostClosing = Mathf.Max(boostClosing, dist / Mathf.Max(topSpeed, GameMinClosingMps));

                closing = Mathf.Max(closing, boostClosing);
            }

            if (closing < GameMinClosingMps)
            {
                if (!motorActive && missileInbound <= 0f && relativeClosure <= 0f)
                    return 0f;

                closing = Mathf.Max(
                    projectedInbound,
                    speedNow * 0.5f,
                    GameMinClosingMps);
            }

            float maxClosing = topSpeed + Mathf.Max(0f, targetApproach);
            closing = Mathf.Clamp(closing, GameMinClosingMps, Mathf.Max(maxClosing, GameMinClosingMps));
            return closing;
        }

        private static float ResolveTopSpeedMps(Missile missile, Aircraft aircraft)
        {
            float launchAlt = missile.transform.position.y;
            float targetAlt = aircraft.transform.position.y;
            float topSpeed = missile.GetTopSpeed(launchAlt, targetAlt);
            if (topSpeed > GameMinClosingMps)
                return topSpeed;

            float deltaV = missile.CalcDeltaV();
            if (deltaV > GameMinClosingMps)
                return deltaV;

            return Mathf.Max(missile.speed, MinSeekerSpeedMps);
        }

        private static float ResolveHeadOnFactor(Vector3 los, Vector3 mVel, float speedNow)
        {
            if (speedNow < 1f)
                return 0.35f;

            return Mathf.Clamp01(Vector3.Dot(mVel / speedNow, los));
        }
    }
}
