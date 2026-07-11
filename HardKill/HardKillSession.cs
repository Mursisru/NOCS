namespace NOCS.HardKill
{
    internal sealed class HardKillSession
    {
        internal bool Active;
        internal bool RestorePending;
        internal TargetSnapshot SavedTargets;
        internal int PendingOwnLaunches;
        internal bool SnapshotCaptured;

        internal void Reset()
        {
            Active = false;
            RestorePending = false;
            SavedTargets = TargetSnapshot.Empty;
            PendingOwnLaunches = 0;
            SnapshotCaptured = false;
        }
    }
}
