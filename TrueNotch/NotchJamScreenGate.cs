using NOCS.Util;
using UnityEngine;

namespace NOCS.TrueNotch
{
    internal struct NotchJamScreenGate
    {
        internal bool Valid;
        internal float LocalWidth;
        internal float DotLocalXPx;
        internal float MaxRadialSpeed;
        internal float CurrentRadialSpeed;
        internal bool InGate;
    }

    /// <summary>
    /// Maps jam radial gate onto notchIndicatorBox local X using angular width (view-stable).
    /// Center/position stays vanilla (ThreatItem.AlignNotchIndicator).
    /// </summary>
    internal static class NotchJamScreenGateCalculator
    {
        private const float MinHalfWidthRefPx = 2f;
        private const float MaxScreenWidthFrac = 0.42f;

        internal static NotchJamScreenGate Compute(
            Aircraft aircraft,
            in NotchThreatRadarContext radarCtx,
            Vector3 evadeVector,
            RectTransform notchRect)
        {
            NotchJamScreenGate gate = default;
            if (aircraft == null || aircraft.rb == null || notchRect == null || !radarCtx.Valid)
                return gate;

            if (!NotchJamSignalEvaluator.TryGetMaxRadialSpeed(radarCtx, out float maxRadial))
                return gate;

            Vector3 velocity = aircraft.rb.velocity;
            float speed = velocity.magnitude;
            if (speed <= 10f)
                return gate;

            Vector3 evade = evadeVector;
            evade.y = 0f;
            if (evade.sqrMagnitude < 0.0001f)
                return gate;

            Vector3 los = radarCtx.LosDirection;
            float halfAngleDeg = maxRadial <= 0f
                ? 0f
                : Mathf.Asin(Mathf.Clamp01(maxRadial / speed)) * Mathf.Rad2Deg;

            float minHalfScreenPx = NocsScreenScale.Px(MinHalfWidthRefPx);
            float screenHalfWidth = halfAngleDeg <= 0f
                ? minHalfScreenPx
                : NocsScreenScale.RadiansToPx(halfAngleDeg * Mathf.Deg2Rad);
            if (screenHalfWidth < minHalfScreenPx)
                screenHalfWidth = minHalfScreenPx;

            float screenWidth = screenHalfWidth * 2f;
            float maxScreenWidth = Screen.width * MaxScreenWidthFrac;
            if (screenWidth > maxScreenWidth)
                screenWidth = maxScreenWidth;

            float pxPerLocal = ResolveScreenPixelsPerLocalUnit(notchRect);
            float localWidth = screenWidth / pxPerLocal;

            float currentRadial = Mathf.Abs(Vector3.Dot(los, velocity));

            gate.Valid = true;
            gate.LocalWidth = localWidth;
            gate.DotLocalXPx = 0f;
            gate.MaxRadialSpeed = maxRadial;
            gate.CurrentRadialSpeed = currentRadial;
            gate.InGate = currentRadial <= maxRadial + 0.01f;
            return gate;
        }

        private static float ResolveScreenPixelsPerLocalUnit(RectTransform notchRect)
        {
            Vector2 originScreen = notchRect.position;
            Vector3 unitRight = notchRect.TransformPoint(new Vector3(1f, 0f, 0f));
            float pxPerLocal = Vector2.Distance(originScreen, new Vector2(unitRight.x, unitRight.y));
            return pxPerLocal > 0.001f ? pxPerLocal : NocsScreenScale.Px(1f);
        }
    }
}
