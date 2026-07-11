using System.Collections.Generic;
using System.Reflection;
using NOCS.Config;
using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    internal enum MwsCollectMode : byte
    {
        Preview,
        Engage,
    }

    internal static class MwsThreatFilter
    {
        private const int MaxThreats = 8;
        private const float MinRelVelSqr = 0.001f;
        private const float MinDefensiveSpeedMps = 50f;

        private static readonly List<Missile> ActiveThreats = new List<Missile>(MaxThreats);
        private static readonly int[] CandidateIndices = new int[MaxThreats];
        private static readonly float[] CandidateDist = new float[MaxThreats];
        private static readonly float[] CandidateClosure = new float[MaxThreats];
        private static readonly List<Missile> ScanMissiles = new List<Missile>(16);

        private static FieldInfo? _unknownMissilesField;

        internal static void Bind(Aircraft aircraft)
        {
            Unbind();
        }

        internal static void Unbind()
        {
            ActiveThreats.Clear();
        }

        internal static IReadOnlyList<Missile> GetScratch(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            Collect(aircraft, ActiveThreats, defensiveStation, MwsCollectMode.Preview);
            return ActiveThreats;
        }

        internal static int CollectVisual(Aircraft aircraft, List<Missile> buffer, WeaponStation? defensiveStation)
        {
            return Collect(aircraft, buffer, defensiveStation, MwsCollectMode.Preview);
        }

        internal static int CollectEngage(
            Aircraft aircraft,
            List<Missile> buffer,
            WeaponStation? defensiveStation)
        {
            return Collect(aircraft, buffer, defensiveStation, MwsCollectMode.Engage);
        }

        internal static int Collect(
            Aircraft aircraft,
            List<Missile> buffer,
            WeaponStation? defensiveStation,
            MwsCollectMode mode)
        {
            buffer.Clear();
            if (!NocsGuard.IsValidUnit(aircraft) || aircraft!.rb == null)
                return 0;

            MissileWarning? warning = aircraft.GetMissileWarningSystem();
            if (warning?.knownMissiles == null)
                return 0;

            bool previewMode = mode == MwsCollectMode.Preview;
            BuildScanList(warning, previewMode);

            Camera? cam = SceneSingleton<CameraStateManager>.i?.mainCamera;
            if (cam == null && !previewMode)
                return 0;

            Vector3 camForward = cam != null ? cam.transform.forward : aircraft.transform.forward;
            Vector3 camPos = cam != null ? cam.transform.position : aircraft.rb.position;

            Rigidbody aircraftRb = aircraft.rb;
            Vector3 acPos = aircraftRb.position;
            Vector3 acVel = aircraftRb.velocity;

            PersistentID selfId = aircraft.persistentID;
            FactionHQ? playerHq = aircraft.NetworkHQ;
            bool applyArmGate = mode == MwsCollectMode.Engage;

            WeaponStation? activeWeapon = ResolveActiveWeapon(aircraft, defensiveStation);
            MsnParams msn = SeekerParamCache.GetMsnParams(activeWeapon);
            float tArm = msn.ArmDelaySec;
            float armSlack = Mathf.Max(0.1f, NocsConfigCache.MinArmDistSlack);
            float maxCpa = Mathf.Max(1f, NocsConfigCache.MaxCpaMeters);
            float maxMissileRange = Mathf.Max(1f, msn.MaxRangeM);
            float defensiveSpeed = Mathf.Max(MinDefensiveSpeedMps, msn.MaxSpeedMps);
            float launchCooldown = Mathf.Max(0f, msn.LaunchCooldownSec);
            float engageRangeFactor = Mathf.Max(0.01f, NocsConfigCache.AseMaxRangeFactor);
            float previewRangeFactor = Mathf.Max(0.01f, NocsConfigCache.AsePreviewRangeFactor);

            int candidateCount = 0;
            List<Missile> scan = ScanMissiles;

            for (int i = 0; i < scan.Count; i++)
            {
                Missile? missile = scan[i];
                if (missile == null)
                    continue;

                Rigidbody? missileRb = missile.rb;
                if (missileRb == null)
                    continue;

                // Rule 0: Guidance Check — intercept radar threats only (ARH / SARH).
                if (!IsRadarThreatOnSelf(missile, selfId, playerHq))
                    continue;

                Vector3 mPos = missileRb.position;
                if (!previewMode)
                {
                    Vector3 threatDir = mPos - camPos;
                    if (Vector3.Dot(camForward, threatDir) <= 0f)
                        continue;
                }

                Vector3 toAircraft = acPos - mPos;
                float dist = toAircraft.magnitude;
                if (dist < 1f)
                    continue;

                Vector3 los = toAircraft / dist;
                float closure = ThreatKinematics.ResolveClosure(
                    missileRb.velocity,
                    acVel,
                    los,
                    previewMode);
                if (closure <= 0f)
                    continue;

                Vector3 relVel = acVel - missileRb.velocity;
                if (!previewMode)
                {
                    float velSqr = relVel.sqrMagnitude;
                    if (velSqr < MinRelVelSqr)
                        continue;
                }

                if (!ThreatKinematics.TryResolveCpaMeters(
                        toAircraft,
                        relVel,
                        dist,
                        previewMode,
                        maxCpa,
                        out _))
                {
                    continue;
                }

                if (applyArmGate && dist <= closure * tArm * armSlack)
                    continue;

                if (candidateCount >= MaxThreats)
                    break;

                CandidateIndices[candidateCount] = i;
                CandidateDist[candidateCount] = dist;
                CandidateClosure[candidateCount] = closure;
                candidateCount++;
            }

            if (candidateCount == 0)
                return 0;

            float tSalvoQueue = candidateCount * launchCooldown;
            float previewRangeCap = maxMissileRange * previewRangeFactor;

            for (int c = 0; c < candidateCount; c++)
            {
                float dist = CandidateDist[c];
                float closure = CandidateClosure[c];
                bool passesRange = previewMode
                    ? PassesPreviewRangeGate(dist, previewRangeCap)
                    : PassesEngageRangeGate(
                        dist,
                        closure,
                        tSalvoQueue,
                        tArm,
                        armSlack,
                        maxMissileRange,
                        defensiveSpeed,
                        engageRangeFactor);

                if (!passesRange)
                    continue;

                Missile missile = scan[CandidateIndices[c]];
                TryAddUnique(buffer, missile);
            }

            return buffer.Count;
        }

        internal static bool PassesMaxRangeGate(
            float dist,
            float closure,
            int activeThreatsCount,
            WeaponStation? defensiveStation)
        {
            if (dist < 1f)
                return false;

            float effectiveClosure = Mathf.Max(closure, NocsConfigCache.MinClosureMps);
            if (effectiveClosure <= 0f)
                return false;

            MsnParams msn = SeekerParamCache.GetMsnParams(defensiveStation);
            float tSalvoQueue = Mathf.Max(0, activeThreatsCount) * Mathf.Max(0f, msn.LaunchCooldownSec);
            float armSlack = Mathf.Max(0.1f, NocsConfigCache.MinArmDistSlack);
            return PassesEngageRangeGate(
                dist,
                effectiveClosure,
                tSalvoQueue,
                msn.ArmDelaySec,
                armSlack,
                Mathf.Max(1f, msn.MaxRangeM),
                Mathf.Max(MinDefensiveSpeedMps, msn.MaxSpeedMps),
                Mathf.Max(0.01f, NocsConfigCache.AseMaxRangeFactor));
        }

        internal static bool IsInsideArmDeadZone(
            Missile missile,
            Aircraft aircraft,
            WeaponStation? defensiveStation)
        {
            if (!NocsGuard.IsValidMissile(missile) || !NocsGuard.IsValidUnit(aircraft))
                return true;

            Rigidbody? aircraftRb = aircraft!.rb;
            Rigidbody? missileRb = missile!.rb;
            if (aircraftRb == null || missileRb == null)
                return true;

            Vector3 toAircraft = aircraftRb.position - missileRb.position;
            float dist = toAircraft.magnitude;
            if (dist < 1f)
                return true;

            float closure = ThreatKinematics.ResolveClosure(
                missileRb.velocity,
                aircraftRb.velocity,
                toAircraft / dist,
                previewMode: false);
            if (closure <= 0f)
                return true;

            MsnParams msn = SeekerParamCache.GetMsnParams(defensiveStation);
            float armSlack = Mathf.Max(0.1f, NocsConfigCache.MinArmDistSlack);
            return dist <= closure * msn.ArmDelaySec * armSlack;
        }

        internal static WeaponStation? ResolveActiveWeapon(Aircraft aircraft, WeaponStation? preferred)
        {
            if (preferred != null && WeaponStationCatalog.IsEligibleStation(preferred, aircraft))
                return preferred;

            WeaponStation? selected = aircraft.weaponManager?.currentWeaponStation;
            if (selected != null && WeaponStationCatalog.IsEligibleStation(selected, aircraft))
                return selected;

            IReadOnlyList<WeaponStationEntry> stations = WeaponStationCatalog.Build(aircraft);
            if (stations.Count == 0)
                return null;

            return stations[0].Station;
        }

        private static void BuildScanList(MissileWarning warning, bool includeUnknown)
        {
            ScanMissiles.Clear();
            AppendUniqueMissiles(warning.knownMissiles);
            if (!includeUnknown)
                return;

            List<Missile>? unknown = ResolveUnknownMissiles(warning);
            if (unknown == null)
                return;

            AppendUniqueMissiles(unknown);
        }

        private static void AppendUniqueMissiles(List<Missile> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                Missile? missile = source[i];
                if (missile == null || ContainsMissile(missile))
                    continue;

                ScanMissiles.Add(missile);
            }
        }

        private static bool ContainsMissile(Missile missile)
        {
            for (int j = 0; j < ScanMissiles.Count; j++)
            {
                if (ScanMissiles[j] == missile)
                    return true;
            }

            return false;
        }

        private static List<Missile>? ResolveUnknownMissiles(MissileWarning warning)
        {
            _unknownMissilesField ??= typeof(MissileWarning).GetField(
                "unknownMissiles",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return _unknownMissilesField?.GetValue(warning) as List<Missile>;
        }

        private static bool PassesPreviewRangeGate(float dist, float previewRangeCap)
        {
            return dist <= previewRangeCap;
        }

        private static bool PassesEngageRangeGate(
            float dist,
            float closure,
            float tSalvoQueue,
            float tArm,
            float armSlack,
            float maxMissileRange,
            float defensiveSpeed,
            float rangeFactor)
        {
            float speed = Mathf.Max(defensiveSpeed, MinDefensiveSpeedMps);
            float ttiTarget = dist / speed;
            float leadBuffer = tArm * armSlack + tSalvoQueue;
            float closureRate = Mathf.Max(closure, NocsConfigCache.MinClosureMps);
            float kinematicRange = (ttiTarget + leadBuffer) * closureRate;
            float maxDynamicRange = Mathf.Min(maxMissileRange, kinematicRange) * rangeFactor;
            if (dist <= maxDynamicRange)
                return true;

            return dist <= maxMissileRange * rangeFactor;
        }

        private static bool IsRadarThreatOnSelf(Missile missile, PersistentID selfId, FactionHQ? playerHq)
        {
            if (!NocsGuard.IsValidMissile(missile))
                return false;

            if (missile.targetID != selfId)
                return false;

            if (playerHq != null && missile.NetworkHQ == playerHq)
                return false;

            SeekerKind kind = SeekerParamCache.ResolveSeekerKind(missile);
            if (SeekerParamCache.IsRadarSeeker(kind))
                return true;

            // Early ARH/SARH may report empty seeker string while already in active lock/search.
            if (kind == SeekerKind.None
                && missile.seekerMode < Missile.SeekerMode.passive)
            {
                return true;
            }

            return false;
        }

        private static void TryAddUnique(List<Missile> buffer, Missile missile)
        {
            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i] == missile)
                    return;
            }

            if (buffer.Count >= MaxThreats)
                return;

            buffer.Add(missile);
        }
    }
}
