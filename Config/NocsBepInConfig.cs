using BepInEx.Configuration;
using UnityEngine;

namespace NOCS.Config
{
    internal static class NocsBepInConfig
    {
        internal static bool IsBound { get; private set; }

        internal static ConfigEntry<bool> TrueNotchEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> TrueNotchSmoothTime { get; private set; } = null!;
        internal static ConfigEntry<bool> TrueNotchClampWidth { get; private set; } = null!;
        internal static ConfigEntry<float> TrueNotchMinWidthScale { get; private set; } = null!;
        internal static ConfigEntry<float> TrueNotchMaxWidthScale { get; private set; } = null!;
        internal static ConfigEntry<float> NotchDopplerBias { get; private set; } = null!;

        internal static ConfigEntry<bool> HardKillEnabled { get; private set; } = null!;
        internal static ConfigEntry<bool> AseCircleEnabled { get; private set; } = null!;
        internal static ConfigEntry<bool> AutoEngage { get; private set; } = null!;
        internal static ConfigEntry<KeyCode> HotKeyModifier { get; private set; } = null!;
        internal static ConfigEntry<KeyCode> HotKey { get; private set; } = null!;
        internal static ConfigEntry<WeaponPriority> WeaponPriority { get; private set; } = null!;
        internal static ConfigEntry<WeaponFilterMode> WeaponFilterMode { get; private set; } = null!;
        internal static ConfigEntry<bool> SafetyDistanceGate { get; private set; } = null!;
        internal static ConfigEntry<float> AseMetersToRefPx { get; private set; } = null!;
        internal static ConfigEntry<float> DefaultMaxTurnG { get; private set; } = null!;
        internal static ConfigEntry<float> MinClosureMps { get; private set; } = null!;
        internal static ConfigEntry<float> MaxManeuverWindow { get; private set; } = null!;
        internal static ConfigEntry<float> AseSensitivityBias { get; private set; } = null!;
        internal static ConfigEntry<float> AseInterceptConfidence { get; private set; } = null!;
        internal static ConfigEntry<float> MaxCpaMeters { get; private set; } = null!;
        internal static ConfigEntry<float> MinArmDistSlack { get; private set; } = null!;
        internal static ConfigEntry<float> MaxTimingTickDt { get; private set; } = null!;
        internal static ConfigEntry<float> AseMaxRangeFactor { get; private set; } = null!;
        internal static ConfigEntry<float> AsePreviewRangeFactor { get; private set; } = null!;
        internal static ConfigEntry<float> LaunchCooldown { get; private set; } = null!;

        internal static ConfigEntry<bool> WarningTtiEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> TtiSmoothingFactor { get; private set; } = null!;

