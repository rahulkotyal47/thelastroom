using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Works with the old Input Manager, the new Input System, or both.
public static class GameInput
{
    public static Vector2 Move
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = ((k.dKey.isPressed || k.rightArrowKey.isPressed) ? 1f : 0f) - ((k.aKey.isPressed || k.leftArrowKey.isPressed) ? 1f : 0f);
            float y = ((k.wKey.isPressed || k.upArrowKey.isPressed) ? 1f : 0f) - ((k.sKey.isPressed || k.downArrowKey.isPressed) ? 1f : 0f);
            return new Vector2(x, y);
#elif ENABLE_LEGACY_INPUT_MANAGER
            float x = ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1f : 0f) - ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? 1f : 0f);
            float y = ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1f : 0f) - ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ? 1f : 0f);
            return new Vector2(x, y);
#else
            return Vector2.zero;
#endif
        }
    }

    public static Vector2 LookDelta
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m == null) return Vector2.zero;
            return m.delta.ReadValue() * 0.1f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
            return Vector2.zero;
#endif
        }
    }

    public static bool Sprint
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift);
#else
            return false;
#endif
        }
    }

    public static bool InteractPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }
    }

    public static bool FlashlightPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F);
#else
            return false;
#endif
        }
    }

    public static bool PausePressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
    }
}
