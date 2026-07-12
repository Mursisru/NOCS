using System.Collections.Generic;
using NOCS.Config;
using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    internal sealed class WeaponAllocationEngine
    {
        private const int MaxQueue = 8;

        private readonly List<Missile> _threatQueue = new List<Missile>(MaxQueue);
        private readonly List<Missile> _refreshScratch = new List<Missile>(MaxQueue);
        private readonly float[] _impactScratch = new float[MaxQueue];

        private readonly LaunchTimingGate _salvoGate = new LaunchTimingGate();

        private WeaponStationEntry? _activeStation;
        private Missile? _primaryThreat;
        private int _queueIndex;
        private int _sessionLaunchBudget;
        private int _sessionLaunchesUsed;
        private bool _queueComplete;

        internal Missile? PrimaryThreat => _primaryThreat;

        internal int QueueThreatCount => _threatQueue.Count;

        internal Missile? CurrentThreat
        {
            get
            {
                if (_queueIndex < 0 || _queueIndex >= _threatQueue.Count)
                    return null;
                return _threatQueue[_queueIndex];
            }
        }

        internal bool QueueComplete => _queueComplete;

        internal bool IsSalvoComplete => _queueComplete;

        internal WeaponStation? ResolveGateStation(WeaponStation? fallback)
        {
            if (_activeStation.HasValue)
                return _activeStation.Value.Station;
            return fallback;
        }

        internal void ExtendEngagement(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
                return;

            SyncEngageQueue(aircraft, defensiveStation);
            RefreshLaunchBudget(aircraft);

            if (_threatQueue.Count == 0)
                return;

            if (_sessionLaunchesUsed >= _sessionLaunchBudget && !HasUnengagedThreat())
                return;

            _queueIndex = FindNextEngageIndex();
            if (_queueIndex < _threatQueue.Count)
            {
                _queueComplete = false;
                _primaryThreat = _threatQueue[_queueIndex];
                SelectActiveStation(aircraft);
                RepointTargets(aircraft);
            }
            else if (WeaponStationCatalog.CountEligibleAmmo(aircraft) > 0)
            {
                _queueComplete = false;
            }

            _salvoGate.MarkIrPending();
        }

        internal void Reset()
        {
            _threatQueue.Clear();
            _activeStation = null;
            _primaryThreat = null;
            _queueIndex = 0;
            _sessionLaunchBudget = 0;
            _sessionLaunchesUsed = 0;
            _queueComplete = false;
            _salvoGate.Reset();
            WeaponStationCatalog.InvalidateFrameCache();
        }

        internal bool PrepareEngagement(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            if (!NocsGuard.CanMutateLocalWeapons(aircraft))
                return false;

            Reset();
            PopulateThreatQueue(aircraft, defensiveStation);
            RemoveAlreadyEngagedThreats();

            if (_threatQueue.Count == 0)
                return false;

            SortQueueByImpact(aircraft);
            _primaryThreat = _threatQueue[0];
            _queueIndex = 0;
            RefreshLaunchBudget(aircraft);

            if (_sessionLaunchBudget <= 0)
                return false;

            if (!SelectActiveStation(aircraft))
                return false;

            _salvoGate.MarkIrPending();
            return true;
        }

        internal void SyncEngageQueue(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            IReadOnlyList<Missile> scratch = MwsThreatFilter.GetSalvoScratch(aircraft);
            if (scratch.Count == 0)
                return;

            _refreshScratch.Clear();
            for (int i = 0; i < scratch.Count; i++)
            {
                Missile candidate = scratch[i];
                if (ContainsThreatInQueue(candidate))
                    continue;

                if (ThreatEngagementLedger.WasEngaged(candidate.persistentID))
                    continue;

                if (_threatQueue.Count + _refreshScratch.Count >= MaxQueue)
                    break;

                _refreshScratch.Add(candidate);
            }

            if (_refreshScratch.Count == 0)
                return;

            SortQueueByImpact(aircraft, _refreshScratch, _impactScratch);

            for (int i = 0; i < _refreshScratch.Count; i++)
                _threatQueue.Add(_refreshScratch[i]);

            RefreshLaunchBudget(aircraft);
            if (_sessionLaunchesUsed < _sessionLaunchBudget && HasUnengagedThreat())
                _queueComplete = false;
        }

        internal bool TryKeepSessionAlive(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            if (!NocsGuard.IsLocalPlayerAircraft(aircraft))
                return false;

            SyncEngageQueue(aircraft, defensiveStation);
            RefreshLaunchBudget(aircraft);

            if (WeaponStationCatalog.CountEligibleAmmo(aircraft) <= 0)
            {
                _queueComplete = true;
                return false;
            }

            if (!HasUnengagedThreat())
            {
                IReadOnlyList<Missile> live = MwsThreatFilter.GetSalvoScratch(aircraft);
                for (int i = 0; i < live.Count; i++)
                {
                    Missile threat = live[i];
                    if (!NocsGuard.IsValidMissile(threat))
                        continue;

                    if (ThreatEngagementLedger.WasEngaged(threat.persistentID))
                        continue;

                    if (_threatQueue.Count >= MaxQueue)
                        break;

                    _threatQueue.Add(threat);
                }

                RefreshLaunchBudget(aircraft);
            }

            int next = FindNextEngageIndex();
            if (next >= _threatQueue.Count)
            {
                _queueComplete = true;
                return false;
            }

            _queueComplete = false;
            _queueIndex = next;
            _primaryThreat = _threatQueue[next];
            SelectActiveStation(aircraft);
            RepointTargets(aircraft);
            if (!_salvoGate.IsIrReady())
                _salvoGate.MarkIrPending();
            return true;
        }

        internal void RepointTargets(Aircraft aircraft)
        {
            if (!NocsGuard.CanMutateLocalWeapons(aircraft))
                return;

            Missile? threat = CurrentThreat;
            if (!NocsGuard.IsValidUnit(aircraft) || aircraft!.weaponManager == null || threat == null)
                return;

            WeaponManager wm = aircraft.weaponManager;
            wm.ClearTargetList();
            wm.AddTargetList(threat);
            aircraft.NetworkHQ?.CmdUpdateTrackingInfo(threat.persistentID);
            wm.TargetListChanged();
        }

        internal bool TrySalvoStep(
            Aircraft aircraft,
            float dt,
            WeaponStation? defensiveStation,
            out int launchesStarted)
        {
            launchesStarted = 0;
            if (!NocsGuard.IsLocalPlayerAircraft(aircraft) || _queueComplete)
                return false;

            _salvoGate.Tick(dt);
            if (!_salvoGate.IsIrReady())
                return false;

            SyncEngageQueue(aircraft, defensiveStation);
            RefreshLaunchBudget(aircraft);

            if (_sessionLaunchesUsed >= _sessionLaunchBudget)
            {
                if (!HasUnengagedThreat())
                {
                    _queueComplete = true;
                    return false;
                }

                RefreshLaunchBudget(aircraft);
                if (_sessionLaunchesUsed >= _sessionLaunchBudget)
                {
                    _queueComplete = true;
                    return false;
                }
            }

            if (_queueIndex >= _threatQueue.Count || CurrentThreat == null)
                _queueIndex = FindNextEngageIndex();

            while (_queueIndex < _threatQueue.Count)
            {
                Missile? threat = CurrentThreat;
                if (threat == null || !NocsGuard.IsValidMissile(threat))
                {
                    AdvanceQueue(aircraft);
                    continue;
                }

                if (ThreatEngagementLedger.WasEngaged(threat.persistentID))
                {
                    AdvanceQueue(aircraft);
                    continue;
                }

                SelectActiveStation(aircraft);

                if (!TryLaunchSingle(aircraft, threat))
                {
                    if (WeaponStationCatalog.CountEligibleAmmo(aircraft) <= 0)
                    {
                        _queueComplete = true;
                        return launchesStarted > 0;
                    }

                    if (!WeaponStationCatalog.HasLaunchableStation(aircraft))
                    {
                        float wait = ResolveSalvoWait(aircraft, defensiveStation);
                        _salvoGate.BeginWait(wait);
                        return launchesStarted > 0;
                    }

                    return launchesStarted > 0;
                }

                ThreatEngagementLedger.MarkEngaged(threat.persistentID);
                _sessionLaunchesUsed++;
                launchesStarted++;
                AdvanceQueue(aircraft);
                WeaponStationCatalog.InvalidateFrameCache();

                if (_sessionLaunchesUsed >= _sessionLaunchBudget)
                    break;

                if (!HasUnengagedThreat())
                {
                    SyncEngageQueue(aircraft, defensiveStation);
                    RefreshLaunchBudget(aircraft);
                }

                if (HasUnengagedThreat()
                    && _sessionLaunchesUsed < _sessionLaunchBudget
                    && WeaponStationCatalog.HasLaunchableStation(aircraft))
                {
                    _queueIndex = FindNextEngageIndex();
                    continue;
                }

                break;
            }

            if (launchesStarted > 0)
            {
                float cooldown = ResolveSalvoWait(aircraft, defensiveStation);
                _salvoGate.BeginWait(cooldown);
                return true;
            }

            SyncEngageQueue(aircraft, defensiveStation);
            RefreshLaunchBudget(aircraft);
            if (HasUnengagedThreat() && _sessionLaunchesUsed < _sessionLaunchBudget)
            {
                _queueIndex = FindNextEngageIndex();
                _queueComplete = false;
                return false;
            }

            _queueComplete = !HasUnengagedThreat()
                || _sessionLaunchesUsed >= _sessionLaunchBudget
                || WeaponStationCatalog.CountEligibleAmmo(aircraft) <= 0;
            return false;
        }

        private float ResolveSalvoWait(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            float cooldown = SeekerParamCache.GetMsnParams(
                _activeStation.HasValue ? _activeStation.Value.Station : defensiveStation).LaunchCooldownSec;
            return Mathf.Max(0.05f, cooldown);
        }

        internal bool TryResumeIncompleteSalvo(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            return TryKeepSessionAlive(aircraft, defensiveStation);
        }

        internal int RunSalvo(Aircraft aircraft, float dt, WeaponStation? defensiveStation)
        {
            int started;
            TrySalvoStep(aircraft, dt, defensiveStation, out started);
            return started;
        }

        private void RemoveAlreadyEngagedThreats()
        {
            for (int i = _threatQueue.Count - 1; i >= 0; i--)
            {
                Missile threat = _threatQueue[i];
                if (ThreatEngagementLedger.WasEngaged(threat.persistentID))
                    _threatQueue.RemoveAt(i);
            }
        }

        private void AdvanceQueue(Aircraft aircraft)
        {
            _queueIndex++;

            if (_queueIndex < _threatQueue.Count)
            {
                _primaryThreat = _threatQueue[_queueIndex];
                SelectActiveStation(aircraft);
                RepointTargets(aircraft);
                return;
            }

            _primaryThreat = null;
            _activeStation = null;

            int next = FindNextEngageIndex();
            if (next < _threatQueue.Count)
            {
                _queueIndex = next;
                _primaryThreat = _threatQueue[next];
                SelectActiveStation(aircraft);
                RepointTargets(aircraft);
                _queueComplete = false;
                return;
            }

            if (_sessionLaunchesUsed >= _sessionLaunchBudget || WeaponStationCatalog.CountEligibleAmmo(aircraft) <= 0)
                _queueComplete = true;
            else
                _queueComplete = false;
        }

        private bool HasUnengagedThreat()
        {
            return FindNextEngageIndex() < _threatQueue.Count;
        }

        private void PopulateThreatQueue(Aircraft aircraft, WeaponStation? defensiveStation)
        {
            _threatQueue.Clear();
            IReadOnlyList<Missile> scratch = MwsThreatFilter.GetSalvoScratch(aircraft);
            for (int i = 0; i < scratch.Count; i++)
                _threatQueue.Add(scratch[i]);
        }

        private int FindNextEngageIndex()
        {
            for (int i = 0; i < _threatQueue.Count; i++)
            {
                Missile threat = _threatQueue[i];
                if (!NocsGuard.IsValidMissile(threat))
                    continue;

                if (!ThreatEngagementLedger.WasEngaged(threat.persistentID))
                    return i;
            }

            return _threatQueue.Count;
        }

        private bool SelectActiveStation(Aircraft aircraft)
        {
            IReadOnlyList<WeaponStationEntry> stations = WeaponStationCatalog.Build(aircraft);
            if (stations.Count == 0)
            {
                _activeStation = null;
                return false;
            }

            bool irFirst = NocsConfigCache.WeaponPriority == WeaponPriority.IR_First;
            WeaponStationEntry? picked = PickFirstStation(stations, irFirst, preferIr: true)
                ?? PickFirstStation(stations, irFirst, preferIr: false);

            if (picked == null)
            {
                _activeStation = null;
                return false;
            }

            _activeStation = picked;
            return true;
        }

        private static WeaponStationEntry? PickFirstStation(
            IReadOnlyList<WeaponStationEntry> stations,
            bool irFirst,
            bool preferIr)
        {
            bool wantIr = irFirst == preferIr;
            for (int i = 0; i < stations.Count; i++)
            {
                WeaponStationEntry entry = stations[i];
                if (wantIr && entry.IsIr)
                    return entry;
                if (!wantIr && entry.IsRadar)
                    return entry;
            }

            return null;
        }

        private bool TryLaunchSingle(Aircraft aircraft, Missile threat)
        {
            IReadOnlyList<WeaponStationEntry> stations = WeaponStationCatalog.Build(aircraft);
            if (stations.Count == 0)
                return false;

            bool irFirst = NocsConfigCache.WeaponPriority == WeaponPriority.IR_First;
            if (TryLaunchFromStationGroup(aircraft, threat, stations, irFirst, preferIr: true))
                return true;

            return TryLaunchFromStationGroup(aircraft, threat, stations, irFirst, preferIr: false);
        }

        private bool TryLaunchFromStationGroup(
            Aircraft aircraft,
            Missile threat,
            IReadOnlyList<WeaponStationEntry> stations,
            bool irFirst,
            bool preferIr)
        {
            bool wantIr = irFirst == preferIr;
            for (int i = 0; i < stations.Count; i++)
            {
                WeaponStationEntry entry = stations[i];
                if (wantIr && !entry.IsIr)
                    continue;

                if (!wantIr && !entry.IsRadar)
                    continue;

                WeaponStation station = entry.Station;
                if (station.Ammo <= 0 || !station.Ready())
                    continue;

                if (!HotTriggerGate.IsCommittedSalvoLaunchAllowed(aircraft, threat, entry.Station))
                    continue;

                if (!TryLaunch(aircraft, entry, threat))
                    continue;

                _activeStation = entry;
                return true;
            }

            return false;
        }

        private void RefreshLaunchBudget(Aircraft aircraft)
        {
            int ammo = WeaponStationCatalog.CountEligibleAmmo(aircraft);
            if (ammo <= 0)
                ammo = WeaponStationCatalog.CountAvailableAmmo(aircraft);

            int pendingThreats = CountUnengagedThreats();
            int remainingNeed = pendingThreats + _sessionLaunchesUsed;
            int queueNeed = Mathf.Max(_threatQueue.Count, remainingNeed);
            _sessionLaunchBudget = ammo <= 0 || queueNeed <= 0
                ? 0
                : Mathf.Min(queueNeed, ammo);
        }

        private int CountUnengagedThreats()
        {
            int count = 0;
            for (int i = 0; i < _threatQueue.Count; i++)
            {
                Missile threat = _threatQueue[i];
                if (!NocsGuard.IsValidMissile(threat))
                    continue;

                if (!ThreatEngagementLedger.WasEngaged(threat.persistentID))
                    count++;
            }

            return count;
        }

        private bool ContainsThreatInQueue(Missile candidate)
        {
            for (int i = 0; i < _threatQueue.Count; i++)
            {
                if (_threatQueue[i] == candidate)
                    return true;
            }

            return false;
        }

        private void SortQueueByImpact(Aircraft aircraft)
        {
            SortQueueByImpact(aircraft, _threatQueue, _impactScratch);
        }

        private static void SortQueueByImpact(
            Aircraft aircraft,
            List<Missile> queue,
            float[] impactScratch)
        {
            int count = queue.Count;
            if (count <= 1 || aircraft.rb == null)
                return;

            Vector3 acPos = aircraft.transform.position;
            for (int i = 0; i < count; i++)
            {
                Missile m = queue[i];
                impactScratch[i] = EstimateTimeToImpact(m, aircraft, acPos);
            }

            for (int i = 1; i < count; i++)
            {
                Missile keyMissile = queue[i];
                float keyImpact = impactScratch[i];
                int j = i - 1;
                while (j >= 0 && impactScratch[j] > keyImpact)
                {
                    queue[j + 1] = queue[j];
                    impactScratch[j + 1] = impactScratch[j];
                    j--;
                }

                queue[j + 1] = keyMissile;
                impactScratch[j + 1] = keyImpact;
            }
        }

        private static float EstimateTimeToImpact(Missile missile, Aircraft aircraft, Vector3 acPos)
        {
            return ThreatImpactTime.EstimateOrMax(missile, aircraft, acPos);
        }

        private static bool TryLaunch(Aircraft aircraft, WeaponStationEntry entry, Missile threat)
        {
            if (!NocsGuard.CanMutateLocalWeapons(aircraft))
                return false;

            WeaponManager? wm = aircraft.weaponManager;
            WeaponStation station = entry.Station;
            if (wm == null || station == null)
                return false;

            if (station.Ammo <= 0 || !station.Ready())
                return false;

            if (!NocsGuard.IsValidMissile(threat))
                return false;

            wm.SetActiveStation(entry.Index);
            wm.ClearTargetList();
            wm.AddTargetList(threat);

            GlobalPosition aim;
            if (entry.IsRadar)
            {
                aircraft.NetworkHQ?.CmdUpdateTrackingInfo(threat.persistentID);
                wm.TargetListChanged();
                aim = threat.GlobalPosition();
            }
            else
            {
                aim = ResolveIrLeadAim(aircraft, threat, station);
            }

            HardKillController.NotifySalvoLaunchCommitted();
            station.LaunchMount(aircraft, threat, aim);
            WeaponStationCatalog.InvalidateFrameCache();
            return true;
        }

        private static GlobalPosition ResolveIrLeadAim(
            Aircraft aircraft,
            Missile threat,
            WeaponStation station)
        {
            MsnParams msn = SeekerParamCache.GetMsnParams(station);
            Vector3 threatVel = threat.rb != null ? threat.rb.velocity : Vector3.zero;
            Vector3 ownVel = aircraft.rb != null ? aircraft.rb.velocity : Vector3.zero;
            Vector3 lead = TargetCalc.GetLeadVector(
                threat.GlobalPosition(),
                aircraft.GlobalPosition(),
                threatVel,
                ownVel,
                msn.MaxLeadSec);
            return threat.GlobalPosition() + lead;
        }
    }
}
