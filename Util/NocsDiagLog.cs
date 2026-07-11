using UnityEngine;

namespace NOCS.Util
{
    internal static class NocsDiagLog
    {
        internal static void Info(string message)
        {
            if (NocsPlugin.ModLogger != null)
                NocsPlugin.ModLogger.LogInfo(message);
            else
                Debug.Log("[NOCS] " + message);
        }

        internal static void Warn(string message)
        {
            if (NocsPlugin.ModLogger != null)
                NocsPlugin.ModLogger.LogWarning(message);
            else
                Debug.LogWarning("[NOCS] " + message);
        }
    }
}
