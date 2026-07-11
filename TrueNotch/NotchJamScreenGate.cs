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
    /// Maps GetSignalStrength radial gate onto notchIndicatorBox local X.
    /// Center/position stays vanilla (ThreatItem.AlignNotchIndicator).
    /// </summary>
    internal static class NotchJamScreenGateCalculator
    {
        private const float PreviewDistanceMeters = 1000f;
        private const float MinHalfWidthPx = 1f;
        private const float MaxScreenWidthFrac = 0.42f;
        private static readonly Vector3 ScreenFlatten = new Vector3(1f, 1f, 0f);

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

            Camera? cam = SceneSingleton<CameraStateManager>.i?.mainCamera;
            Transform? camRoot = SceneSingleton<CameraStateManager>.i?.transform;
            if (cam == null || camRoot == null)
                return gate;

            FlightHud? hud = SceneSingleton<FlightHud>.i;
            if (hud == null || !FlightHudReflection.TryGetCockpit(hud, out Transform? cockpit, out _) || cockpit == null)
                return gate;

            Vector3 velocity = aircraft.rb.velocity;
            float speed = velocity.magnitude;
            if (speed <= 10f)
                return gate;

            Vector3 evade = evadeVector;
            evade.y = 0f;
            if (evade.sqrMagnitude < 0.0001f)
                return gate;
            Vector3 evadeN = evade.normalized;

            Vector3 los = radarCtx.LosDirection;
            Vector3 rotAxis = Vector3.Cross(los, velocity);
            if (rotAxis.sqrMagnitude < 0.0001f)
                rotAxis = Vector3.Cross(los, evadeN);
            if (rotAxis.sqrMagnitude < 0.0001f)
                rotAxis = Vector3.Cross(evadeN, Vector3.up);
            if (rotAxis.sqrMagnitude < 0.0001f)
                return gate;
            rotAxis.Normalize();

            float halfAngleDeg = maxRadial <= 0f
                ? 0f
                : Mathf.Asin(Mathf.Clamp01(maxRadial / speed)) * Mathf.Rad2Deg;

            Vector3 velPlus = halfAngleDeg <= 0f
                ? velocity
                : Quaternion.AngleAxis(halfAngleDeg, rotAxis) * velocity;
            Vector3 velMinus = halfAngleDeg <= 0f
                ? velocity
                : Quaternion.AngleAxis(-halfAngleDeg, rotAxis) * velocity;

            Vector2 centerScreen = notchRect.position;
            Vector2 plusScreen = ProjectVelocityDot(cockpit, cam, camRoot, velPlus);
            Vector2 minusScreen = ProjectVelocityDot(cockpit, cam, camRoot, velMinus);
            Vector2 dotScreen = ProjectVelocityDot(cockpit, cam, camRoot, velocity);

            Vector2 axisX = new Vector2(notchRect.right.x, notchRect.right.y);
            if (axisX.sqrMagnitude < 0.0001f)
                return gate;
            axisX.Normalize();

            float halfWidth = halfAngleDeg <= 0f
                ? MinHalfWidthPx
                : 0.5f * Mathf.Abs(Vector2.Dot(plusScreen - minusScreen, axisX));
            if (halfWidth < MinHalfWidthPx)
                halfWidth = MinHalfWidthPx;

            float scaleX = Mathf.Max(Mathf.Abs(notchRect.lossyScale.x), 0.001f);
            float localHalfWidth = halfWidth / scaleX;
            float localWidth = localHalfWidth * 2f;

            float maxLocalWidth = Screen.width * MaxScreenWidthFrac / scaleX;
            if (localWidth > maxLocalWidth)
                localWidth = maxLocalWidth;

            float dotLocalX = Vector2.Dot(dotScreen - centerScreen, axisX) / scaleX;
            float currentRadial = Mathf.Abs(Vector3.Dot(los, velocity));

            gate.Valid = true;
            gate.LocalWidth = localWidth;
            gate.DotLocalXPx = dotLocalX;
            gate.MaxRadialSpeed = maxRadial;
            gate.CurrentRadialSpeed = currentRadial;
            gate.InGate = currentRadial <= maxRadial + 0.01f;
            return gate;
        }

        private static Vector2 ProjectVelocityDot(
            Transform cockpit,
            Camera cam,
            Transform camRoot,
            Vector3 velocity)
        {
            Vector3 world = cockpit.position + velocity * PreviewDistanceMeters;
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (Vector3.Dot(camRoot.forward, velocity) > 0f)
                screen = Vector3.Scale(screen, ScreenFlatten);

            return new Vector2(screen.x, screen.y);
        }
    }
}
