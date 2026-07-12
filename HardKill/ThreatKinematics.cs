using NOCS.Config;
using UnityEngine;

namespace NOCS.HardKill
{
    internal static class ThreatKinematics
    {
        private const float PreviewCpaDistScale = 6f;
        private const float PreviewCpaPerMeter = 0.012f;
        private const float PreviewMinClosingMps = 10f;
        private const float PreviewHeadOnFloor = 0.35f;

        internal static float ResolveClosure(
            Vector3 missileVel,
            Vector3 acVel,
            Vector3 losToAircraft,
            bool previewMode)
        {
            float relative = Vector3.Dot(missileVel - acVel, losToAircraft);
            if (relative > 0f)
                return relative;

            if (!previewMode)
                return relative;

            float inbound = Vector3.Dot(missileVel, losToAircraft);
            return Mathf.Max(inbound, NocsConfigCache.ClosureMinThreshold);
        }

        internal static float ResolvePreviewClosure(
            Missile missile,
            Vector3 missileVel,
            Vector3 acVel,
            Vector3 losToAircraft)
        {
            float relative = Vector3.Dot(missileVel - acVel, losToAircraft);
            float missileInbound = Vector3.Dot(missileVel, losToAircraft);
            float speedNow = missileVel.magnitude;
            float headOn = speedNow > 1f
                ? Mathf.Clamp01(Vector3.Dot(missileVel / speedNow, losToAircraft))
                : PreviewHeadOnFloor;

            float closing = relative;
            if (closing < NocsConfigCache.ClosureMinThreshold)
                closing = Mathf.Max(missileInbound, closing);

            float projectedInbound = Mathf.Max(missileInbound, speedNow * Mathf.Max(headOn, PreviewHeadOnFloor));
            closing = Mathf.Max(closing, projectedInbound);

            if (closing < PreviewMinClosingMps)
            {
                float targetApproach = Vector3.Dot(acVel, -losToAircraft);
                closing = Mathf.Max(closing, speedNow * PreviewHeadOnFloor, PreviewMinClosingMps);
                float maxClosing = speedNow + Mathf.Max(0f, targetApproach);
                if (maxClosing > PreviewMinClosingMps)
                    closing = Mathf.Min(closing, maxClosing);
            }

            if (missile != null && missile.EngineOn() && missile.timeSinceSpawn < 3f)
            {
                float boostClosing = Mathf.Max(projectedInbound, speedNow * Mathf.Max(headOn, PreviewHeadOnFloor));
                closing = Mathf.Max(closing, boostClosing);
            }

            return Mathf.Max(closing, NocsConfigCache.ClosureMinThreshold);
        }

        internal static float ResolvePreviewCpaLimit(float dist, float maxCpa)
        {
            float scaled = maxCpa + dist * PreviewCpaPerMeter;
            float appearDist = NocsConfigCache.AsePreviewAppearDistanceM;
            if (dist <= appearDist)
                scaled = Mathf.Max(scaled, maxCpa * 8f, appearDist * 0.05f);

            return scaled;
        }

        internal static bool PassesPreviewAppearDistance(float dist)
        {
            return dist <= NocsConfigCache.AsePreviewAppearDistanceM;
        }

        internal static bool TryResolveCpaMeters(
            Vector3 toAircraft,
            Vector3 relVel,
            float dist,
            bool previewMode,
            float maxCpa,
            out float cpaDist)
        {
            float effectiveMaxCpa = previewMode
                ? ResolvePreviewCpaLimit(dist, maxCpa)
                : maxCpa;

            float velSqr = relVel.sqrMagnitude;
            if (previewMode && velSqr < 0.001f)
            {
                cpaDist = dist;
                return cpaDist <= effectiveMaxCpa || PassesPreviewAppearDistance(dist);
            }

            if (velSqr < 0.001f)
            {
                cpaDist = dist;
                return false;
            }

            float tCpa = -Vector3.Dot(toAircraft, relVel) / velSqr;
            if (tCpa < 0f)
            {
                cpaDist = dist;
                return previewMode
                    && (PassesPreviewAppearDistance(dist) || dist <= effectiveMaxCpa * PreviewCpaDistScale);
            }

            cpaDist = (toAircraft + relVel * tCpa).magnitude;
            if (cpaDist <= effectiveMaxCpa)
                return true;

            return previewMode
                && (PassesPreviewAppearDistance(dist) || dist <= effectiveMaxCpa * PreviewCpaDistScale);
        }
    }
}
