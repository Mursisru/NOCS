using Mirage.Serialization;

namespace NOCS.HardKill
{
    internal struct TargetSnapshot
    {
        internal const int MaxTargets = 16;

        internal PersistentID[] TargetIds;
        internal int TargetCount;
        internal Config.TrackMode TrackMode;
        internal byte ActiveStationIndex;
        internal bool PrimaryHadAccurateTrack;
        internal bool Captured;

        internal static TargetSnapshot Empty => new TargetSnapshot
        {
            TargetIds = new PersistentID[MaxTargets],
        };
    }
}
