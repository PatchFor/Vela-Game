using UnityEngine;
using Vela.Core;

namespace Vela.Player
{
    /// Builds PlayerCommands from VelaInput once per frame, before any gameplay script runs.
    /// Swap this component for a network / AI / test driver later; nothing else changes.
    [DefaultExecutionOrder(-60)]
    public class PlayerInputReader : MonoBehaviour
    {
        private PlayerCommands current;
        private bool injected;

        public PlayerCommands Current => current;

        /// The reader on `go`, added on the fly for scenes built before it existed.
        public static PlayerInputReader For(GameObject go)
        {
            var reader = go.GetComponent<PlayerInputReader>();
            return reader != null ? reader : go.AddComponent<PlayerInputReader>();
        }

        /// Tests / bots: supply commands for the next frame instead of reading the devices.
        public void Inject(PlayerCommands commands)
        {
            current = commands;
            injected = true;
        }

        /// A click was used (item pickup) — combat must not also see it.
        public void ConsumeLeftClick()
        {
            current.LeftDown = false;
            VelaInput.ConsumeClick(VelaInput.MouseButton.Left);
        }

        private void Update()
        {
            if (injected)
            {
                injected = false;
                return;
            }

            var skill = -1;
            for (var i = 0; i < 4; i++)
            {
                if (!VelaInput.SkillPressed(i)) continue;
                skill = i;
                break;
            }

            current = new PlayerCommands
            {
                Move = VelaInput.Move,
                Dash = VelaInput.DashPressed,
                AttackKey = VelaInput.AttackKeyPressed,
                AttackKeyHeld = VelaInput.AttackKeyHeld,
                ChargeKeyHeld = VelaInput.ChargeKeyHeld,
                SkillKey = skill,
                LeftDown = VelaInput.MouseDown(VelaInput.MouseButton.Left),
                LeftHeld = VelaInput.MouseHeld(VelaInput.MouseButton.Left),
                RightDown = VelaInput.MouseDown(VelaInput.MouseButton.Right),
                RightHeld = VelaInput.MouseHeld(VelaInput.MouseButton.Right),
                AimPoint = GroundPoint(),
                LockOn = VelaInput.LockOnPressed,
                NextTarget = VelaInput.NextTargetPressed,
                PickUp = VelaInput.PickUpPressed,
                CycleWeapon = VelaInput.CycleWeaponPressed
            };
        }

        private Vector3 GroundPoint()
        {
            var cam = Camera.main;
            if (cam == null) return transform.position + transform.forward;

            var ray = cam.ScreenPointToRay(VelaInput.MousePosition);
            var ground = new Plane(Vector3.up, transform.position);
            return ground.Raycast(ray, out var enter) ? ray.GetPoint(enter) : transform.position;
        }
    }
}
