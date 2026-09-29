using UnityEngine;

namespace Vela.Player
{
    /// Everything the player asked for this frame, as plain data.
    /// Gameplay (movement, combat, pickup, targeting) reads ONLY this struct, never raw input.
    /// Why: this is the unit you send to a server / predict / replay when the game goes online,
    /// and what PlayMode tests inject to drive the player without a keyboard.
    public struct PlayerCommands
    {
        public Vector2 Move;
        public bool Dash;

        public bool AttackKey;
        public bool AttackKeyHeld;
        public bool ChargeKeyHeld;
        public int SkillKey; // -1 = none, 0..3

        public bool LeftDown;
        public bool LeftHeld;
        public bool RightDown;
        public bool RightHeld;

        /// Where the mouse points on the ground (world space).
        public Vector3 AimPoint;

        public bool LockOn;
        public bool NextTarget;
        public bool PickUp;
        public bool CycleWeapon;
    }
}
