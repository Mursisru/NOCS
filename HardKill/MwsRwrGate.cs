using System.Collections.Generic;
using System.Reflection;
using NOCS.Config;
using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    /// <summary>
    /// HardKill may only react to missiles listed on the local aircraft MissileWarning (RWR).
    /// No world scan, no salvo-only union outside RWR lists.
    /// </summary>
    internal static class MwsRwrGate
    {
        private static FieldInfo? _unknownMissilesField;

        internal static bool HasRwrPicture(Aircraft aircraft)
        {
            if (!NocsGuard.IsValidUnit(aircraft))
                return false;

            MissileWarning? warning = aircraft!.GetMissileWarningSystem();
            if (warning?.knownMissiles == null)
                return false;

            return CountIncomingListed(aircraft, warning, aircraft.persistentID) > 0;
        }

        internal static bool IsRwrListed(MissileWarning warning, Missile missile)
        {
            if (!NocsGuard.IsValidMissile(missile))
                return false;

            PersistentID id = missile.persistentID;
            if (IsListedById(warning.knownMissiles, id))
                return true;

            if (!TryGetUnknownMissiles(warning, out List<Missile>? unknown) || unknown == null)
                return false;

            return IsListedById(unknown, id);
        }

        internal static bool IsEngageableRwrThreat(
            Aircraft aircraft,
            MissileWarning warning,
            Missile missile,
            PersistentID selfId,
            FactionHQ? playerHq)
        {
            if (!IsSalvoThreatOnSelf(missile, selfId, playerHq))
                return false;

            if (!IsRwrListed(warning, missile))
                return false;

            return IncomingThreatPolicy.IsEngageableIncoming(aircraft, missile);
        }

        internal static void AppendRwrScanList(MissileWarning warning, List<Missile> destination)
        {
            destination.Clear();
            AppendUniqueListed(warning.knownMissiles, destination);

            if (TryGetUnknownMissiles(warning, out List<Missile>? unknown) && unknown != null)
                AppendUniqueListed(unknown, destination);
        }

        private static int CountIncomingListed(Aircraft aircraft, MissileWarning warning, PersistentID selfId)
        {
            int count = 0;
            count += CountIncomingInList(aircraft, warning.knownMissiles, selfId);
            if (TryGetUnknownMissiles(warning, out List<Missile>? unknown) && unknown != null)
                count += CountIncomingInList(aircraft, unknown, selfId);
            return count;
        }

        private static int CountIncomingInList(Aircraft aircraft, List<Missile> source, PersistentID selfId)
        {
            int count = 0;
            for (int i = 0; i < source.Count; i++)
            {
                Missile? missile = source[i];
                if (!NocsGuard.IsValidMissile(missile))
                    continue;

                if (missile!.targetID != selfId)
                    continue;

                if (!IncomingThreatPolicy.IsEngageableIncoming(aircraft, missile))
                    continue;

                count++;
            }

            return count;
        }

        private static void AppendUniqueListed(List<Missile> source, List<Missile> destination)
        {
            for (int i = 0; i < source.Count; i++)
            {
                Missile? missile = source[i];
                if (missile == null)
                    continue;

                for (int j = 0; j < destination.Count; j++)
                {
                    if (destination[j] == missile)
                        goto next;
                }

                destination.Add(missile);
                next: ;
            }
        }

        private static bool IsListedById(List<Missile> source, PersistentID id)
        {
            if (!id.IsValid)
                return false;

            for (int i = 0; i < source.Count; i++)
            {
                Missile? missile = source[i];
                if (missile == null)
                    continue;

                if (missile.persistentID == id)
                    return true;
            }

            return false;
        }

        private static bool TryGetUnknownMissiles(MissileWarning warning, out List<Missile>? unknown)
        {
            unknown = null;
            _unknownMissilesField ??= typeof(MissileWarning).GetField(
                "unknownMissiles",
                BindingFlags.Instance | BindingFlags.NonPublic);

            unknown = _unknownMissilesField?.GetValue(warning) as List<Missile>;
            return unknown != null;
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
    }
}
