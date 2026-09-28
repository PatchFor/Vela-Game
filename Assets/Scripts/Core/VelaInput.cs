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
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private static bool Down(Key key)
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }

        private static bool Held(Key key)
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].isPressed;
        }
#endif

        public static Vector2 Move
        {
            get
            {
                var move = Vector2.zero;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                if (Held(Key.W) || Held(Key.UpArrow)) move.y += 1f;
                if (Held(Key.S) || Held(Key.DownArrow)) move.y -= 1f;
                if (Held(Key.D) || Held(Key.RightArrow)) move.x += 1f;
                if (Held(Key.A) || Held(Key.LeftArrow)) move.x -= 1f;
#else
                move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
                return Vector2.ClampMagnitude(move, 1f);
            }
        }

        public static Vector2 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var mouse = Mouse.current;
                return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        /// Scroll wheel notches this frame: positive = away from the player (zoom in).
        public static float Zoom
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var mouse = Mouse.current;
                var scroll = mouse != null ? mouse.scroll.ReadValue().y / 120f : 0f;
                if (Held(Key.Equals) || Held(Key.NumpadPlus)) scroll += 4f * Time.unscaledDeltaTime;
                if (Held(Key.Minus) || Held(Key.NumpadMinus)) scroll -= 4f * Time.unscaledDeltaTime;
                return scroll;
#else
                var scroll = Input.mouseScrollDelta.y;
                if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) scroll += 4f * Time.unscaledDeltaTime;
                if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) scroll -= 4f * Time.unscaledDeltaTime;
                return scroll;
#endif
            }
        }

        public static bool AttackPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var mouse = Mouse.current;
                return (mouse != null && mouse.leftButton.wasPressedThisFrame) || Down(Key.J);
#else
                return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J);
#endif
            }
        }

        public static bool AttackHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var mouse = Mouse.current;
                return (mouse != null && mouse.leftButton.isPressed) || Held(Key.J);
#else
                return Input.GetMouseButton(0) || Input.GetKey(KeyCode.J);
#endif
            }
        }

        /// Charge / heavy attack button (right mouse or K).
        public static bool SpecialHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var mouse = Mouse.current;
                return (mouse != null && mouse.rightButton.isPressed) || Held(Key.K);
#else
                return Input.GetMouseButton(1) || Input.GetKey(KeyCode.K);
#endif
            }
        }

        public static bool DashPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Down(Key.Space) || Down(Key.LeftShift);
#else
                return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift);
#endif
            }
        }

        /// 0-based weapon slot chosen with the number keys this frame, or -1.
        public static int WeaponSlotPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                if (Down(Key.Digit1)) return 0;
                if (Down(Key.Digit2)) return 1;
                if (Down(Key.Digit3)) return 2;
#else
                if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
                if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
                if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
#endif
                return -1;
            }
        }

        public static bool CycleWeaponPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Down(Key.Tab);
#else
                return Input.GetKeyDown(KeyCode.Tab);
#endif
            }
        }

        public static bool RestartPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Down(Key.R);
#else
                return Input.GetKeyDown(KeyCode.R);
#endif
            }
        }

        public static bool RespawnEnemiesPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Down(Key.T);
#else
                return Input.GetKeyDown(KeyCode.T);
#endif
            }
        }

        public static bool GodModePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Down(Key.G);
#else
                return Input.GetKeyDown(KeyCode.G);
#endif
            }
        }

        public static bool TeleportToBossPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Down(Key.B);
#else
                return Input.GetKeyDown(KeyCode.B);
#endif
            }
        }

        public static bool HelpPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Down(Key.F1) || Down(Key.H);
#else
                return Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.H);
#endif
            }
        }
    }
}
