using NOCS.Core;

namespace NOCS.HardKill
{
    internal static class ThreatEngagementLedger
    {
        private const int MaxEntries = 8;

        private static readonly PersistentID[] EngagedIds = new PersistentID[MaxEntries];
        private static int _count;

        internal static void Reset()
        {
            _count = 0;
        }

        internal static void PruneInvalid()
        {
            int write = 0;
            for (int read = 0; read < _count; read++)
            {
                PersistentID id = EngagedIds[read];
                if (!id.IsValid)
                    continue;

                if (!NocsUnitLookup.TryGetLiveUnit(id, out Unit unit))
                    continue;

                EngagedIds[write++] = id;
            }

            _count = write;
        }

        internal static bool WasEngaged(PersistentID threatId)
        {
            if (!threatId.IsValid)
                return false;

            for (int i = 0; i < _count; i++)
            {
                if (EngagedIds[i] == threatId)
                    return true;
            }

            return false;
        }

        internal static void MarkEngaged(PersistentID threatId)
        {
            if (!threatId.IsValid || WasEngaged(threatId))
                return;

            if (_count >= MaxEntries)
                return;

            EngagedIds[_count++] = threatId;
        }
    }
}
