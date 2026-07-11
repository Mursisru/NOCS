using Mirage.Serialization;

namespace NOCS.Core
{
    internal static class NocsUnitLookup
    {
        internal static bool TryGetUnit(PersistentID id, out Unit unit)
        {
            unit = null!;
            if (!id.IsValid)
                return false;

            if (!UnitRegistry.TryGetUnit(new PersistentID?(id), out Unit resolved) || resolved == null)
                return false;

            unit = resolved;
            return true;
        }

        internal static bool TryGetLiveUnit(PersistentID id, out Unit unit)
        {
            if (!TryGetUnit(id, out unit))
                return false;

            if (unit.disabled)
            {
                unit = null!;
                return false;
            }

            return true;
        }
    }
}
