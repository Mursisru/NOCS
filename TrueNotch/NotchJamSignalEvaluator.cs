using NOCS.Config;
using UnityEngine;

namespace NOCS.TrueNotch
{
    /// <summary>
    /// Inverse of vanilla RadarParams.GetSignalStrength for the max |Dot(LOS, velocity)|
    /// at which ECM still drops return below minSignal.
    /// </summary>
    internal static class NotchJamSignalEvaluator
    {
        private const float MaxRadialSpeed = 150f;

        internal static bool TryGetMaxRadialSpeed(in NotchThreatRadarContext ctx, out float maxRadialSpeed)
        {
            maxRadialSpeed = 0f;
            RadarParams p = ctx.RadarParams;
            if (p.dopplerFactor <= 0f || ctx.Distance <= 1f)
                return false;

            float baseSignal = p.maxRange / ctx.Distance * Mathf.Pow(ctx.Rcs, 0.25f);
            baseSignal = Mathf.Min(baseSignal, p.maxSignal);
            baseSignal -= ctx.Clutter * p.clutterFactor;

            if (baseSignal <= ctx.MinSignal)
            {
                maxRadialSpeed = MaxRadialSpeed;
                return true;
            }

            float boosted = Mathf.Lerp(baseSignal, p.maxSignal, 0.5f);
            if (boosted <= 0.0001f)
                return false;

            float ecm = ctx.PreviewEcm;
            if (NocsConfigCache.NotchDopplerBias != 0f)
                ecm -= NocsConfigCache.NotchDopplerBias;

            float maxAllowedBoosted = ctx.MinSignal + ecm;
            if (maxAllowedBoosted <= 0f)
                return false;

            float maxFactor = maxAllowedBoosted / boosted;
            if (maxFactor <= 1f)
            {
                maxRadialSpeed = 0f;
                return true;
            }

            maxRadialSpeed = Mathf.Min((maxFactor - 1f) / p.dopplerFactor, MaxRadialSpeed);
            return true;
        }

        internal static bool IsJamEffective(in NotchThreatRadarContext ctx, Vector3 velocity)
        {
            if (!TryGetMaxRadialSpeed(ctx, out float maxRadial))
                return false;

            float radial = Mathf.Abs(Vector3.Dot(ctx.LosDirection, velocity));
            return radial <= maxRadial;
        }

        internal static float EvaluateSignal(in NotchThreatRadarContext ctx, Vector3 velocity, float ecm)
        {
            RadarParams p = ctx.RadarParams;
            float num = p.maxRange / ctx.Distance * Mathf.Pow(ctx.Rcs, 0.25f);
            num = Mathf.Min(num, p.maxSignal);
            num -= ctx.Clutter * p.clutterFactor;
            if (num <= ctx.MinSignal)
                return num;

            float boosted = Mathf.Lerp(num, p.maxSignal, 0.5f);
            float radial = Mathf.Min(Mathf.Abs(Vector3.Dot(ctx.LosDirection, velocity)), MaxRadialSpeed) * p.dopplerFactor;
            float signal = boosted * (1f + radial) - ecm;
            if (NocsConfigCache.NotchDopplerBias != 0f)
                signal += NocsConfigCache.NotchDopplerBias;
            return signal;
        }
    }
}