        internal static void Bind(ConfigFile config)
        {
            const string trueNotch = "TrueNotchHUD";
            TrueNotchEnabled = config.Bind(trueNotch, "Enabled", true, "Master switch for notch width overlay.");
            TrueNotchSmoothTime = config.Bind(trueNotch, "SmoothTime", 0.05f,
                new ConfigDescription("Width smoothing time constant.", new AcceptableValueRange<float>(0.01f, 1f)));
            TrueNotchClampWidth = config.Bind(trueNotch, "ClampWidth", false, "Clamp width to min/max scale.");
            TrueNotchMinWidthScale = config.Bind(trueNotch, "MinWidthScale", 1f,
                new ConfigDescription("Min scale when clamp enabled.", new AcceptableValueRange<float>(0.1f, 5f)));
            TrueNotchMaxWidthScale = config.Bind(trueNotch, "MaxWidthScale", 1f,
                new ConfigDescription("Max scale when clamp enabled.", new AcceptableValueRange<float>(0.1f, 5f)));
            NotchDopplerBias = config.Bind(trueNotch, "NotchDopplerBias", 0f,
                new ConfigDescription("Optional Doppler bias offset.", new AcceptableValueRange<float>(-1f, 1f)));

            const string hardKill = "HardKillAPS";
            HardKillEnabled = config.Bind(hardKill, "Enabled", true, "Master switch for Hard-Kill APS.");
            AseCircleEnabled = config.Bind(hardKill, "AseCircleEnabled", true,
                "Show the swarm ASE intercept circle and cue labels.");
            AutoEngage = config.Bind(hardKill, "AutoEngage", false, "Automatic salvo without hotkey.");
            HotKeyModifier = config.Bind(hardKill, "HotKeyModifier", KeyCode.RightShift, "Engagement modifier key.");
            HotKey = config.Bind(hardKill, "HotKey", KeyCode.Slash, "Engagement fire key (/ on US layout).");
            WeaponPriority = config.Bind(hardKill, "WeaponPriority", Config.WeaponPriority.IR_First,
                "IR_First or ARH_First defensive station priority.");
            WeaponFilterMode = config.Bind(hardKill, "WeaponFilterMode", Config.WeaponFilterMode.AntiMissileOnly,
                "Station filter mode.");
            SafetyDistanceGate = config.Bind(hardKill, "SafetyDistanceGate", true,
                "Require velocity vector inside all threat envelopes for SHOOT.");
            AseMetersToRefPx = config.Bind(hardKill, "AseMetersToRefPx", 120f,
                new ConfigDescription("Reference meters-to-pixel scale.", new AcceptableValueRange<float>(10f, 500f)));
            DefaultMaxTurnG = config.Bind(hardKill, "DefaultMaxTurnG", 15f,
                new ConfigDescription("Fallback g-limit when prefab data missing.", new AcceptableValueRange<float>(1f, 60f)));
            MinClosureMps = config.Bind(hardKill, "MinClosureMps", 5f,
                new ConfigDescription("Minimum closure for threat gates.", new AcceptableValueRange<float>(1f, 100f)));
            MaxManeuverWindow = config.Bind(hardKill, "MaxManeuverWindow", 4.5f,
                new ConfigDescription("Max maneuver time for envelope (seconds).", new AcceptableValueRange<float>(0.1f, 20f)));
            AseSensitivityBias = config.Bind(hardKill, "AseSensitivityBias", 1f,
                new ConfigDescription("Global radius multiplier.", new AcceptableValueRange<float>(0.01f, 5f)));
            AseInterceptConfidence = config.Bind(hardKill, "AseInterceptConfidence", 0.99f,
                new ConfigDescription("Intercept confidence target.", new AcceptableValueRange<float>(0.5f, 0.999f)));
            MaxCpaMeters = config.Bind(hardKill, "MaxCpaMeters", 50f,
                new ConfigDescription("Max CPA distance for threat inclusion.", new AcceptableValueRange<float>(1f, 500f)));
            MinArmDistSlack = config.Bind(hardKill, "MinArmDistSlack", 1f,
                new ConfigDescription("Arm-distance slack multiplier.", new AcceptableValueRange<float>(0.1f, 5f)));
            MaxTimingTickDt = config.Bind(hardKill, "MaxTimingTickDt", 0f,
                new ConfigDescription("Optional salvo timing cap (0 = off).", new AcceptableValueRange<float>(0f, 1f)));
            AseMaxRangeFactor = config.Bind(hardKill, "AseMaxRangeFactor", 1f,
                new ConfigDescription("Engage window range multiplier.", new AcceptableValueRange<float>(0.01f, 5f)));
            AsePreviewRangeFactor = config.Bind(hardKill, "AsePreviewRangeFactor", 1f,
                new ConfigDescription("ASE preview range multiplier.", new AcceptableValueRange<float>(0.01f, 5f)));
            LaunchCooldown = config.Bind(hardKill, "LaunchCooldown", 0.05f,
                new ConfigDescription("Minimum delay between salvo launches (seconds).", new AcceptableValueRange<float>(0f, 5f)));

            const string warningTti = "WarningTTI";
            WarningTtiEnabled = config.Bind(warningTti, "Enabled", true, "Append TTI suffix to MWS threat labels.");
            TtiSmoothingFactor = config.Bind(warningTti, "TtiSmoothingFactor", 0.08f,
                new ConfigDescription("Low-pass blend toward raw physics TTI.", new AcceptableValueRange<float>(0.01f, 1f)));

            IsBound = true;
            WireSettingChanged();
        }

