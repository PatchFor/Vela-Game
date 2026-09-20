using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Vela.Core
{
    // Compiles against whichever input backend the project is set to, so the
    // prototype keeps working if the Input System package is added later.
    public static class VelaInput
    {
        public static Vector2 Move
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var keyboard = Keyboard.current;
                if (keyboard == null) return Vector2.zero;

                var move = Vector2.zero;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
                return Vector2.ClampMagnitude(move, 1f);
#else
                var move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                return Vector2.ClampMagnitude(move, 1f);
#endif
            }
        }

        public static bool DashPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var keyboard = Keyboard.current;
                return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Space);
#endif
            }
        }

        public static bool RestartPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var keyboard = Keyboard.current;
                return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.R);
#endif
            }
        }

        /// -1 when the camera should swing one step left, +1 one step right, 0 otherwise.
        public static float CameraRotateStep
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var keyboard = Keyboard.current;
                if (keyboard == null) return 0f;
                if (keyboard.qKey.wasPressedThisFrame) return -1f;
                if (keyboard.eKey.wasPressedThisFrame) return 1f;
                return 0f;
#else
                if (Input.GetKeyDown(KeyCode.Q)) return -1f;
                if (Input.GetKeyDown(KeyCode.E)) return 1f;
                return 0f;
#endif
            }
        }
    }
}
