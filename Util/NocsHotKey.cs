using UnityEngine;

namespace NOCS.Util
{
    internal static class NocsHotKey
    {
        internal static bool IsHeld(KeyCode modifier, KeyCode key)
        {
            if (key == KeyCode.None)
                return false;

            if (modifier != KeyCode.None && !Input.GetKey(modifier))
                return false;

            return Input.GetKey(key);
        }

        internal static bool WasPressed(KeyCode modifier, KeyCode key)
        {
            if (key == KeyCode.None)
                return false;

            if (modifier != KeyCode.None && !Input.GetKey(modifier))
                return false;

            // Chat-only guard: bare key without modifier (e.g. "/" in chat).
            // Never block when a modifier is held — GetKeyDown and inputString share the same frame for "/".
            if (modifier == KeyCode.None && !string.IsNullOrEmpty(Input.inputString))
                return false;

            return Input.GetKeyDown(key);
        }
    }
}
