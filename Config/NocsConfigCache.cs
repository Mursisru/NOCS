using UnityEngine;

namespace NOCS.Config
{
    internal static class NocsConfigCache
    {
        internal static bool TrueNotchEnabled = true;
        internal static float TrueNotchSmoothTime = 0.05f;
        internal static bool TrueNotchClampWidth = false;
        internal static float TrueNotchMinWidthScale = 1f;
        internal static float TrueNotchMaxWidthScale = 1f;
        internal static float NotchDopplerBias = 0f;

        internal static bool HardKillEnabled = true;
        internal static bool AutoEngage = false;
        internal static KeyCode HotKeyModifier = KeyCode.RightShift;
        internal static KeyCode HotKey = KeyCode.Slash;
        internal static WeaponPriority WeaponPriority = WeaponPriority.IR_First;
        internal static WeaponFilterMode WeaponFilterMode = WeaponFilterMode.AntiMissileOnly;
        internal static bool EngageIrThreats;
        internal static int IrInterceptBlockedAboveFlares = 4;

        internal static bool RenderAseCircle = false;
        internal static bool RenderRadialText = true;
        internal static float AseVisualScale = 1f;

        internal static float MaxLaunchRangeMeters = 15000f;
        internal static float MinLaunchRangeMeters = 150f;
        internal static float AseMaxRangeFactor = 1f;
        internal static float AsePreviewRangeFactor = 1f;
        internal static float AsePreviewAppearDistanceM = 5000f;
        internal static float DefaultMaxTurnG = 15f;
        internal static float MaxManeuverWindow = 4.5f;
        internal static float AseSensitivityBias = 1f;
        internal static float AseInterceptConfidence = 0.99f;
        internal static float MinArmDistSlack = 1f;

        internal static float ManualLaunchAimTolerance = 0f;
        internal static bool RequireAseScreenShoot = false;
        internal static float LaunchCooldown = 0.35f;
        internal static float MissDistanceToleranceMeters = 50f;
        internal static float MaxTimingTickDt;

        internal static bool WarningTtiEnabled = true;
        internal static float TtiSmoothingFactor = 0.08f;
        internal static float ClosureMinThreshold = 0.1f;

        internal static void BindFromBepIn(in NocsConfigSnapshot snapshot)
        {
            Apply(in snapshot);
        }

        internal static void RefreshFromBepIn()
        {
            if (!NocsBepInConfig.IsBound)
                return;

            Apply(NocsBepInConfig.Snapshot());
        }

        private static void Apply(in NocsConfigSnapshot s)
        {
            TrueNotchEnabled = s.TrueNotchEnabled;
            TrueNotchSmoothTime = Mathf.Max(0.01f, s.TrueNotchSmoothTime);
            TrueNotchClampWidth = s.TrueNotchClampWidth;
            float minScale = Mathf.Max(0.1f, s.TrueNotchMinWidthScale);
            float maxScale = Mathf.Max(0.1f, s.TrueNotchMaxWidthScale);
            if (minScale > maxScale)
            {
                float swap = minScale;
                minScale = maxScale;
                maxScale = swap;
            }

            TrueNotchMinWidthScale = minScale;
            TrueNotchMaxWidthScale = maxScale;
            NotchDopplerBias = Mathf.Clamp(s.NotchDopplerBias, -1f, 1f);

            HardKillEnabled = s.HardKillEnabled;
            AutoEngage = s.AutoEngage;
            HotKeyModifier = s.HotKeyModifier;
            HotKey = s.HotKey;
            WeaponPriority = s.WeaponPriority;
            WeaponFilterMode = s.WeaponFilterMode;
            EngageIrThreats = s.EngageIrThreats;
            IrInterceptBlockedAboveFlares = Mathf.Max(0, s.IrInterceptBlockedAboveFlares);

            RenderAseCircle = s.RenderAseCircle;
            RenderRadialText = s.RenderRadialText;
            AseVisualScale = Mathf.Clamp(s.AseVisualScale, 0.5f, 2f);

            MaxLaunchRangeMeters = Mathf.Clamp(s.MaxLaunchRangeMeters, 1000f, 100000f);
            MinLaunchRangeMeters = Mathf.Clamp(s.MinLaunchRangeMeters, 0f, 2000f);
            AseMaxRangeFactor = Mathf.Clamp(s.AseMaxRangeFactor, 0.5f, 1.5f);
            AsePreviewRangeFactor = Mathf.Clamp(s.AsePreviewRangeFactor, 0.01f, 5f);
            AsePreviewAppearDistanceM = Mathf.Clamp(s.AsePreviewAppearDistanceM, 500f, 50000f);
            DefaultMaxTurnG = Mathf.Max(1f, s.DefaultMaxTurnG);
            MaxManeuverWindow = Mathf.Max(0.1f, s.MaxManeuverWindow);
            AseSensitivityBias = Mathf.Max(0.01f, s.AseSensitivityBias);
            AseInterceptConfidence = Mathf.Clamp(s.AseInterceptConfidence, 0.5f, 0.999f);
            MinArmDistSlack = Mathf.Max(0.1f, s.MinArmDistSlack);

            ManualLaunchAimTolerance = Mathf.Clamp(s.ManualLaunchAimTolerance, 0f, 180f);
            RequireAseScreenShoot = s.RequireAseScreenShoot;
            LaunchCooldown = Mathf.Clamp(s.LaunchCooldown, 0.05f, 2f);
            MissDistanceToleranceMeters = Mathf.Clamp(s.MissDistanceToleranceMeters, 10f, 200f);
            MaxTimingTickDt = Mathf.Clamp(s.MaxTimingTickDt, 0f, 1f);

            WarningTtiEnabled = s.WarningTtiEnabled;
            TtiSmoothingFactor = Mathf.Clamp(s.TtiSmoothingFactor, 0.01f, 1f);
            ClosureMinThreshold = Mathf.Max(0.01f, s.ClosureMinThreshold);
        }
    }
}
