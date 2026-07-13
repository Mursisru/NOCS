using System;
using Mirage.Serialization;
using NOCS.Config;
using NOCS.Core;
using NOCS.Util;

namespace NOCS.HardKill
{
    internal static class TargetRestoreValidator
    {
        internal static void Restore(Aircraft aircraft, in TargetSnapshot snapshot)
        {
            if (!NocsGuard.CanMutateLocalWeapons(aircraft))
                return;

            if (!NocsGuard.IsValidUnit(aircraft) || aircraft!.weaponManager == null)
                return;

            if (!snapshot.Captured)
                return;

            try
            {
                RestoreCore(aircraft, in snapshot);
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("TargetRestoreValidator.Restore", ex);
            }
        }

        private static void RestoreCore(Aircraft aircraft, in TargetSnapshot snapshot)
        {
            WeaponManager wm = aircraft.weaponManager!;
            wm.ClearTargetList();

            FactionHQ? hq = aircraft.NetworkHQ;
            int restored = 0;

            for (int i = 0; i < snapshot.TargetCount; i++)
            {
                PersistentID id = snapshot.TargetIds[i];
                if (!id.IsValid)
                    continue;

                if (!NocsUnitLookup.TryGetLiveUnit(id, out Unit unit))
                    continue;

                bool accurate = hq != null && hq.IsTargetPositionAccurate(unit, 20f);
                bool los = false;
                try
                {
                    los = unit.LineOfSight(aircraft.transform.position, 1000f);
                }
                catch
                {
                    continue;
                }

                if (!accurate || !los)
                {
                    TrackingCmdDebounce.TrySend(aircraft, id);
                    continue;
                }

                wm.AddTargetList(unit);
                restored++;
            }

            wm.SetActiveStation(snapshot.ActiveStationIndex);
            wm.TargetListChanged();

            if (snapshot.TrackMode == TrackMode.STT && restored > 1)
            {
                System.Collections.Generic.List<Unit> targets = wm.GetTargetList();
                while (targets != null && targets.Count > 1)
                {
                    Unit last = targets[targets.Count - 1];
                    wm.RemoveTargetList(last);
                    targets = wm.GetTargetList();
                }
            }
        }
    }
}
