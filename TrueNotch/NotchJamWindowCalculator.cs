using Mirage.Serialization;
using UnityEngine;

namespace NOCS.TrueNotch
{
    internal struct NotchJamSample
    {
        internal bool Valid;
        internal float LocalWidth;
        internal float DotOffsetPx;
        internal float MaxRadialSpeed;
        internal float CurrentRadialSpeed;
        internal bool InGate;
        internal PersistentID MissileId;
    }

    internal static class NotchJamWindowCalculator
    {
        internal static NotchJamSample Compute(
            Aircraft aircraft,
            Missile missile,
            Vector3 evasionVector,
            RectTransform? notchRect)
        {
            NotchJamSample sample = default;
            if (aircraft == null || missile == null || aircraft.rb == null || notchRect == null)
                return sample;

            if (!NotchEvadeGeometry.TryComputeEvadeVector(aircraft, evasionVector, out Vector3 evadeVector))
                return sample;

            if (!NotchThreatRadarResolver.TryResolve(aircraft, missile, out NotchThreatRadarContext radarCtx))
                return sample;

            NotchJamScreenGate gate = NotchJamScreenGateCalculator.Compute(aircraft, radarCtx, evadeVector, notchRect);
            if (!gate.Valid)
                return sample;

            sample.Valid = true;
            sample.LocalWidth = gate.LocalWidth;
            sample.DotOffsetPx = gate.DotLocalXPx;
            sample.MaxRadialSpeed = gate.MaxRadialSpeed;
            sample.CurrentRadialSpeed = gate.CurrentRadialSpeed;
            sample.InGate = gate.InGate;
            sample.MissileId = missile.persistentID;
            return sample;
        }
    }
}
