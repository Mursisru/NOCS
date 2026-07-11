namespace NOCS.Config
{
    internal enum WeaponPriority
    {
        IR_First,
        ARH_First,
    }

    internal enum WeaponFilterMode
    {
        AntiMissileOnly,
        AnyIrArh,
    }

    internal enum TrackMode : byte
    {
        STT = 0,
        TWS = 1,
    }

    internal enum SeekerKind : byte
    {
        None = 0,
        IR = 1,
        ARH = 2,
        SARH = 3,
        Other = 4,
    }
}
