using System.Reflection;
using NOCS.Config;
using UnityEngine;

namespace NOCS.HardKill
{
    internal struct MsnParams
    {
        internal SeekerKind Kind;
        internal float MaxSpeedMps;
        internal float MaxRangeM;
        internal float ArmDelaySec;
        internal float GuidanceDelaySec;
        internal float MaxLeadSec;
        internal float LaunchCooldownSec;
    }

    internal static class SeekerParamCache
    {
        private const float DefaultMaxLeadSec = 5f;
        private const float DefaultMaxSpeedMps = 800f;
        private const float DefaultMaxRangeM = 15000f;
        private const float DefaultTrackingAngleDeg = 60f;
        private const float DefaultArmDelaySec = 1f;
        private const float DefaultGuidanceDelaySec = 0.5f;
        private const float MinLaunchCooldownSec = 0.05f;

        private static FieldInfo? _arhArmDelay;
        private static FieldInfo? _arhGuidanceDelay;
        private static FieldInfo? _arhMaxLead;
        private static FieldInfo? _arhMaxTrackingAngle;
        private static FieldInfo? _irArmDelay;
        private static FieldInfo? _irGuidanceDelay;
        private static FieldInfo? _irMaxLead;
        private static FieldInfo? _sarhArmDelay;
        private static FieldInfo? _sarhGuidanceDelay;
        private static FieldInfo? _missileGLimit;

        private static WeaponStation? _msnCacheStation;
        private static MsnParams _msnCache;

        internal static MsnParams GetMsnParams(WeaponStation? station)
        {
            if (station == null)
                return BuildDefaultMsnParams(SeekerKind.None);

            if (ReferenceEquals(station, _msnCacheStation))
                return _msnCache;

            _msnCacheStation = station;
            _msnCache = BuildMsnParams(station);
            return _msnCache;
        }

        internal static void InvalidateMsnCache()
        {
            _msnCacheStation = null;
            _msnCache = default;
        }

        internal static float GetArmDelay(WeaponStation station)
        {
            return GetMsnParams(station).ArmDelaySec;
        }

        internal static float GetGuidanceDelay(WeaponStation station)
        {
            return GetMsnParams(station).GuidanceDelaySec;
        }

        internal static float GetMaxLead(WeaponStation station)
        {
            return GetMsnParams(station).MaxLeadSec;
        }

        internal static float GetMaxTrackingAngleDeg(WeaponStation station)
        {
            MissileSeeker? seeker = GetSeeker(station);
            if (seeker == null)
                return DefaultTrackingAngleDeg;

            if (ResolveSeekerKind(station) == SeekerKind.ARH)
            {
                float angle = ReadFloat(seeker, ref _arhMaxTrackingAngle, "maxTrackingAngle", DefaultTrackingAngleDeg);
                return angle > 0f ? angle : DefaultTrackingAngleDeg;
            }

            return DefaultTrackingAngleDeg;
        }

        internal static float GetMaxSpeedMps(WeaponStation station)
        {
            return GetMsnParams(station).MaxSpeedMps;
        }

        internal static float GetMaxTurnRateG(WeaponStation station)
        {
            GameObject? prefab = station?.WeaponInfo?.weaponPrefab;
            if (prefab == null)
                return NocsConfigCache.DefaultMaxTurnG;

            Missile? missile = prefab.GetComponent<Missile>();
            if (missile == null)
                return NocsConfigCache.DefaultMaxTurnG;

            float gLimit = ReadFloat(missile, ref _missileGLimit, "gLimit", 0f);
            if (gLimit > 0f)
                return gLimit;

            return NocsConfigCache.DefaultMaxTurnG;
        }

        internal static string GetStationSeekerType(WeaponStation? station)
        {
            MissileSeeker? seeker = GetSeeker(station);
            return seeker != null ? seeker.GetSeekerType() : string.Empty;
        }

        internal static SeekerKind ResolveSeekerKind(WeaponStation? station)
        {
            return ResolveSeekerKind(GetStationSeekerType(station));
        }

        internal static SeekerKind ResolveSeekerKind(Missile? missile)
        {
            if (missile == null)
                return SeekerKind.None;

            return ResolveSeekerKind(missile.GetSeekerType());
        }

        internal static SeekerKind ResolveSeekerKind(string seekerType)
        {
            if (string.IsNullOrEmpty(seekerType))
                return SeekerKind.None;

            if (seekerType == "IR")
                return SeekerKind.IR;

            if (seekerType == "ARH")
                return SeekerKind.ARH;

            if (seekerType == "SARH")
                return SeekerKind.SARH;

            return SeekerKind.Other;
        }

