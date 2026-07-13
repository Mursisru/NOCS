using System.Collections.Generic;
using NOCS.Config;
using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    /// <summary>
    /// Incoming threat seeker policy + per-type nose FOV (radar 50°, IR 80°).
    /// Own interceptor priority is always IR ammo first, then radar — never mixed with the flare gate.
    /// </summary>
    internal static class IncomingThreatPolicy
    {
        internal const float RadarNoseFovDeg = 50f;
        internal const float IrNoseFovDeg = 80f;

        private static readonly float RadarHalfCos =
            Mathf.Cos((RadarNoseFovDeg * 0.5f) * Mathf.Deg2Rad);

        private static readonly float IrHalfCos =
            Mathf.Cos((IrNoseFovDeg * 0.5f) * Mathf.Deg2Rad);

        /// <summary>
        /// Whether an inbound missile is a Hard-Kill target.
        /// IR inbound: when X=IrInterceptBlockedAboveFlares &gt; 0 → flares &lt; X;
        /// when X=0 → EngageIrThreats master only.
        /// </summary>
        internal static bool IsEngageableIncoming(Aircraft aircraft, Missile? missile)
        {
            if (!NocsGuard.IsValidMissile(missile))
                return false;

            SeekerKind kind = SeekerParamCache.ResolveSeekerKind(missile);
            if (SeekerParamCache.IsRadarSeeker(kind))
                return true;

            if (!SeekerParamCache.IsIrSeeker(kind))
                return false;

            return IsIrInboundEngageAllowed(aircraft);
        }

        /// <summary>
        /// X = IrInterceptBlockedAboveFlares.
        /// X &gt; 0: engage inbound IR while remaining flares &lt; X (HUD flare count).
        /// X &lt;= 0: EngageIrThreats only (no flare gate).
        /// </summary>
        internal static bool IsIrInboundEngageAllowed(Aircraft aircraft)
        {
            int threshold = NocsConfigCache.IrInterceptBlockedAboveFlares;
            if (threshold > 0)
            {
                if (!NocsGuard.IsValidUnit(aircraft))
                    return false;

                return AircraftFlareInventory.CountRemainingFlares(aircraft) < threshold;
            }

            return NocsConfigCache.EngageIrThreats;
        }

        internal static bool IsEngageableByRadarStation(Missile? missile)
        {
            if (!NocsGuard.IsValidMissile(missile))
                return false;

            return SeekerParamCache.IsRadarSeeker(SeekerParamCache.ResolveSeekerKind(missile));
        }

        /// <summary>IR pylon phase may engage any RWR threat allowed by policy (IR + ARH/SARH).</summary>
        internal static bool IsEngageableByIrPhase(Aircraft aircraft, Missile? missile)
        {
            return IsEngageableIncoming(aircraft, missile);
        }

        internal static bool HasIrStationAmmo(IReadOnlyList<WeaponStationEntry> stations)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                WeaponStationEntry entry = stations[i];
                if (entry.IsIr && entry.Station.Ammo > 0)
                    return true;
            }

            return false;
        }

        /// <summary>Own IR interceptors always first while any IR ammo remains.</summary>
        internal static bool ShouldUseIrInterceptPhase(IReadOnlyList<WeaponStationEntry> stations)
        {
            return HasIrStationAmmo(stations);
        }

        internal static bool PrefersIrInterceptor(Missile? missile)
        {
            if (!NocsGuard.IsValidMissile(missile))
                return false;

            return SeekerParamCache.IsIrSeeker(SeekerParamCache.ResolveSeekerKind(missile));
        }

        internal static float ResolveNoseFovDeg(Missile? missile)
        {
            return PrefersIrInterceptor(missile) ? IrNoseFovDeg : RadarNoseFovDeg;
        }

        internal static bool PassesNoseFov(
            Vector3 noseForward,
            Vector3 aircraftPos,
            Missile? missile,
            Vector3 threatPos)
        {
            if (!NocsGuard.IsValidMissile(missile))
                return false;

            Vector3 toThreat = threatPos - aircraftPos;
            float sqr = toThreat.sqrMagnitude;
            if (sqr < 0.0001f)
                return false;

            float invLen = 1f / Mathf.Sqrt(sqr);
            float cos = Vector3.Dot(noseForward, toThreat * invLen);
            float halfCos = PrefersIrInterceptor(missile) ? IrHalfCos : RadarHalfCos;
            return cos >= halfCos;
        }
    }
}
