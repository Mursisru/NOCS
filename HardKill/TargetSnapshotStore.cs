using System;
using System.Collections.Generic;
using Mirage.Serialization;
using NOCS.Config;
using NOCS.Core;
using NOCS.Util;

namespace NOCS.HardKill
{
    internal static class TargetSnapshotStore
    {
        internal static TargetSnapshot Capture(Aircraft aircraft)
        {
            try
            {
                return CaptureCore(aircraft);
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("TargetSnapshotStore.Capture", ex);
                return TargetSnapshot.Empty;
            }
        }

        private static TargetSnapshot CaptureCore(Aircraft aircraft)
        {
            TargetSnapshot snapshot = TargetSnapshot.Empty;
            if (!NocsGuard.CanMutateLocalWeapons(aircraft))
                return snapshot;

            if (!NocsGuard.IsValidUnit(aircraft) || aircraft!.weaponManager == null)
                return snapshot;

            List<Unit> targets = aircraft.weaponManager.GetTargetList();
            if (targets == null)
                return snapshot;

            int count = targets.Count;
            if (count > TargetSnapshot.MaxTargets)
                count = TargetSnapshot.MaxTargets;

            for (int i = 0; i < count; i++)
            {
                Unit? unit = targets[i];
                if (!NocsGuard.IsValidUnit(unit))
                    continue;

                snapshot.TargetIds[snapshot.TargetCount++] = unit!.persistentID;
            }

            snapshot.TrackMode = snapshot.TargetCount > 1 ? TrackMode.TWS : TrackMode.STT;
            WeaponStation? station = aircraft.weaponManager.currentWeaponStation;
            snapshot.ActiveStationIndex = station != null ? station.Number : (byte)0;

            if (snapshot.TargetCount > 0 && aircraft.NetworkHQ != null)
            {
                Unit? primary = ResolveUnit(snapshot.TargetIds[0]);
                snapshot.PrimaryHadAccurateTrack = primary != null
                    && aircraft.NetworkHQ.IsTargetPositionAccurate(primary, 20f);
            }

            snapshot.Captured = snapshot.TargetCount > 0 || station != null;
            return snapshot;
        }

        private static Unit? ResolveUnit(PersistentID id)
        {
            return NocsUnitLookup.TryGetLiveUnit(id, out Unit unit) ? unit : null;
        }
    }
}
