using NOCS.Config;
using UnityEngine;

namespace NOCS.HardKill
{
    internal static class ThreatKinematics
    {
        private const float PreviewCpaDistScale = 6f;

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

        internal static bool TryResolveCpaMeters(
            Vector3 toAircraft,
            Vector3 relVel,
            float dist,
            bool previewMode,
            float maxCpa,
            out float cpaDist)
        {
            float velSqr = relVel.sqrMagnitude;
            if (previewMode && velSqr < 0.001f)
            {
                cpaDist = dist;
                return cpaDist <= maxCpa || dist <= maxCpa * PreviewCpaDistScale;
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
                return previewMode && dist <= maxCpa * PreviewCpaDistScale;
            }

            cpaDist = (toAircraft + relVel * tCpa).magnitude;
            if (cpaDist <= maxCpa)
                return true;

            return previewMode && dist <= maxCpa * PreviewCpaDistScale;
        }
    }
}
