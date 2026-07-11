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
        internal static bool AseCircleEnabled = true;
        internal static bool AutoEngage = false;
        internal static KeyCode HotKeyModifier = KeyCode.RightShift;
        internal static KeyCode HotKey = KeyCode.Slash;
        internal static WeaponPriority WeaponPriority = WeaponPriority.IR_First;
        internal static WeaponFilterMode WeaponFilterMode = WeaponFilterMode.AntiMissileOnly;
        internal static bool SafetyDistanceGate = true;
        internal static float AseMetersToRefPx = 120f;
        internal static float DefaultMaxTurnG = 15f;
        internal static float MinClosureMps = 5f;
        internal static float MaxManeuverWindow = 4.5f;
        internal static float AseSensitivityBias = 1f;
        internal static float AseInterceptConfidence = 0.99f;
        internal static float MaxCpaMeters = 50f;
        internal static float MinArmDistSlack = 1f;
        internal static float MaxTimingTickDt;
        internal static float AseMaxRangeFactor = 1f;
        internal static float AsePreviewRangeFactor = 1f;
        internal static float LaunchCooldown = 0.05f;
        internal static bool WarningTtiEnabled = true;
        internal static float TtiSmoothingFactor = 0.08f;

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

        internal static void RefreshForTick()
        {
            RefreshFromBepIn();
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
            AseCircleEnabled = s.AseCircleEnabled;
            AutoEngage = s.AutoEngage;
            HotKeyModifier = s.HotKeyModifier;
            HotKey = s.HotKey;
            WeaponPriority = s.WeaponPriority;
            WeaponFilterMode = s.WeaponFilterMode;
            SafetyDistanceGate = s.SafetyDistanceGate;
            AseMetersToRefPx = s.AseMetersToRefPx;
            DefaultMaxTurnG = Mathf.Max(1f, s.DefaultMaxTurnG);
            MinClosureMps = Mathf.Max(1f, s.MinClosureMps);
            MaxManeuverWindow = Mathf.Max(0.1f, s.MaxManeuverWindow);
            AseSensitivityBias = Mathf.Max(0.01f, s.AseSensitivityBias);
            AseInterceptConfidence = Mathf.Clamp(s.AseInterceptConfidence, 0.5f, 0.999f);
            MaxCpaMeters = Mathf.Max(1f, s.MaxCpaMeters);
            MinArmDistSlack = Mathf.Max(0.1f, s.MinArmDistSlack);
            MaxTimingTickDt = s.MaxTimingTickDt;
            AseMaxRangeFactor = Mathf.Max(0.01f, s.AseMaxRangeFactor);
            AsePreviewRangeFactor = Mathf.Max(0.01f, s.AsePreviewRangeFactor);
            LaunchCooldown = Mathf.Max(0f, s.LaunchCooldown);
            WarningTtiEnabled = s.WarningTtiEnabled;
            TtiSmoothingFactor = Mathf.Clamp(s.TtiSmoothingFactor, 0.01f, 1f);
        }
    }
}