        internal static NocsConfigSnapshot Snapshot()
        {
            return new NocsConfigSnapshot
            {
                TrueNotchEnabled = TrueNotchEnabled.Value,
                TrueNotchSmoothTime = TrueNotchSmoothTime.Value,
                TrueNotchClampWidth = TrueNotchClampWidth.Value,
                TrueNotchMinWidthScale = TrueNotchMinWidthScale.Value,
                TrueNotchMaxWidthScale = TrueNotchMaxWidthScale.Value,
                NotchDopplerBias = NotchDopplerBias.Value,
                HardKillEnabled = HardKillEnabled.Value,
                AseCircleEnabled = AseCircleEnabled.Value,
                AutoEngage = AutoEngage.Value,
                HotKeyModifier = HotKeyModifier.Value,
                HotKey = HotKey.Value,
                WeaponPriority = WeaponPriority.Value,
                WeaponFilterMode = WeaponFilterMode.Value,
                SafetyDistanceGate = SafetyDistanceGate.Value,
                AseMetersToRefPx = AseMetersToRefPx.Value,
                DefaultMaxTurnG = DefaultMaxTurnG.Value,
                MinClosureMps = MinClosureMps.Value,
                MaxManeuverWindow = MaxManeuverWindow.Value,
                AseSensitivityBias = AseSensitivityBias.Value,
                AseInterceptConfidence = AseInterceptConfidence.Value,
                MaxCpaMeters = MaxCpaMeters.Value,
                MinArmDistSlack = MinArmDistSlack.Value,
                MaxTimingTickDt = MaxTimingTickDt.Value,
                AseMaxRangeFactor = AseMaxRangeFactor.Value,
                AsePreviewRangeFactor = AsePreviewRangeFactor.Value,
                LaunchCooldown = LaunchCooldown.Value,
                WarningTtiEnabled = WarningTtiEnabled.Value,
                TtiSmoothingFactor = TtiSmoothingFactor.Value,
            };
        }

        private static void WireSettingChanged()
        {
            TrueNotchEnabled.SettingChanged += OnAnySettingChanged;
            TrueNotchSmoothTime.SettingChanged += OnAnySettingChanged;
            TrueNotchClampWidth.SettingChanged += OnAnySettingChanged;
            TrueNotchMinWidthScale.SettingChanged += OnAnySettingChanged;
            TrueNotchMaxWidthScale.SettingChanged += OnAnySettingChanged;
            NotchDopplerBias.SettingChanged += OnAnySettingChanged;
            HardKillEnabled.SettingChanged += OnAnySettingChanged;
            AseCircleEnabled.SettingChanged += OnAnySettingChanged;
            AutoEngage.SettingChanged += OnAnySettingChanged;
            HotKeyModifier.SettingChanged += OnAnySettingChanged;
            HotKey.SettingChanged += OnAnySettingChanged;
            WeaponPriority.SettingChanged += OnAnySettingChanged;
            WeaponFilterMode.SettingChanged += OnAnySettingChanged;
            SafetyDistanceGate.SettingChanged += OnAnySettingChanged;
            AseMetersToRefPx.SettingChanged += OnAnySettingChanged;
            DefaultMaxTurnG.SettingChanged += OnAnySettingChanged;
            MinClosureMps.SettingChanged += OnAnySettingChanged;
            MaxManeuverWindow.SettingChanged += OnAnySettingChanged;
            AseSensitivityBias.SettingChanged += OnAnySettingChanged;
            AseInterceptConfidence.SettingChanged += OnAnySettingChanged;
            MaxCpaMeters.SettingChanged += OnAnySettingChanged;
            MinArmDistSlack.SettingChanged += OnAnySettingChanged;
            MaxTimingTickDt.SettingChanged += OnAnySettingChanged;
            AseMaxRangeFactor.SettingChanged += OnAnySettingChanged;
            AsePreviewRangeFactor.SettingChanged += OnAnySettingChanged;
            LaunchCooldown.SettingChanged += OnAnySettingChanged;
            WarningTtiEnabled.SettingChanged += OnAnySettingChanged;
            TtiSmoothingFactor.SettingChanged += OnAnySettingChanged;
        }

        private static void OnAnySettingChanged(object sender, System.EventArgs e)
        {
            NocsConfigCache.RefreshFromBepIn();
        }
    }

    internal struct NocsConfigSnapshot
    {
        internal bool TrueNotchEnabled;
        internal float TrueNotchSmoothTime;
        internal bool TrueNotchClampWidth;
        internal float TrueNotchMinWidthScale;
        internal float TrueNotchMaxWidthScale;
        internal float NotchDopplerBias;
        internal bool HardKillEnabled;
        internal bool AseCircleEnabled;
        internal bool AutoEngage;
        internal KeyCode HotKeyModifier;
        internal KeyCode HotKey;
        internal WeaponPriority WeaponPriority;
        internal WeaponFilterMode WeaponFilterMode;
        internal bool SafetyDistanceGate;
        internal float AseMetersToRefPx;
        internal float DefaultMaxTurnG;
        internal float MinClosureMps;
        internal float MaxManeuverWindow;
        internal float AseSensitivityBias;
        internal float AseInterceptConfidence;
        internal float MaxCpaMeters;
        internal float MinArmDistSlack;
        internal float MaxTimingTickDt;
        internal float AseMaxRangeFactor;
        internal float AsePreviewRangeFactor;
        internal float LaunchCooldown;
        internal bool WarningTtiEnabled;
        internal float TtiSmoothingFactor;
    }
}
