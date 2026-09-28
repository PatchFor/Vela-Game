using System;
using UnityEngine;
using Vela.CameraRig;
using Vela.Config;
using Vela.Core;
using Vela.FX;
using Vela.Gameplay;
using Vela.Visual;
using Vela.World;

namespace Vela.Player
{
    /// Movement, dash (with i-frames and afterimages), jump links, knockback and hurt-stun,
    /// plus "walk to" for clicking far-away items. Attacks live in PlayerCombat.
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;

        private CharacterController controller;
        private Health health;
        private SpriteBillboard billboard;
        private PaperDoll paperDoll;
        private PlayerCombat combat;
        private PlayerTargeting targeting;

        private Vector3 planarVelocity;
        private Vector3 dashDirection;
        private Vector3 impulseVelocity;
        private float impulseTime;
        private Vector3 knockbackVelocity;
        private float verticalVelocity;
        private float dashTimeRemaining;
        private float dashCooldownRemaining;
        private float afterImageTimer;
        private float stunnedUntil;
        private float lastHurtTime = -10f;
        private Vector3 facing = Vector3.back;

        // Jump link traversal
        private bool jumping;
        private Vector3 jumpFrom;
        private Vector3 jumpTo;
        private float jumpTime;

        // Walk-to (click an item that's out of reach)
        private Vector3? walkTarget;
        private float walkStopDistance;
        private Action walkArrived;

        public event Action Dashed;

        public PlayerConfig Config
        {
            get => config;
            set => config = value;
        }

        public Health Health => health;
        public SpriteBillboard Billboard => billboard;
        public PaperDoll PaperDoll => paperDoll;
        public bool IsDashing => dashTimeRemaining > 0f;
        public bool IsJumping => jumping;
        public bool IsStunned => Time.time < stunnedUntil;
        public bool IsAlive => health != null && health.IsAlive;
        public Vector3 Facing => facing;
        public float DashCooldownNormalized =>
            config != null && config.dashCooldown > 0f ? dashCooldownRemaining / config.dashCooldown : 0f;

        /// Set by PlayerCombat each frame (attacks slow you down).
        public float MoveSpeedMultiplier { get; set; } = 1f;

        /// Where the mouse points on the ground. Updated every frame.
        public Vector3 AimPoint { get; private set; }

        /// Flat direction from the player toward the mouse pointer.
        public Vector3 PointerDirection
        {
            get
            {
                var d = AimPoint - transform.position;
                d.y = 0f;
                return d.sqrMagnitude > 0.01f ? d.normalized : facing;
            }
        }

        private void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<PlayerConfig>();

            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            billboard = GetComponent<SpriteBillboard>();
            paperDoll = GetComponent<PaperDoll>();
            combat = GetComponent<PlayerCombat>();
            targeting = GetComponent<PlayerTargeting>();

            health.Configure(config.maxHealth, Team.Player, config.invulnerabilityAfterHit);
            if (billboard != null) billboard.Setup(config.visual);
            if (paperDoll != null && config.rig != null) paperDoll.Rig = config.rig;

