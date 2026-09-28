using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Vela.Core
{
    /// All input in one place. Compiles against whichever input backend the project uses.
    ///
    /// Keyboard plays: WASD move, Space/Shift dash, J attack, K charge, 1–4 skills, Q lock-on,
    /// E next target, F pick up, I inventory, Tab weapon.
    /// Mouse points: click items / monsters; left/right buttons run whatever action is bound.
    public static class VelaInput
    {
        public enum MouseButton
        {
            Left = 0,
            Right = 1,
            Middle = 2
        }

        private static int consumedLeftFrame = -1;
        private static int consumedRightFrame = -1;

        /// Set every frame by UI windows while the pointer is over them: mouse clicks then
        /// belong to the UI, not the game.
        public static bool PointerOverUI { get; set; }

        /// A window (inventory) is open and wants the mouse.
        public static bool UIOpen { get; set; }

        /// Call when a click was used for something (picking an item) so combat ignores it.
        public static void ConsumeClick(MouseButton button)
        {
            if (button == MouseButton.Left) consumedLeftFrame = Time.frameCount;
            else if (button == MouseButton.Right) consumedRightFrame = Time.frameCount;
        }

        private static bool Consumed(MouseButton button) =>
            (button == MouseButton.Left && consumedLeftFrame == Time.frameCount) ||
            (button == MouseButton.Right && consumedRightFrame == Time.frameCount);

        private static bool GameGetsMouse => !PointerOverUI;

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

        private static UnityEngine.InputSystem.Controls.ButtonControl Button(MouseButton b)
        {
            var mouse = Mouse.current;
            if (mouse == null) return null;
            return b == MouseButton.Left ? mouse.leftButton : b == MouseButton.Right ? mouse.rightButton : mouse.middleButton;
        }

        private static bool RawMouseDown(MouseButton b) => Button(b)?.wasPressedThisFrame ?? false;
        private static bool RawMouseHeld(MouseButton b) => Button(b)?.isPressed ?? false;
#else
        private static bool Down(KeyCode key) => Input.GetKeyDown(key);
        private static bool Held(KeyCode key) => Input.GetKey(key);
        private static bool RawMouseDown(MouseButton b) => Input.GetMouseButtonDown((int)b);
        private static bool RawMouseHeld(MouseButton b) => Input.GetMouseButton((int)b);
#endif

        // ------------------------------------------------------------------ mouse

        public static bool MouseDown(MouseButton b) => GameGetsMouse && !Consumed(b) && RawMouseDown(b);
        public static bool MouseHeld(MouseButton b) => GameGetsMouse && RawMouseHeld(b);

        /// Raw state, ignoring UI (for UI code itself).
        public static bool MouseDownRaw(MouseButton b) => RawMouseDown(b);

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

        /// Scroll wheel notches this frame: positive = zoom in.
        public static float Zoom
        {
            get
            {
                if (PointerOverUI) return 0f;
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

        // ------------------------------------------------------------------ keyboard

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
                if (Held(KeyCode.W) || Held(KeyCode.UpArrow)) move.y += 1f;
                if (Held(KeyCode.S) || Held(KeyCode.DownArrow)) move.y -= 1f;
                if (Held(KeyCode.D) || Held(KeyCode.RightArrow)) move.x += 1f;
                if (Held(KeyCode.A) || Held(KeyCode.LeftArrow)) move.x -= 1f;
#endif
                return Vector2.ClampMagnitude(move, 1f);
            }
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        public static bool AttackKeyPressed => Down(Key.J);
        public static bool AttackKeyHeld => Held(Key.J);
        public static bool ChargeKeyHeld => Held(Key.K);
        public static bool DashPressed => Down(Key.Space) || Down(Key.LeftShift);
        public static bool LockOnPressed => Down(Key.Q) || RawMouseDown(MouseButton.Middle);
        public static bool NextTargetPressed => Down(Key.E);
        public static bool PickUpPressed => Down(Key.F);
        public static bool InventoryPressed => Down(Key.I);
        public static bool CycleWeaponPressed => Down(Key.Tab);
        public static bool RestartPressed => Down(Key.R);
        public static bool RespawnEnemiesPressed => Down(Key.T);
        public static bool GodModePressed => Down(Key.G);
        public static bool TeleportToBossPressed => Down(Key.B);
        public static bool HelpPressed => Down(Key.F1) || Down(Key.H);
        public static bool DebugLootPressed => Down(Key.F5);
        public static bool DebugOutfitPressed => Down(Key.O);
        public static bool CancelPressed => Down(Key.Escape);

        public static bool SkillPressed(int index) => index switch
        {
            0 => Down(Key.Digit1),
            1 => Down(Key.Digit2),
            2 => Down(Key.Digit3),
            3 => Down(Key.Digit4),
            _ => false
        };
#else
        public static bool AttackKeyPressed => Down(KeyCode.J);
        public static bool AttackKeyHeld => Held(KeyCode.J);
        public static bool ChargeKeyHeld => Held(KeyCode.K);
        public static bool DashPressed => Down(KeyCode.Space) || Down(KeyCode.LeftShift);
        public static bool LockOnPressed => Down(KeyCode.Q) || RawMouseDown(MouseButton.Middle);
        public static bool NextTargetPressed => Down(KeyCode.E);
        public static bool PickUpPressed => Down(KeyCode.F);
        public static bool InventoryPressed => Down(KeyCode.I);
        public static bool CycleWeaponPressed => Down(KeyCode.Tab);
        public static bool RestartPressed => Down(KeyCode.R);
        public static bool RespawnEnemiesPressed => Down(KeyCode.T);
        public static bool GodModePressed => Down(KeyCode.G);
        public static bool TeleportToBossPressed => Down(KeyCode.B);
        public static bool HelpPressed => Down(KeyCode.F1) || Down(KeyCode.H);
        public static bool DebugLootPressed => Down(KeyCode.F5);
        public static bool DebugOutfitPressed => Down(KeyCode.O);
        public static bool CancelPressed => Down(KeyCode.Escape);

        public static bool SkillPressed(int index) => index switch
        {
            0 => Down(KeyCode.Alpha1),
            1 => Down(KeyCode.Alpha2),
            2 => Down(KeyCode.Alpha3),
            3 => Down(KeyCode.Alpha4),
            _ => false
        };
#endif
    }
}