        internal static bool IsRadarSeeker(SeekerKind kind)
        {
            return kind == SeekerKind.ARH || kind == SeekerKind.SARH;
        }

        internal static bool IsRadarSeeker(string seekerType)
        {
            return IsRadarSeeker(ResolveSeekerKind(seekerType));
        }

        internal static bool IsIrSeeker(SeekerKind kind)
        {
            return kind == SeekerKind.IR;
        }

        internal static bool IsIrSeeker(string seekerType)
        {
            return IsIrSeeker(ResolveSeekerKind(seekerType));
        }

        private static MsnParams BuildMsnParams(WeaponStation station)
        {
            SeekerKind kind = ResolveSeekerKind(station);
            MissileSeeker? seeker = GetSeeker(station);
            WeaponInfo? info = station.WeaponInfo;

            float maxSpeed = DefaultMaxSpeedMps;
            float maxRange = DefaultMaxRangeM;
            if (info != null)
            {
                float speed = info.GetMaxSpeed();
                if (speed > 50f)
                    maxSpeed = speed;

                float range = info.targetRequirements.maxRange;
                if (range > 1f)
                    maxRange = range;
            }

            float armDelay = DefaultArmDelaySec;
            float guidanceDelay = DefaultGuidanceDelaySec;
            float maxLead = DefaultMaxLeadSec;

            if (seeker != null)
            {
                switch (kind)
                {
                    case SeekerKind.IR:
                        armDelay = ReadFloat(seeker, ref _irArmDelay, "armDelay", 0.5f);
                        guidanceDelay = ReadFloat(seeker, ref _irGuidanceDelay, "guidanceDelay", 0.25f);
                        maxLead = ReadFloat(seeker, ref _irMaxLead, "maxLead", DefaultMaxLeadSec);
                        break;
                    case SeekerKind.ARH:
                        armDelay = ReadFloat(seeker, ref _arhArmDelay, "armDelay", 1f);
                        guidanceDelay = ReadFloat(seeker, ref _arhGuidanceDelay, "guidanceDelay", 1f);
                        maxLead = ReadFloat(seeker, ref _arhMaxLead, "maxLead", DefaultMaxLeadSec);
                        break;
                    case SeekerKind.SARH:
                        armDelay = ReadFloat(seeker, ref _sarhArmDelay, "armDelay", 2f);
                        guidanceDelay = ReadFloat(seeker, ref _sarhGuidanceDelay, "guidanceDelay", 0f);
                        break;
                }
            }

            float fireInterval = info != null ? info.fireInterval : 0f;
            float launchCooldown = Mathf.Max(MinLaunchCooldownSec, NocsConfigCache.LaunchCooldown);
            if (fireInterval > MinLaunchCooldownSec)
                launchCooldown = Mathf.Max(launchCooldown, fireInterval);

            return new MsnParams
            {
                Kind = kind,
                MaxSpeedMps = maxSpeed,
                MaxRangeM = maxRange,
                ArmDelaySec = armDelay,
                GuidanceDelaySec = guidanceDelay,
                MaxLeadSec = maxLead,
                LaunchCooldownSec = launchCooldown,
            };
        }

        private static MsnParams BuildDefaultMsnParams(SeekerKind kind)
        {
            return new MsnParams
            {
                Kind = kind,
                MaxSpeedMps = DefaultMaxSpeedMps,
                MaxRangeM = DefaultMaxRangeM,
                ArmDelaySec = DefaultArmDelaySec,
                GuidanceDelaySec = DefaultGuidanceDelaySec,
                MaxLeadSec = DefaultMaxLeadSec,
                LaunchCooldownSec = Mathf.Max(MinLaunchCooldownSec, NocsConfigCache.LaunchCooldown),
            };
        }

        private static MissileSeeker? GetSeeker(WeaponStation? station)
        {
            GameObject? prefab = station?.WeaponInfo?.weaponPrefab;
            return prefab != null ? prefab.GetComponent<MissileSeeker>() : null;
        }

        private static float ReadFloat(object target, ref FieldInfo? cache, string name, float fallback)
        {
            cache ??= target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (cache == null)
                return fallback;

            object? value = cache.GetValue(target);
            return value is float f ? f : fallback;
        }
    }
}
