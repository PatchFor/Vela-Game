using System.Collections.Generic;
using UnityEngine;
using Vela.Combat;
using Vela.Core;
using Vela.Enemies;
using Vela.FX;
using Vela.Gameplay;
using Vela.Visual;

namespace Vela.Player
{
    /// Lock-on and pointer targeting.
    ///  - Hover a monster with the mouse → red pixel outline (click it to lock it).
    ///  - Q (or middle mouse): lock the hovered monster, else the best one in front; again to release.
    ///  - E: switch to the next monster in range.
    ///  - While locked, attacks and facing go toward the target. The lock breaks when the target
    ///    dies or gets too far.
    /// Runs before PlayerCombat so a click that locks a target also attacks it.
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerTargeting : MonoBehaviour
    {
        private static readonly Color LockColor = new Color(1f, 0.45f, 0.25f, 0.95f);

        private PlayerController player;
        private PlayerInputReader input;
        private TargetReticle lockReticle;
        private SpriteBillboard outlined;
        private readonly List<EnemyBrain> candidates = new List<EnemyBrain>();

        public EnemyBrain Target { get; private set; }
        public EnemyBrain Hovered { get; private set; }

        public bool HasTarget => Target != null;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            input = PlayerInputReader.For(gameObject);
        }

        private void Start()
        {
            lockReticle = TargetReticle.Create("[LockReticle]");
        }

        private void OnDestroy()
        {
            if (lockReticle != null) Destroy(lockReticle.gameObject);
            SetOutlined(null);
        }

        public void Lock(EnemyBrain enemy)
        {
            if (enemy == null || !enemy.IsAlive) return;
            if (Target != enemy) FxManager.Ring(enemy.transform.position, 0.2f, Radius(enemy) * 1.6f, 0.2f, LockColor);
            Target = enemy;
        }

        public void Release() => Target = null;

        /// Flat direction from the player to the locked target, or null.
        public Vector3? DirectionToTarget
        {
            get
            {
                if (Target == null) return null;
                var d = CombatUtility.Flat(Target.transform.position - transform.position);
                return d.sqrMagnitude > 0.0001f ? d.normalized : (Vector3?)null;
            }
        }

        private void Update()
        {
            var config = player.Config;

            if (!player.IsAlive)
            {
                Release();
                Hovered = null;
                UpdateReticles();
                return;
            }

            Hovered = VelaInput.PointerOverUI ? null : PickUnderPointer(config.hoverRadiusPixels);

            if (Target != null)
            {
                var far = CombatUtility.Flat(Target.transform.position - transform.position).magnitude > config.lockOnBreakRange;
                if (!Target.IsAlive || far) Release();
            }

            var commands = input.Current;
            if (commands.LockOn)
            {
                if (Target != null && (Hovered == null || Hovered == Target)) Release();
                else Lock(Hovered != null ? Hovered : BestCandidate(config.lockOnRange));
            }

            if (commands.NextTarget) Cycle(config.lockOnRange);

            // Clicking a monster locks it; the click still goes on to attack.
            if (Hovered != null && commands.LeftDown) Lock(Hovered);

            UpdateReticles();
        }

        private void UpdateReticles()
        {
            if (lockReticle == null) return;

            if (Target != null) lockReticle.Show(Target.transform, Radius(Target), LockColor);
            else lockReticle.Hide();

            SetOutlined(Hovered != null && Hovered.IsAlive ? Hovered.GetComponent<SpriteBillboard>() : null);
        }

        private void SetOutlined(SpriteBillboard next)
        {
            var config = player != null ? player.Config : null;
            if (outlined != null && outlined != next) outlined.SetOutline(false, Color.clear);
            outlined = next;
            if (outlined != null && config != null) outlined.SetOutline(true, config.hoverOutlineColor, config.hoverOutlineWidth);
        }

        private static float Radius(EnemyBrain enemy) =>
            enemy.Config != null ? enemy.Config.colliderRadius + 0.45f : 0.9f;

        private EnemyBrain PickUnderPointer(float radiusPixels)
        {
            var cam = Camera.main;
            if (cam == null) return null;

            var mouse = VelaInput.MousePosition;
            var scale = Screen.height / 1080f;
            EnemyBrain best = null;
            var bestDistance = float.MaxValue;

            foreach (var enemy in CombatRegistry.Enemies)
            {
                if (enemy == null || !enemy.IsAlive || enemy.Config == null) continue;

                var visual = enemy.Config.visual;
                var center = enemy.transform.position + Vector3.up * (visual.worldHeight * 0.5f + visual.hoverHeight);
                var screen = cam.WorldToScreenPoint(center);
                if (screen.z < 0f) continue;

                var size = Mathf.Clamp(visual.worldHeight / 1.8f, 0.7f, 2.2f);
                var distance = Vector2.Distance(mouse, screen);
                if (distance > radiusPixels * scale * size || distance >= bestDistance) continue;

                bestDistance = distance;
                best = enemy;
            }

            return best;
        }

        private EnemyBrain BestCandidate(float range)
        {
            EnemyBrain best = null;
            var bestScore = float.MaxValue;
            var forward = player.Facing;

            foreach (var enemy in CombatRegistry.Enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                var offset = CombatUtility.Flat(enemy.transform.position - transform.position);
                var distance = offset.magnitude;
                if (distance > range) continue;

                // Prefer what's in front, then what's close.
                var score = Vector3.Angle(forward, offset) * 0.08f + distance;
                if (score >= bestScore) continue;
                bestScore = score;
                best = enemy;
            }

            return best;
        }

        private void Cycle(float range)
        {
            candidates.Clear();
            foreach (var enemy in CombatRegistry.Enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (CombatUtility.Flat(enemy.transform.position - transform.position).magnitude <= range) candidates.Add(enemy);
            }
            if (candidates.Count == 0) return;

            // Order clockwise around the player (screen-consistent with a locked camera).
            candidates.Sort((a, b) => Bearing(a).CompareTo(Bearing(b)));
            var index = Target != null ? candidates.IndexOf(Target) : -1;
            Lock(candidates[(index + 1) % candidates.Count]);
        }

        private float Bearing(EnemyBrain enemy)
        {
            var d = CombatUtility.Flat(enemy.transform.position - transform.position);
            return Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f);
        }
    }
}
