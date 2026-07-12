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

        internal static bool RenderAseCircle = true;
        internal static bool RenderRadialText = true;
        internal static float AseVisualScale = 1f;

        internal static float AbsoluteMaxEngagementRange = 15000f;
        internal static float AbsoluteMinEngagementRange = 150f;
        internal static float AseMaxRangeFactor = 1f;
        internal static float AsePreviewRangeFactor = 1f;
        internal static float AsePreviewAppearDistanceM = 5000f;
        internal static float DefaultMaxTurnG = 15f;
        internal static float MaxManeuverWindow = 4.5f;
        internal static float AseSensitivityBias = 1f;
        internal static float AseInterceptConfidence = 0.99f;
        internal static float MinArmDistSlack = 1f;

        internal static float AseGateToleranceAngle = 0f;
        internal static float LaunchCooldown = 0.35f;
        internal static float MaxCpaMeters = 50f;
        internal static float MaxTimingTickDt;

        internal static bool WarningTtiEnabled = true;
        internal static float TtiSmoothingFactor = 0.08f;
        internal static float ClosureMinThreshold = 0.1f;
        internal static bool EngageIrThreats = false;

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
            TrueNotchMinWidthScale = s.TrueNotchMinWidthScale;
            TrueNotchMaxWidthScale = s.TrueNotchMaxWidthScale;
            NotchDopplerBias = s.NotchDopplerBias;

            HardKillEnabled = s.HardKillEnabled;
            AutoEngage = s.AutoEngage;
            HotKeyModifier = s.HotKeyModifier;
            HotKey = s.HotKey;
            WeaponPriority = s.WeaponPriority;
            WeaponFilterMode = s.WeaponFilterMode;

            RenderAseCircle = s.RenderAseCircle;
            RenderRadialText = s.RenderRadialText;
            AseVisualScale = Mathf.Clamp(s.AseVisualScale, 0.5f, 2f);

            AbsoluteMaxEngagementRange = Mathf.Clamp(s.AbsoluteMaxEngagementRange, 1000f, 100000f);
            AbsoluteMinEngagementRange = Mathf.Clamp(s.AbsoluteMinEngagementRange, 0f, 2000f);
            AseMaxRangeFactor = Mathf.Clamp(s.AseMaxRangeFactor, 0.5f, 1.5f);
            AsePreviewRangeFactor = Mathf.Max(0.01f, s.AsePreviewRangeFactor);
            AsePreviewAppearDistanceM = Mathf.Clamp(s.AsePreviewAppearDistanceM, 500f, 50000f);
            DefaultMaxTurnG = Mathf.Max(1f, s.DefaultMaxTurnG);
            MaxManeuverWindow = Mathf.Max(0.1f, s.MaxManeuverWindow);
            AseSensitivityBias = Mathf.Max(0.01f, s.AseSensitivityBias);
            AseInterceptConfidence = Mathf.Clamp(s.AseInterceptConfidence, 0.5f, 0.999f);
            MinArmDistSlack = Mathf.Max(0.1f, s.MinArmDistSlack);

            AseGateToleranceAngle = Mathf.Clamp(s.AseGateToleranceAngle, 0f, 180f);
            LaunchCooldown = Mathf.Clamp(s.LaunchCooldown, 0.05f, 2f);
            MaxCpaMeters = Mathf.Clamp(s.MaxCpaMeters, 10f, 200f);
            MaxTimingTickDt = s.MaxTimingTickDt;

            WarningTtiEnabled = s.WarningTtiEnabled;
            TtiSmoothingFactor = Mathf.Clamp(s.TtiSmoothingFactor, 0.01f, 1f);
            ClosureMinThreshold = Mathf.Max(0.01f, s.ClosureMinThreshold);
            EngageIrThreats = s.EngageIrThreats;
        }
    }
}
