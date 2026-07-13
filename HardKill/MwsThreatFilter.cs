using System.Collections.Generic;
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

        private static readonly List<Missile> PreviewScratch = new List<Missile>(MaxThreats);
        private static readonly List<Missile> EngageScratch = new List<Missile>(MaxThreats);
        private static readonly List<Missile> SalvoScratch = new List<Missile>(MaxThreats);
        private static readonly List<Missile> FireControlScratch = new List<Missile>(MaxThreats);
        private static readonly int[] CandidateIndices = new int[MaxThreats];
        private static readonly float[] CandidateDist = new float[MaxThreats];
        private static readonly float[] CandidateClosure = new float[MaxThreats];
        private static readonly List<Missile> ScanMissiles = new List<Missile>(16);

        private static int _cachedPreviewFrame = -1;
        private static Aircraft? _cachedPreviewAircraft;
        private static WeaponStation? _cachedPreviewStation;
        private static int _cachedEngageFrame = -1;
        private static Aircraft? _cachedEngageAircraft;
        private static WeaponStation? _cachedEngageStation;
        private static int _cachedSalvoFrame = -1;
        private static Aircraft? _cachedSalvoAircraft;
        private static int _cachedFireControlFrame = -1;
        private static Aircraft? _cachedFireControlAircraft;
        private static WeaponStation? _cachedFireControlStation;

        private static int _activeWeaponFrame = -1;
        private static Aircraft? _activeWeaponAircraft;
        private static WeaponStation? _activeWeaponPreferred;
        private static WeaponStation? _activeWeaponResult;

        internal static void Bind(Aircraft aircraft)
        {
            Unbind();
        }

        internal static void Unbind()
        {
            PreviewScratch.Clear();
            EngageScratch.Clear();
            SalvoScratch.Clear();
            FireControlScratch.Clear();
            InvalidateFrameCache();
        }

        internal static void InvalidateFrameCache()
        {
            _cachedPreviewFrame = -1;
            _cachedPreviewAircraft = null;
            _cachedPreviewStation = null;
            _cachedEngageFrame = -1;
            _cachedEngageAircraft = null;
            _cachedEngageStation = null;
            _cachedSalvoFrame = -1;
            _cachedSalvoAircraft = null;
            _cachedFireControlFrame = -1;
            _cachedFireControlAircraft = null;
            _cachedFireControlStation = null;
            _activeWeaponFrame = -1;
            _activeWeaponAircraft = null;
            _activeWeaponPreferred = null;
            _activeWeaponResult = null;
        }

        internal static IReadOnlyList<Missile> GetScratch(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            return GetPreviewScratch(aircraft, defensiveStation);
        }

        internal static IReadOnlyList<Missile> GetPreviewScratch(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            int frame = Time.frameCount;
            if (ReferenceEquals(_cachedPreviewAircraft, aircraft)
                && ReferenceEquals(_cachedPreviewStation, defensiveStation)
                && _cachedPreviewFrame == frame)
            {
                return PreviewScratch;
            }

            _cachedPreviewFrame = frame;
            _cachedPreviewAircraft = aircraft;
            _cachedPreviewStation = defensiveStation;
            Collect(aircraft, PreviewScratch, defensiveStation, MwsCollectMode.Preview);
            return PreviewScratch;
        }

        internal static IReadOnlyList<Missile> GetEngageScratch(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            int frame = Time.frameCount;
            if (ReferenceEquals(_cachedEngageAircraft, aircraft)
                && ReferenceEquals(_cachedEngageStation, defensiveStation)
                && _cachedEngageFrame == frame)
            {
                return EngageScratch;
            }

            _cachedEngageFrame = frame;
            _cachedEngageAircraft = aircraft;
            _cachedEngageStation = defensiveStation;
            Collect(aircraft, EngageScratch, defensiveStation, MwsCollectMode.Engage);
            return EngageScratch;
        }

        internal static IReadOnlyList<Missile> GetSalvoScratch(Aircraft aircraft)
        {
            int frame = Time.frameCount;
            if (ReferenceEquals(_cachedSalvoAircraft, aircraft) && _cachedSalvoFrame == frame)
                return SalvoScratch;

            _cachedSalvoFrame = frame;
            _cachedSalvoAircraft = aircraft;
            CollectSalvoRaw(aircraft, SalvoScratch);
            return SalvoScratch;
        }

        /// <summary>
        /// ASE / SHOOT sample — preview threats confirmed on local RWR only.
        /// </summary>
        internal static IReadOnlyList<Missile> GetFireControlScratch(
            Aircraft aircraft,
            WeaponStation? defensiveStation)
        {
            int frame = Time.frameCount;
            if (ReferenceEquals(_cachedFireControlAircraft, aircraft)
                && ReferenceEquals(_cachedFireControlStation, defensiveStation)
                && _cachedFireControlFrame == frame)
            {
                return FireControlScratch;
            }

            _cachedFireControlFrame = frame;
            _cachedFireControlAircraft = aircraft;
            _cachedFireControlStation = defensiveStation;
            FireControlScratch.Clear();

            if (!MwsRwrGate.HasRwrPicture(aircraft))
                return FireControlScratch;

            IReadOnlyList<Missile> preview = GetPreviewScratch(aircraft, defensiveStation);
            for (int i = 0; i < preview.Count; i++)
                TryAddUnique(FireControlScratch, preview[i]);

            return FireControlScratch;
        }

        internal static int CountSalvoThreats(Aircraft aircraft)
        {
            return GetSalvoScratch(aircraft).Count;
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

        internal static int CollectSalvoRaw(Aircraft aircraft, List<Missile> buffer)
        {
            buffer.Clear();
            if (!NocsGuard.IsValidUnit(aircraft) || aircraft!.rb == null)
                return 0;

            MissileWarning? warning = aircraft.GetMissileWarningSystem();
            if (warning?.knownMissiles == null || !MwsRwrGate.HasRwrPicture(aircraft))
                return 0;

            MwsRwrGate.AppendRwrScanList(warning, ScanMissiles);

            Vector3 acPos = aircraft.rb.position;
            Vector3 noseForward = aircraft.transform.forward;
            PersistentID selfId = aircraft.persistentID;
            FactionHQ? playerHq = aircraft.NetworkHQ;
            float maxRange = NocsConfigCache.MaxLaunchRangeMeters;

            List<Missile> scan = ScanMissiles;
            int candidateCount = 0;
            for (int i = 0; i < scan.Count; i++)
            {
                Missile? missile = scan[i];
                if (missile == null)
                    continue;

                Rigidbody? missileRb = missile.rb;
                if (missileRb == null)
                    continue;

                Vector3 mPos = missileRb.position;

                if (!IncomingThreatPolicy.PassesNoseFov(noseForward, acPos, missile, mPos))
                    continue;

                if (!IsEngageableThreatOnSelf(aircraft, warning, missile, selfId, playerHq))
                    continue;

                float dist = (acPos - mPos).magnitude;
                if (dist > maxRange)
                    continue;

                TryAddSalvoCandidate(i, dist, ref candidateCount);
            }

            for (int c = 0; c < candidateCount; c++)
            {
                Missile missile = scan[CandidateIndices[c]];
                TryAddUnique(buffer, missile);
            }

            SortBufferByDistance(buffer, acPos);
            return buffer.Count;
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
            if (warning?.knownMissiles == null || !MwsRwrGate.HasRwrPicture(aircraft))
                return 0;

            bool previewMode = mode == MwsCollectMode.Preview;
            MwsRwrGate.AppendRwrScanList(warning, ScanMissiles);

            Camera? cam = SceneSingleton<CameraStateManager>.i?.mainCamera;
            if (cam == null && !previewMode)
                return 0;

            Vector3 camForward = cam != null ? cam.transform.forward : aircraft.transform.forward;
            Vector3 camPos = cam != null ? cam.transform.position : aircraft.rb.position;

            Rigidbody aircraftRb = aircraft.rb;
            Vector3 acPos = aircraftRb.position;
            Vector3 acVel = aircraftRb.velocity;
            Vector3 noseForward = aircraft.transform.forward;

            PersistentID selfId = aircraft.persistentID;
            FactionHQ? playerHq = aircraft.NetworkHQ;
            bool applyArmGate = mode == MwsCollectMode.Engage;

            WeaponStation? activeWeapon = ResolveActiveWeapon(aircraft, defensiveStation);
            MsnParams msn = SeekerParamCache.GetMsnParams(activeWeapon);
            float tArm = msn.ArmDelaySec;
            float armSlack = Mathf.Max(0.1f, NocsConfigCache.MinArmDistSlack);
            float maxCpa = Mathf.Max(1f, NocsConfigCache.MissDistanceToleranceMeters);
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

                Vector3 mPos = missileRb.position;

                if (!IncomingThreatPolicy.PassesNoseFov(noseForward, acPos, missile, mPos))
                    continue;

                if (!IsEngageableThreatOnSelf(aircraft, warning, missile, selfId, playerHq))
                    continue;

                if (!previewMode)
                {
                    Vector3 threatDir = mPos - camPos;
                    if (Vector3.Dot(camForward, threatDir) <= 0f)
                        continue;
                }

                Vector3 toAircraft = acPos - mPos;
                float dist = toAircraft.magnitude;
                if (!previewMode && dist < NocsConfigCache.MinLaunchRangeMeters)
                    continue;

                if (dist > NocsConfigCache.MaxLaunchRangeMeters)
                    continue;

                Vector3 los = toAircraft / dist;
                float closure = previewMode
                    ? ThreatKinematics.ResolvePreviewClosure(missile, missileRb.velocity, acVel, los)
                    : ThreatKinematics.ResolveClosure(
                        missileRb.velocity,
                        acVel,
                        los,
                        previewMode: false);
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
                    if (!(previewMode && ThreatKinematics.PassesPreviewAppearDistance(dist)))
                        continue;
                }

                if (applyArmGate && dist <= closure * tArm * armSlack)
                    continue;

                if (candidateCount < MaxThreats)
                {
                    CandidateIndices[candidateCount] = i;
                    CandidateDist[candidateCount] = dist;
                    CandidateClosure[candidateCount] = closure;
                    candidateCount++;
                    continue;
                }

                int farthest = 0;
                float farthestDist = CandidateDist[0];
                for (int c = 1; c < candidateCount; c++)
                {
                    if (CandidateDist[c] <= farthestDist)
                        continue;

                    farthestDist = CandidateDist[c];
                    farthest = c;
                }

                if (dist >= farthestDist)
                    continue;

                CandidateIndices[farthest] = i;
                CandidateDist[farthest] = dist;
                CandidateClosure[farthest] = closure;
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

            if (dist < NocsConfigCache.MinLaunchRangeMeters)
                return false;

            if (dist > NocsConfigCache.MaxLaunchRangeMeters)
                return false;

            float effectiveClosure = Mathf.Max(closure, NocsConfigCache.ClosureMinThreshold);
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
            int frame = Time.frameCount;
            if (_activeWeaponFrame == frame
                && ReferenceEquals(_activeWeaponAircraft, aircraft)
                && _activeWeaponPreferred == preferred)
            {
                return _activeWeaponResult;
            }

            WeaponStation? resolved;
            if (preferred != null && WeaponStationCatalog.IsEligibleStation(preferred, aircraft))
            {
                resolved = preferred;
            }
            else
            {
                // Hard-Kill ASE/SHOOT must match launch priority: IR-first, then radar.
                IReadOnlyList<WeaponStationEntry> stations = WeaponStationCatalog.Build(aircraft);
                if (IncomingThreatPolicy.ShouldUseIrInterceptPhase(stations))
                {
                    WeaponStationEntry? ir = PickCatalogStation(stations, wantIr: true);
                    resolved = ir?.Station;
                }
                else
                {
                    WeaponStationEntry? radar = PickCatalogStation(stations, wantIr: false);
                    resolved = radar?.Station ?? (stations.Count > 0 ? stations[0].Station : null);
                }
            }

            _activeWeaponFrame = frame;
            _activeWeaponAircraft = aircraft;
            _activeWeaponPreferred = preferred;
            _activeWeaponResult = resolved;
            return resolved;
        }

        private static WeaponStationEntry? PickCatalogStation(
            IReadOnlyList<WeaponStationEntry> stations,
            bool wantIr)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                WeaponStationEntry entry = stations[i];
                if (wantIr && !entry.IsIr)
                    continue;
                if (!wantIr && !entry.IsRadar)
                    continue;
                return entry;
            }

            return null;
        }

        private static bool PassesPreviewRangeGate(float dist, float previewRangeCap)
        {
            float cap = Mathf.Max(previewRangeCap, NocsConfigCache.AsePreviewAppearDistanceM);
            return dist <= cap;
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
            float closureRate = Mathf.Max(closure, NocsConfigCache.ClosureMinThreshold);
            float kinematicRange = (ttiTarget + leadBuffer) * closureRate;
            float maxDynamicRange = Mathf.Min(maxMissileRange, kinematicRange) * rangeFactor;
            if (dist <= maxDynamicRange)
                return true;

            return dist <= maxMissileRange * rangeFactor;
        }

        private static bool IsSalvoThreatOnSelf(Missile? missile, PersistentID selfId, FactionHQ? playerHq)
        {
            if (!NocsGuard.IsValidMissile(missile))
                return false;

            if (missile!.targetID != selfId)
                return false;

            if (playerHq != null && missile.NetworkHQ == playerHq)
                return false;

            return true;
        }

        private static void SortBufferByDistance(List<Missile> buffer, Vector3 acPos)
        {
            int count = buffer.Count;
            if (count <= 1)
                return;

            for (int i = 1; i < count; i++)
            {
                Missile key = buffer[i];
                float keyDist = DistanceTo(key, acPos);
                int j = i - 1;
                while (j >= 0 && DistanceTo(buffer[j], acPos) > keyDist)
                {
                    buffer[j + 1] = buffer[j];
                    j--;
                }

                buffer[j + 1] = key;
            }
        }

        private static float DistanceTo(Missile missile, Vector3 acPos)
        {
            if (missile == null || missile.rb == null)
                return float.MaxValue;

            return (acPos - missile.rb.position).magnitude;
        }

        private static bool IsEngageableThreatOnSelf(
            Aircraft aircraft,
            MissileWarning warning,
            Missile? missile,
            PersistentID selfId,
            FactionHQ? playerHq)
        {
            if (!NocsGuard.IsValidMissile(missile))
                return false;

            return MwsRwrGate.IsEngageableRwrThreat(
                aircraft,
                warning,
                missile!,
                selfId,
                playerHq);
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

        private static void TryAddSalvoCandidate(int scanIndex, float dist, ref int candidateCount)
        {
            if (candidateCount < MaxThreats)
            {
                CandidateIndices[candidateCount] = scanIndex;
                CandidateDist[candidateCount] = dist;
                candidateCount++;
                return;
            }

            int farthest = 0;
            float farthestDist = CandidateDist[0];
            for (int k = 1; k < MaxThreats; k++)
            {
                if (CandidateDist[k] <= farthestDist)
                    continue;

                farthest = k;
                farthestDist = CandidateDist[k];
            }

            if (dist >= farthestDist)
                return;

            CandidateIndices[farthest] = scanIndex;
            CandidateDist[farthest] = dist;
        }

        private static void TrimToNearest(List<Missile> buffer, Vector3 acPos, int keep)
        {
            SortBufferByDistance(buffer, acPos);
            if (buffer.Count <= keep)
                return;

            buffer.RemoveRange(keep, buffer.Count - keep);
        }
    }
}
