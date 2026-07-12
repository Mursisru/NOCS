using System;
using UnityEngine;

namespace NOCS.Util
{
    internal static class NocsDiagLog
    {
        private static string? _lastExceptionKey;
        private static float _lastExceptionTime = -1000f;
        private const float ExceptionLogCooldownSec = 5f;

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

        internal static void ExceptionOnce(string context, Exception? exception)
        {
            if (exception == null)
                return;

            string key = context + ":" + exception.GetType().FullName;
            float now = Time.realtimeSinceStartup;
            if (key == _lastExceptionKey && now - _lastExceptionTime < ExceptionLogCooldownSec)
                return;

            _lastExceptionKey = key;
            _lastExceptionTime = now;

            string message = "[NOCS] Harmony/" + context + ": " + exception.Message;
            if (NocsPlugin.ModLogger != null)
                NocsPlugin.ModLogger.LogError(message);
            else
                Debug.LogError(message);
        }
    }
}
