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
        private static int _lastApplyFrame = -1;
        private static PersistentID _lastApplyMissileId;

        internal static void ApplyLive(
            Aircraft aircraft,
            Missile missile,
            Image notchBox,
            Vector3 evasionVector,
            float dt)
        {
            try
            {
                ApplyLiveCore(aircraft, missile, notchBox, evasionVector, dt);
            }
            catch (System.Exception ex)
            {
                NocsDiagLog.ExceptionOnce("TrueNotchHudDriver.ApplyLive", ex);
            }
        }

        private static void ApplyLiveCore(
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

            int frame = Time.frameCount;
            PersistentID missileId = missile.persistentID;
            if (frame == _lastApplyFrame && missileId == _lastApplyMissileId)
                return;

            if (notchBox == null)
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

            _lastApplyFrame = frame;
            _lastApplyMissileId = missileId;
            ApplySampleWidth(boundRect, sample.LocalWidth, dt);
        }

        internal static void RunTick(float dt)
        {
            try
            {
                if (!NocsConfigCache.TrueNotchEnabled || !GameManager.flightControlsEnabled)
                    return;

                if (!GameManager.GetLocalAircraft(out Aircraft aircraft) || aircraft == null)
                    return;

                if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
                    return;

                if (!NotchTelemetryBridge.TryPeek(out NotchTelemetrySample telemetry, out Image? notchBox) || notchBox == null)
                    return;

                Missile? missile = ResolveMissile(telemetry.MissileId);
                if (!NocsGuard.IsValidMissile(missile))
                    return;

                ApplyLiveCore(aircraft, missile!, notchBox, telemetry.EvasionVector, dt);
            }
            catch (System.Exception ex)
            {
                NocsDiagLog.ExceptionOnce("TrueNotchHudDriver.RunTick", ex);
            }
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
