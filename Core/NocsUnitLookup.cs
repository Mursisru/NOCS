using System;
using Mirage.Serialization;
using NOCS.Util;

namespace NOCS.Core
{
    internal static class NocsUnitLookup
    {
        internal static bool TryGetUnit(PersistentID id, out Unit unit)
        {
            unit = null!;
            if (!id.IsValid)
                return false;

            try
            {
                if (!UnitRegistry.TryGetUnit(new PersistentID?(id), out Unit resolved) || resolved == null)
                    return false;

                unit = resolved;
                return true;
            }
            catch (Exception ex)
            {
                NocsDiagLog.ExceptionOnce("NocsUnitLookup.TryGetUnit", ex);
                return false;
            }
        }

        internal static bool TryGetLiveUnit(PersistentID id, out Unit unit)
        {
            if (!TryGetUnit(id, out unit))
                return false;

            try
            {
                if (unit.disabled)
                {
                    unit = null!;
                    return false;
                }

                return true;
            }
            catch
            {
                unit = null!;
                return false;
            }
        }
    }
}
