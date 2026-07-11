using NOCS.Config;
using NOCS.Core;
using NOCS.Util;
using UnityEngine;
using UnityEngine.UI;

namespace NOCS.TrueNotch
{
    internal static class TrueNotchHudDriver
    {
        private const float MinDisplayWidthRefPx = 4f;

        private static int _smoothRectId = -1;
        private static float _smoothWidth = -1f;
        private static float _smoothVel;

        internal static void ApplyLive(
            Aircraft aircraft,
            Missile missile,
            Image notchBox,
            Vector3 evasionVector,
            float dt)
        {
            if (!NocsConfigCache.TrueNotchEnabled)
                return;

            if (!GameManager.flightControlsEnabled)
                return;

            if (!NocsGuard.IsLocalPlayerAircraft(aircraft) || !NocsGuard.IsValidMissile(missile))
                return;

            RectTransform? rect = notchBox.rectTransform;
            if (rect == null)
                return;

            NotchJamSample sample = NotchJamWindowCalculator.Compute(
                aircraft,
                missile,
                evasionVector,
                rect);

            if (!sample.Valid)
                return;

            if (!NotchOverlayBinder.TryBind(notchBox, out RectTransform boundRect, out _))
                return;

            ApplySampleWidth(boundRect, sample.LocalWidth, dt);
        }

        internal static void RunTick(float dt)
        {
            if (!NocsConfigCache.TrueNotchEnabled || !GameManager.flightControlsEnabled)
                return;

            CombatHUD? combatHud = SceneSingleton<CombatHUD>.i;
            Aircraft? aircraft = combatHud?.aircraft;
            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
                return;

            if (!NotchTelemetryBridge.TryPeek(out NotchTelemetrySample telemetry, out Image? notchBox) || notchBox == null)
                return;

            Missile? missile = ResolveMissile(telemetry.MissileId);
            if (!NocsGuard.IsValidMissile(missile))
                return;

            ApplyLive(aircraft!, missile!, notchBox, telemetry.EvasionVector, dt);
        }

        private static void ApplySampleWidth(RectTransform rect, float targetWidth, float dt)
        {
            float minWidth = NocsScreenScale.Px(MinDisplayWidthRefPx);
            if (targetWidth < minWidth)
                targetWidth = minWidth;

            if (NocsConfigCache.TrueNotchClampWidth
                && NotchOverlayBinder.TryGetPrefabWidth(rect, out float prefabWidth))
            {
                float minClamp = prefabWidth * NocsConfigCache.TrueNotchMinWidthScale;
                float maxClamp = prefabWidth * NocsConfigCache.TrueNotchMaxWidthScale;
                targetWidth = Mathf.Clamp(targetWidth, minClamp, maxClamp);
            }

            int rectId = rect.GetInstanceID();
            if (rectId != _smoothRectId)
            {
                _smoothRectId = rectId;
                _smoothWidth = -1f;
                _smoothVel = 0f;
            }

            if (_smoothWidth < 0f)
                _smoothWidth = targetWidth;
            else
            {
                _smoothWidth = Mathf.SmoothDamp(
                    _smoothWidth,
                    targetWidth,
                    ref _smoothVel,
                    NocsConfigCache.TrueNotchSmoothTime,
                    float.PositiveInfinity,
                    dt);
            }

            ApplyWidth(rect, _smoothWidth);
        }

        private static Missile? ResolveMissile(PersistentID missileId)
        {
            if (!missileId.IsValid)
                return null;

            if (UnitRegistry.TryGetUnit(new PersistentID?(missileId), out Unit unit))
                return unit as Missile;

            return null;
        }

        private static void ApplyWidth(RectTransform rect, float width)
        {
            if (width <= 0f)
                return;

            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        internal static void DisposeViews()
        {
            _smoothRectId = -1;
            _smoothWidth = -1f;
            _smoothVel = 0f;
            NotchOverlayBinder.Clear();
            NotchTelemetryBridge.Clear();
        }
    }
}
