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
        internal static ConfigEntry<bool> AutoEngage { get; private set; } = null!;
        internal static ConfigEntry<KeyCode> HotKeyModifier { get; private set; } = null!;
        internal static ConfigEntry<KeyCode> HotKey { get; private set; } = null!;
        internal static ConfigEntry<WeaponPriority> WeaponPriority { get; private set; } = null!;
        internal static ConfigEntry<WeaponFilterMode> WeaponFilterMode { get; private set; } = null!;

        internal static ConfigEntry<bool> RenderAseCircle { get; private set; } = null!;
        internal static ConfigEntry<bool> RenderRadialText { get; private set; } = null!;
        internal static ConfigEntry<float> AseVisualScale { get; private set; } = null!;

        internal static ConfigEntry<float> MaxLaunchRangeMeters { get; private set; } = null!;
        internal static ConfigEntry<float> MinLaunchRangeMeters { get; private set; } = null!;
        internal static ConfigEntry<float> AseMaxRangeFactor { get; private set; } = null!;
        internal static ConfigEntry<float> AsePreviewRangeFactor { get; private set; } = null!;
        internal static ConfigEntry<float> AsePreviewAppearDistanceM { get; private set; } = null!;
        internal static ConfigEntry<float> DefaultMaxTurnG { get; private set; } = null!;
        internal static ConfigEntry<float> MaxManeuverWindow { get; private set; } = null!;
        internal static ConfigEntry<float> AseSensitivityBias { get; private set; } = null!;
        internal static ConfigEntry<float> AseInterceptConfidence { get; private set; } = null!;
        internal static ConfigEntry<float> MinArmDistSlack { get; private set; } = null!;

        internal static ConfigEntry<float> ManualLaunchAimTolerance { get; private set; } = null!;
        internal static ConfigEntry<bool> RequireAseScreenShoot { get; private set; } = null!;
        internal static ConfigEntry<float> LaunchCooldown { get; private set; } = null!;
        internal static ConfigEntry<float> MissDistanceToleranceMeters { get; private set; } = null!;
        internal static ConfigEntry<float> MaxTimingTickDt { get; private set; } = null!;

        internal static ConfigEntry<bool> WarningTtiEnabled { get; private set; } = null!;
        internal static ConfigEntry<float> TtiSmoothingFactor { get; private set; } = null!;
        internal static ConfigEntry<float> ClosureMinThreshold { get; private set; } = null!;

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
            AutoEngage = config.Bind(hardKill, "AutoEngage", false, "Automatic salvo without hotkey.");
            HotKeyModifier = config.Bind(hardKill, "HotKeyModifier", KeyCode.RightShift, "Engagement modifier key.");
            HotKey = config.Bind(hardKill, "HotKey", KeyCode.Slash, "Engagement fire key (/ on US layout).");
            WeaponPriority = config.Bind(hardKill, "WeaponPriority", Config.WeaponPriority.IR_First,
                "Legacy priority enum (Hard-Kill always expends IR interceptors before radar).");
            WeaponFilterMode = config.Bind(hardKill, "WeaponFilterMode", Config.WeaponFilterMode.AntiMissileOnly,
                "Station filter mode.");

            const string hudVisuals = "1. HUD Visuals";
            RenderAseCircle = config.Bind(hudVisuals, "ShowAseInterceptRing", false,
                "Show ASE intercept ring with arc SHOOT / POTENTIAL HIT labels on the ring circumference.");
            RenderRadialText = config.Bind(hudVisuals, "RenderRadialText", true,
                "Show SHOOT / POTENTIAL HIT status cue under the weapon HUD hint (typography from hint, color from notch rectangle).");
            AseVisualScale = config.Bind(hudVisuals, "AseVisualScale", 1f,
                new ConfigDescription("ASE ring visual scale.", new AcceptableValueRange<float>(0.5f, 2f)));

            const string envelope = "2. Engagement Envelope";
            MaxLaunchRangeMeters = config.Bind(envelope, "MaxLaunchRangeMeters", 15000f,
                new ConfigDescription(
                    "Absolute maximum range for anti-missile intercept implementation.",
                    new AcceptableValueRange<float>(1000f, 100000f)));
            MinLaunchRangeMeters = config.Bind(envelope, "MinLaunchRangeMeters", 150f,
                new ConfigDescription(
                    "Absolute minimum range weapon engagement dead-zone.",
                    new AcceptableValueRange<float>(0f, 2000f)));
            AseMaxRangeFactor = config.Bind(envelope, "AseMaxRangeFactor", 1f,
                new ConfigDescription("Multiplier for the dynamic maxDynamicRange engage window.", new AcceptableValueRange<float>(0.5f, 1.5f)));
            AsePreviewRangeFactor = config.Bind(envelope, "AsePreviewRangeFactor", 1f,
                new ConfigDescription("Advanced: ASE preview range multiplier.", new AcceptableValueRange<float>(0.01f, 5f)));
            AsePreviewAppearDistanceM = config.Bind(envelope, "AsePreviewAppearDistanceM", 5000f,
                new ConfigDescription("Slant range (m) at which the ASE ring may first appear. Preview uses relaxed Doppler/CPA gates at and inside this distance.", new AcceptableValueRange<float>(500f, 50000f)));
            DefaultMaxTurnG = config.Bind(envelope, "DefaultMaxTurnG", 15f,
                new ConfigDescription("Advanced: fallback g-limit when prefab data is missing.", new AcceptableValueRange<float>(1f, 60f)));
            MaxManeuverWindow = config.Bind(envelope, "MaxManeuverWindow", 4.5f,
                new ConfigDescription("Advanced: max maneuver time for envelope (seconds).", new AcceptableValueRange<float>(0.1f, 20f)));
            AseSensitivityBias = config.Bind(envelope, "AseSensitivityBias", 1f,
                new ConfigDescription("Advanced: global envelope radius multiplier.", new AcceptableValueRange<float>(0.01f, 5f)));
            AseInterceptConfidence = config.Bind(envelope, "AseInterceptConfidence", 0.99f,
                new ConfigDescription("Advanced: intercept confidence target.", new AcceptableValueRange<float>(0.5f, 0.999f)));
            MinArmDistSlack = config.Bind(envelope, "MinArmDistSlack", 1f,
                new ConfigDescription("Advanced: arm-distance slack multiplier.", new AcceptableValueRange<float>(0.1f, 5f)));

            const string fireControl = "3. Fire Control & Geometry";
            ManualLaunchAimTolerance = config.Bind(fireControl, "ManualLaunchAimTolerance", 0f,
                new ConfigDescription(
                    "Angle tolerance for manual override shooting. 0 for strict ASE inside lock, 180 for immediate omnidirectional fire.",
                    new AcceptableValueRange<float>(0f, 180f)));
            RequireAseScreenShoot = config.Bind(fireControl, "RequireAseScreenShoot", false,
                "When true, hotkey salvo requires gun cross inside the on-screen ASE circle (first-person HUD). Default off — salvo uses threat/range gates only (works in third person and when looking away).");
            LaunchCooldown = config.Bind(fireControl, "LaunchCooldown", 0.35f,
                new ConfigDescription("Inter-shot station wait inside a pair (seconds). Pair hardware lock is fixed at 1.8s in code.", new AcceptableValueRange<float>(0.05f, 2f)));
            MissDistanceToleranceMeters = config.Bind(fireControl, "MissDistanceToleranceMeters", 50f,
                new ConfigDescription(
                    "Maximum allowed missile miss distance in meters for it to be considered a threat.",
                    new AcceptableValueRange<float>(10f, 200f)));
            MaxTimingTickDt = config.Bind(fireControl, "MaxTimingTickDt", 0f,
                new ConfigDescription("Advanced: optional salvo timing tick cap (0 = off).", new AcceptableValueRange<float>(0f, 1f)));

            const string signal = "4. Signal & Tracking";
            TtiSmoothingFactor = config.Bind(signal, "TtiSmoothingFactor", 0.08f,
                new ConfigDescription("TTI low-pass alpha. Lower = smoother; higher = closer to raw physics.", new AcceptableValueRange<float>(0.01f, 1f)));
            ClosureMinThreshold = config.Bind(signal, "ClosureMinThreshold", 0.1f,
                new ConfigDescription("Minimum closure speed (m/s) used as a floor for TTI and range gates.", new AcceptableValueRange<float>(0.01f, 100f)));

            const string warningTti = "WarningTTI";
            WarningTtiEnabled = config.Bind(warningTti, "Enabled", true, "Append TTI suffix to MWS threat labels.");

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
                AutoEngage = AutoEngage.Value,
                HotKeyModifier = HotKeyModifier.Value,
                HotKey = HotKey.Value,
                WeaponPriority = WeaponPriority.Value,
                WeaponFilterMode = WeaponFilterMode.Value,
                RenderAseCircle = RenderAseCircle.Value,
                RenderRadialText = RenderRadialText.Value,
                AseVisualScale = AseVisualScale.Value,
                MaxLaunchRangeMeters = MaxLaunchRangeMeters.Value,
                MinLaunchRangeMeters = MinLaunchRangeMeters.Value,
                AseMaxRangeFactor = AseMaxRangeFactor.Value,
                AsePreviewRangeFactor = AsePreviewRangeFactor.Value,
                AsePreviewAppearDistanceM = AsePreviewAppearDistanceM.Value,
                DefaultMaxTurnG = DefaultMaxTurnG.Value,
                MaxManeuverWindow = MaxManeuverWindow.Value,
                AseSensitivityBias = AseSensitivityBias.Value,
                AseInterceptConfidence = AseInterceptConfidence.Value,
                MinArmDistSlack = MinArmDistSlack.Value,
                ManualLaunchAimTolerance = ManualLaunchAimTolerance.Value,
                RequireAseScreenShoot = RequireAseScreenShoot.Value,
                LaunchCooldown = LaunchCooldown.Value,
                MissDistanceToleranceMeters = MissDistanceToleranceMeters.Value,
                MaxTimingTickDt = MaxTimingTickDt.Value,
                WarningTtiEnabled = WarningTtiEnabled.Value,
                TtiSmoothingFactor = TtiSmoothingFactor.Value,
                ClosureMinThreshold = ClosureMinThreshold.Value,
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
            AutoEngage.SettingChanged += OnAnySettingChanged;
            HotKeyModifier.SettingChanged += OnAnySettingChanged;
            HotKey.SettingChanged += OnAnySettingChanged;
            WeaponPriority.SettingChanged += OnAnySettingChanged;
            WeaponFilterMode.SettingChanged += OnAnySettingChanged;
            RenderAseCircle.SettingChanged += OnAnySettingChanged;
            RenderRadialText.SettingChanged += OnAnySettingChanged;
            AseVisualScale.SettingChanged += OnAnySettingChanged;
            MaxLaunchRangeMeters.SettingChanged += OnAnySettingChanged;
            MinLaunchRangeMeters.SettingChanged += OnAnySettingChanged;
            AseMaxRangeFactor.SettingChanged += OnAnySettingChanged;
            AsePreviewRangeFactor.SettingChanged += OnAnySettingChanged;
            AsePreviewAppearDistanceM.SettingChanged += OnAnySettingChanged;
            DefaultMaxTurnG.SettingChanged += OnAnySettingChanged;
            MaxManeuverWindow.SettingChanged += OnAnySettingChanged;
            AseSensitivityBias.SettingChanged += OnAnySettingChanged;
            AseInterceptConfidence.SettingChanged += OnAnySettingChanged;
            MinArmDistSlack.SettingChanged += OnAnySettingChanged;
            ManualLaunchAimTolerance.SettingChanged += OnAnySettingChanged;
            RequireAseScreenShoot.SettingChanged += OnAnySettingChanged;
            LaunchCooldown.SettingChanged += OnAnySettingChanged;
            MissDistanceToleranceMeters.SettingChanged += OnAnySettingChanged;
            MaxTimingTickDt.SettingChanged += OnAnySettingChanged;
            WarningTtiEnabled.SettingChanged += OnAnySettingChanged;
            TtiSmoothingFactor.SettingChanged += OnAnySettingChanged;
            ClosureMinThreshold.SettingChanged += OnAnySettingChanged;
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
        internal bool AutoEngage;
        internal KeyCode HotKeyModifier;
        internal KeyCode HotKey;
        internal WeaponPriority WeaponPriority;
        internal WeaponFilterMode WeaponFilterMode;
        internal bool RenderAseCircle;
        internal bool RenderRadialText;
        internal float AseVisualScale;
        internal float MaxLaunchRangeMeters;
        internal float MinLaunchRangeMeters;
        internal float AseMaxRangeFactor;
        internal float AsePreviewRangeFactor;
        internal float AsePreviewAppearDistanceM;
        internal float DefaultMaxTurnG;
        internal float MaxManeuverWindow;
        internal float AseSensitivityBias;
        internal float AseInterceptConfidence;
        internal float MinArmDistSlack;
        internal float ManualLaunchAimTolerance;
        internal bool RequireAseScreenShoot;
        internal float LaunchCooldown;
        internal float MissDistanceToleranceMeters;
        internal float MaxTimingTickDt;
        internal bool WarningTtiEnabled;
        internal float TtiSmoothingFactor;
        internal float ClosureMinThreshold;
    }
}
