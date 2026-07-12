namespace NOCS.HardKill
{
    internal sealed class HardKillSession
    {
        internal const float PendingLaunchTimeoutSec = 2f;

        internal bool Active;
        internal bool RestorePending;
        internal TargetSnapshot SavedTargets;
        internal int PendingOwnLaunches;
        internal bool SnapshotCaptured;
        internal float PendingLaunchStamp = -1000f;

        internal void Reset()
        {
            Active = false;
            RestorePending = false;
            SavedTargets = TargetSnapshot.Empty;
            PendingOwnLaunches = 0;
            SnapshotCaptured = false;
            PendingLaunchStamp = -1000f;
        }
    }
}