            CombatRegistry.Player = this;
            AimPoint = transform.position + facing;
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
        }

        private void OnDestroy()
        {
            if (CombatRegistry.Player == this) CombatRegistry.Player = null;
        }

        /// Push the player along `velocity` for `duration` (attack lunges).
        public void ApplyImpulse(Vector3 velocity, float duration)
        {
            impulseVelocity = velocity;
            impulseTime = duration;
        }

        public void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            facing = direction.normalized;
            if (billboard != null) billboard.SetFacing(facing);
        }

        public void CancelDash() => dashTimeRemaining = 0f;

        /// Walk toward `target` until within `stopDistance`, then call `arrived`.
        /// Any movement key, dash or attack cancels it.
        public void WalkTo(Vector3 target, float stopDistance, Action arrived)
        {
            walkTarget = target;
            walkStopDistance = stopDistance;
            walkArrived = arrived;
        }

        public void CancelWalk()
        {
            walkTarget = null;
            walkArrived = null;
        }

        private void Update()
        {
            UpdateAim();

            if (!IsAlive)
            {
                if (billboard != null)
                {
                    billboard.SetState(VisualState.Hurt);
                    billboard.SetBaseTint(new Color(0.45f, 0.45f, 0.5f));
                    billboard.SetBlink(false, 0f);
                }
                planarVelocity = Vector3.zero;
                ApplyMotion(Vector3.zero);
                return;
            }

            if (jumping)
            {
                UpdateJump();
                return;
            }

            var dt = Time.deltaTime;
            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - dt);

            var input = VelaInput.Move;
            var moveDir = CameraRelative(input);
            var busy = combat != null && combat.IsBusy;

            // Walk-to: only while there's no other intent.
            if (walkTarget.HasValue)
            {
                if (moveDir.sqrMagnitude > 0.01f || busy) CancelWalk();
                else moveDir = WalkDirection();
            }

            if (VelaInput.DashPressed && CanDash())
            {
                CancelWalk();
                var intended = moveDir.sqrMagnitude > 0.01f ? moveDir.normalized : facing;
                var link = JumpLink.Find(transform.position, intended, config.jumpMaxAngle, out var destination);
                if (link != null) StartJump(destination);
                else StartDash(moveDir);
                if (jumping) return;
            }

            if (IsDashing)
            {
                dashTimeRemaining -= dt;
                planarVelocity = dashDirection * config.dashSpeed;
                afterImageTimer -= dt;
                if (afterImageTimer <= 0f)
                {
                    afterImageTimer = config.afterImageInterval;
                    SpawnAfterImages(config.afterImageColor, config.afterImageLifetime);
                }

                if (dashTimeRemaining <= 0f) planarVelocity = dashDirection * config.moveSpeed;
            }
            else
            {
                var speed = IsStunned ? 0f : config.moveSpeed * MoveSpeedMultiplier;
                var desired = moveDir * speed;
                var rate = desired.sqrMagnitude > 0.01f ? config.acceleration : config.deceleration;
                planarVelocity = Vector3.MoveTowards(planarVelocity, desired, rate * dt);

                // Facing: locked target wins; otherwise face where you walk.
                if (!busy)
                {
                    var toTarget = targeting != null ? targeting.DirectionToTarget : null;
                    if (toTarget.HasValue) Face(toTarget.Value);
                    else if (moveDir.sqrMagnitude > 0.01f) Face(moveDir);
                }
            }

            var extra = knockbackVelocity;
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, 40f * dt);

            if (impulseTime > 0f)
            {
                impulseTime -= dt;
                extra += impulseVelocity;
            }

            ApplyMotion(planarVelocity + extra);
            UpdateVisualState();

            // Blink during post-hit i-frames (not during dash i-frames, which have afterimages).
            if (billboard != null)
            {
                var blink = health.IsInvulnerable && !IsDashing && Time.time - lastHurtTime < config.invulnerabilityAfterHit;
                billboard.SetBlink(blink, VelaSettings.Feel.invulnerableBlinkRate);
            }
        }

        private Vector3 WalkDirection()
        {
            var offset = walkTarget.Value - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= walkStopDistance)
            {
                var arrived = walkArrived;
                CancelWalk();
                arrived?.Invoke();
                return Vector3.zero;
            }
            return offset.normalized;
        }

        private bool CanDash()
        {
            if (dashCooldownRemaining > 0f || IsDashing || IsStunned || jumping) return false;
            return combat == null || combat.CanDashNow;
        }

        private void StartDash(Vector3 moveDir)
        {
            dashDirection = moveDir.sqrMagnitude > 0.01f ? moveDir.normalized : facing;
            dashTimeRemaining = config.dashDuration;
            dashCooldownRemaining = config.dashCooldown;
            afterImageTimer = 0f;
            impulseTime = 0f;
            knockbackVelocity = Vector3.zero;
            Face(dashDirection);

            health.GrantInvulnerability(config.dashDuration + config.dashInvulnerabilityBonus);
            FxManager.Dust(transform.position, 8, new Color(0.85f, 0.85f, 0.8f, 0.8f));
            if (billboard != null) billboard.Punch(new Vector2(1.25f, 0.8f));

            Dashed?.Invoke();
        }

        // ------------------------------------------------------------------ jump links

        private void StartJump(Vector3 destination)
        {
            jumping = true;
            jumpFrom = transform.position;
            jumpTo = destination;
            jumpTime = 0f;
            dashCooldownRemaining = config.dashCooldown;
            planarVelocity = Vector3.zero;
            knockbackVelocity = Vector3.zero;
            impulseTime = 0f;
            controller.enabled = false;

            Face(jumpTo - jumpFrom);
            health.GrantInvulnerability(config.jumpDuration + 0.1f);
            FxManager.Dust(transform.position, 10, new Color(0.85f, 0.85f, 0.8f, 0.8f));
            if (billboard != null)
            {
                billboard.Punch(new Vector2(0.75f, 1.3f));
                billboard.SetState(VisualState.Move);
            }

            Dashed?.Invoke();
        }

        private void UpdateJump()
        {
            jumpTime += Time.deltaTime;
            var t = Mathf.Clamp01(jumpTime / Mathf.Max(0.05f, config.jumpDuration));
            // Ease-in-out along the ground, parabola for height (shadow stays on the floor).
            var along = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(jumpFrom, jumpTo, along);
            if (billboard != null) billboard.SetAirHeight(4f * config.jumpArcHeight * t * (1f - t));

            afterImageTimer -= Time.deltaTime;
            if (afterImageTimer <= 0f)
            {
                afterImageTimer = config.afterImageInterval * 2f;
                SpawnAfterImages(config.afterImageColor * new Color(1f, 1f, 1f, 0.5f), config.afterImageLifetime);
            }

            if (t < 1f) return;

            jumping = false;
            controller.enabled = true;
            verticalVelocity = -2f;
            if (billboard != null)
            {
                billboard.SetAirHeight(0f);
                billboard.Punch(new Vector2(1.35f, 0.7f));
            }
            FxManager.Dust(transform.position, 14, new Color(0.85f, 0.85f, 0.8f, 0.85f));
            FxManager.Ring(transform.position, 0.2f, 1.3f, 0.2f, new Color(1f, 1f, 1f, 0.6f));
            CameraShake.Add(0.1f);
        }

        // ------------------------------------------------------------------ helpers

        private void SpawnAfterImages(Color color, float lifetime)
        {
            if (paperDoll != null && paperDoll.VisibleLayers.Count > 0)
            {
                foreach (var layer in paperDoll.VisibleLayers) FxManager.AfterImageOf(layer, color, lifetime);
            }
            else if (billboard != null)
            {
                FxManager.AfterImageOf(billboard.Renderer, color, lifetime);
            }
        }

        private void ApplyMotion(Vector3 planar)
        {
            if (!controller.enabled) return;

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += -25f * Time.deltaTime;

            var motion = planar;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        private void UpdateAim()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var ray = cam.ScreenPointToRay(VelaInput.MousePosition);
            var ground = new Plane(Vector3.up, transform.position);
            if (ground.Raycast(ray, out var enter)) AimPoint = ray.GetPoint(enter);
        }

        private void UpdateVisualState()
        {
            if (billboard == null) return;

            if (IsStunned) billboard.SetState(VisualState.Hurt);
            else if (combat != null && combat.IsBusy) billboard.SetState(VisualState.Attack);
            else if (planarVelocity.sqrMagnitude > 0.5f) billboard.SetState(VisualState.Move);
            else billboard.SetState(VisualState.Idle);
        }

        private void OnDamaged(Health self, DamageInfo info)
        {
            lastHurtTime = Time.time;
            CancelWalk();
            var armored = combat != null && combat.HasSuperArmor;
            if (armored) return;

            var knockback = info.Knockback * config.knockbackTaken * VelaSettings.Feel.knockbackScale;
            knockbackVelocity = info.Direction * knockback;
            stunnedUntil = Time.time + config.hurtStun;
            planarVelocity = Vector3.zero;
            if (combat != null) combat.Interrupt();
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            var cam = Camera.main;
            var yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
            var direction = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
            return Vector3.ClampMagnitude(direction, 1f);
        }
    }
}
