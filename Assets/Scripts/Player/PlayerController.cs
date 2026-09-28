using System;
using UnityEngine;
using Vela.Config;
using Vela.Core;
using Vela.FX;
using Vela.Gameplay;
using Vela.Visual;

namespace Vela.Player
{
    /// Movement, dash (with i-frames and afterimages), knockback and hurt-stun.
    /// Attacks live in PlayerCombat, which borrows movement through the hooks below.
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;

        private CharacterController controller;
        private Health health;
        private SpriteBillboard billboard;
        private PlayerCombat combat;

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
        private Vector3 facing = Vector3.back;

        public event Action Dashed;

        public PlayerConfig Config
        {
            get => config;
            set => config = value;
        }

        public Health Health => health;
        public SpriteBillboard Billboard => billboard;
        public bool IsDashing => dashTimeRemaining > 0f;
        public bool IsStunned => Time.time < stunnedUntil;
        public bool IsAlive => health != null && health.IsAlive;
        public Vector3 Facing => facing;
        public float DashCooldownNormalized =>
            config != null && config.dashCooldown > 0f ? dashCooldownRemaining / config.dashCooldown : 0f;

        /// Set by PlayerCombat each frame (attacks slow you down).
        public float MoveSpeedMultiplier { get; set; } = 1f;

        /// Where the mouse points on the ground. Updated every frame.
        public Vector3 AimPoint { get; private set; }

        public Vector3 AimDirection
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
            combat = GetComponent<PlayerCombat>();

            health.Configure(config.maxHealth, Team.Player, config.invulnerabilityAfterHit);
            if (billboard != null) billboard.Setup(config.visual);

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

        private void Update()
        {
            UpdateAim();

            if (!IsAlive)
            {
                if (billboard != null)
                {
                    billboard.SetState(VisualState.Hurt);
                    billboard.SetBaseTint(new Color(0.45f, 0.45f, 0.5f));
                }
                planarVelocity = Vector3.zero;
                ApplyMotion(Vector3.zero);
                return;
            }

            var dt = Time.deltaTime;
            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - dt);

            var input = VelaInput.Move;
            var moveDir = CameraRelative(input);

            if (VelaInput.DashPressed && CanDash()) StartDash(moveDir);

            if (IsDashing)
            {
                dashTimeRemaining -= dt;
                planarVelocity = dashDirection * config.dashSpeed;
                afterImageTimer -= dt;
                if (afterImageTimer <= 0f && billboard != null)
                {
                    afterImageTimer = config.afterImageInterval;
                    FxManager.AfterImageOf(billboard.Renderer, config.afterImageColor, config.afterImageLifetime);
                }

                if (dashTimeRemaining <= 0f) planarVelocity = dashDirection * config.moveSpeed;
            }
            else
            {
                var speed = IsStunned ? 0f : config.moveSpeed * MoveSpeedMultiplier;
                var desired = moveDir * speed;
                var rate = desired.sqrMagnitude > 0.01f ? config.acceleration : config.deceleration;
                planarVelocity = Vector3.MoveTowards(planarVelocity, desired, rate * dt);

                var attacking = combat != null && combat.IsBusy;
                if (!attacking && moveDir.sqrMagnitude > 0.01f) Face(moveDir);
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
        }

        private bool CanDash()
        {
            if (dashCooldownRemaining > 0f || IsDashing || IsStunned) return false;
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

        private void ApplyMotion(Vector3 planar)
        {
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
