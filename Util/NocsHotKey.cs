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

            return Input.GetKeyDown(key);
        }
    }
}
