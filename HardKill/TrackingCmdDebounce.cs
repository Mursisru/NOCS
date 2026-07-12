using UnityEngine;

namespace NOCS.HardKill
{
    /// <summary>
    /// Debounces FactionHQ.CmdUpdateTrackingInfo to at most once per threat ID per frame.
    /// </summary>
    internal static class TrackingCmdDebounce
    {
        private const int MaxTracked = 8;

        private static readonly PersistentID[] SentIds = new PersistentID[MaxTracked];
        private static int _sentCount;
        private static int _frame = -1;

        internal static void TrySend(Aircraft aircraft, PersistentID threatId)
        {
            if (aircraft == null || !threatId.IsValid)
                return;

            FactionHQ? hq = aircraft.NetworkHQ;
            if (hq == null)
                return;

            int frame = Time.frameCount;
            if (_frame != frame)
            {
                _frame = frame;
                _sentCount = 0;
            }

            for (int i = 0; i < _sentCount; i++)
            {
                if (SentIds[i] == threatId)
                    return;
            }

            if (_sentCount < MaxTracked)
                SentIds[_sentCount++] = threatId;

            hq.CmdUpdateTrackingInfo(threatId);
        }

        internal static void Reset()
        {
            _sentCount = 0;
            _frame = -1;
        }
    }
}
