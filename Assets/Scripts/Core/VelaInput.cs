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

        /// Move 1 — quick slash. Left mouse or J.
        public static bool Attack1Pressed
        {
            get { return KeyPressed(KeyCode.J) || MousePressed(0); }
        }

        /// Move 2 — lunging thrust. Right mouse or K.
        public static bool Attack2Pressed
        {
            get { return KeyPressed(KeyCode.K) || MousePressed(1); }
        }

        /// Move 3 — heavy spin. Middle mouse or L.
        public static bool Attack3Pressed
        {
            get { return KeyPressed(KeyCode.L) || MousePressed(2); }
        }

        public static bool DodgePressed
        {
            get { return KeyPressed(KeyCode.Space) || KeyPressed(KeyCode.LeftShift); }
        }

        public static bool PickupPressed
        {
            get { return KeyPressed(KeyCode.F); }
        }

        public static bool RestartPressed
        {
            get { return KeyPressed(KeyCode.R); }
        }

        public static bool HelpPressed
        {
            get { return KeyPressed(KeyCode.H); }
        }

        /// -1 when the camera should swing one step left, +1 one step right, 0 otherwise.
        public static float CameraRotateStep
        {
            get
            {
                if (KeyPressed(KeyCode.Q)) return -1f;
                if (KeyPressed(KeyCode.E)) return 1f;
                return 0f;
            }
        }

        private static bool KeyPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            switch (key)
            {
                case KeyCode.J: return keyboard.jKey.wasPressedThisFrame;
                case KeyCode.K: return keyboard.kKey.wasPressedThisFrame;
                case KeyCode.L: return keyboard.lKey.wasPressedThisFrame;
                case KeyCode.F: return keyboard.fKey.wasPressedThisFrame;
                case KeyCode.R: return keyboard.rKey.wasPressedThisFrame;
                case KeyCode.H: return keyboard.hKey.wasPressedThisFrame;
                case KeyCode.Q: return keyboard.qKey.wasPressedThisFrame;
                case KeyCode.E: return keyboard.eKey.wasPressedThisFrame;
                case KeyCode.Space: return keyboard.spaceKey.wasPressedThisFrame;
                case KeyCode.LeftShift: return keyboard.leftShiftKey.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown(key);
#endif
        }

        private static bool MousePressed(int button)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = Mouse.current;
            if (mouse == null) return false;

            switch (button)
            {
                case 0: return mouse.leftButton.wasPressedThisFrame;
                case 1: return mouse.rightButton.wasPressedThisFrame;
                case 2: return mouse.middleButton.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetMouseButtonDown(button);
#endif
        }
    }
}
