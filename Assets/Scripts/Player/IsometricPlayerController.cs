using UnityEngine;
using Vela.Core;
using Vela.Fx;

namespace Vela.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class IsometricPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6.5f;
        [SerializeField] private float acceleration = 55f;
        [SerializeField] private float deceleration = 70f;
        [SerializeField] private float turnSpeedDegrees = 900f;

        [Header("Dodge roll")]
        [SerializeField] private float dodgeSpeed = 17f;
        [SerializeField] private float dodgeDuration = 0.2f;
        [SerializeField] private float dodgeCooldown = 0.75f;
        [SerializeField] private float dodgeInvulnerabilityBonus = 0.12f;

        [Header("Perfect dodge")]
        [Tooltip("Slow-motion applied when i-frames eat an incoming attack.")]
        [SerializeField] private float perfectDodgeSlowMotion = 0.3f;
        [SerializeField] private float perfectDodgeSeconds = 0.35f;
        [Tooltip("How long the follow-up damage bonus lasts after a perfect dodge.")]
        [SerializeField] private float focusWindowSeconds = 2f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedStick = -2f;

        [SerializeField] private Transform cameraPivot;

        private CharacterController controller;
        private Health health;

        private Vector3 planarVelocity;
        private Vector3 dodgeDirection;
        private Vector3 impulseVelocity;
        private Vector3 knockbackVelocity;

        private float verticalVelocity;
        private float dodgeTimeRemaining;
        private float dodgeCooldownRemaining;
        private float impulseTimeRemaining;
        private float actionLockRemaining;
        private float actionMoveScale = 1f;
        private float focusUntil;

        public Transform CameraPivot
        {
            get { return cameraPivot; }
            set { cameraPivot = value; }
        }

        public bool IsDodging { get { return dodgeTimeRemaining > 0f; } }
        public bool IsActionLocked { get { return actionLockRemaining > 0f; } }
        public bool HasFocus { get { return Time.time < focusUntil; } }
        public float FocusRemaining { get { return Mathf.Max(0f, focusUntil - Time.time); } }
        public float DodgeCooldownRemaining { get { return dodgeCooldownRemaining; } }

        /// 1 when the dodge is ready, 0 right after using it.
        public float DodgeReadyNormalized
        {
            get { return dodgeCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(dodgeCooldownRemaining / dodgeCooldown); }
        }

        public Vector3 AimDirection
        {
            get
            {
                Vector3 input = CameraRelative(VelaInput.Move);
                return input.sqrMagnitude > 0.01f ? input.normalized : transform.forward;
            }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.Dodged += OnAttackDodged;
            health.Damaged += OnDamaged;
            health.Healed += OnHealed;
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.Dodged -= OnAttackDodged;
            health.Damaged -= OnDamaged;
            health.Healed -= OnHealed;
        }

        private void Update()
        {
            float delta = Time.deltaTime;

            dodgeCooldownRemaining = Mathf.Max(0f, dodgeCooldownRemaining - delta);
            actionLockRemaining = Mathf.Max(0f, actionLockRemaining - delta);
            if (actionLockRemaining <= 0f) actionMoveScale = 1f;

            if (VelaInput.DodgePressed) TryDodge();

            if (dodgeTimeRemaining > 0f)
            {
                dodgeTimeRemaining -= delta;
                planarVelocity = dodgeDirection * dodgeSpeed;
            }
            else if (impulseTimeRemaining > 0f)
            {
                impulseTimeRemaining -= delta;
                planarVelocity = impulseVelocity;
            }
            else
            {
                Vector3 desired = CameraRelative(VelaInput.Move) * moveSpeed * actionMoveScale;
                float rate = desired.sqrMagnitude > 0.01f ? acceleration : deceleration;
                planarVelocity = Vector3.MoveTowards(planarVelocity, desired, rate * delta);
            }

            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, 26f * delta);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = groundedStick;
            else verticalVelocity += gravity * delta;

            Vector3 motion = planarVelocity + knockbackVelocity;
            motion.y = verticalVelocity;
            controller.Move(motion * delta);

            FaceMovementDirection();
        }

        public bool TryDodge()
        {
            if (dodgeCooldownRemaining > 0f || IsDodging) return false;

            Vector3 input = CameraRelative(VelaInput.Move);
            dodgeDirection = input.sqrMagnitude > 0.01f ? input.normalized : transform.forward;
            dodgeTimeRemaining = dodgeDuration;
            dodgeCooldownRemaining = dodgeCooldown;

            // Cancel whatever attack was running — dodge always wins.
            actionLockRemaining = 0f;
            actionMoveScale = 1f;
            impulseTimeRemaining = 0f;

            if (health != null) health.GrantInvulnerability(dodgeDuration + dodgeInvulnerabilityBonus);

            transform.rotation = Quaternion.LookRotation(dodgeDirection, Vector3.up);
            PrimitiveFx.Ring(transform.position, 0.9f, new Color(0.55f, 0.85f, 1f, 0.55f), 0.25f, 0.6f, 1.6f);
            CombatFeed.Post("Dodge roll — i-frames", new Color(0.6f, 0.85f, 1f));
            return true;
        }

        /// Straight-line burst used by the lunging attack.
        public void AddImpulse(Vector3 velocity, float duration)
        {
            impulseVelocity = velocity;
            impulseTimeRemaining = duration;
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f || force <= 0f) return;

            knockbackVelocity = direction.normalized * force;
        }

        /// Commits the player to an attack: movement is scaled down for a while.
        public void SetActionLock(float duration, float moveScale)
        {
            actionLockRemaining = Mathf.Max(actionLockRemaining, duration);
            actionMoveScale = Mathf.Clamp01(moveScale);
        }

        public void ClearActionLock()
        {
            actionLockRemaining = 0f;
            actionMoveScale = 1f;
        }

        public void FaceAim()
        {
            Vector3 aim = AimDirection;
            if (aim.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(aim, Vector3.up);
        }

        public void ConsumeFocus()
        {
            focusUntil = 0f;
        }

        private void OnDamaged(Health self, DamageInfo info)
        {
            ApplyKnockback(info.direction, Mathf.Max(3f, info.knockback));

            Hitstop.Request(0.07f, 0.06f);
            ScreenShake.Add(Mathf.Max(0.2f, info.shake), 0.3f);
            DamagePopups.Spawn(transform.position + Vector3.up * 2f, "-" + info.amount,
                new Color(1f, 0.35f, 0.35f), 1.35f, 0.9f);
            PrimitiveFx.Spark(transform.position + Vector3.up * 1.1f, new Color(1f, 0.3f, 0.3f, 0.9f), 0.5f);
            CombatFeed.Post(info.label + " hit you for " + info.amount, new Color(1f, 0.45f, 0.45f));
        }

        private void OnHealed(Health self, int amount)
        {
            DamagePopups.Spawn(transform.position + Vector3.up * 2f, "+" + amount,
                new Color(0.45f, 0.95f, 0.55f), 1.2f, 0.9f);
        }

        private void OnAttackDodged(Health self, DamageInfo info)
        {
            // Only i-frames from the roll count as a "perfect" dodge; the free
            // frames after taking a hit should not be rewarded.
            if (!IsDodging) return;

            focusUntil = Time.time + focusWindowSeconds;
            dodgeCooldownRemaining = 0f;

            Hitstop.Request(perfectDodgeSeconds, perfectDodgeSlowMotion);
            ScreenShake.Add(0.12f, 0.25f);
            DamagePopups.Spawn(transform.position + Vector3.up * 2.2f, "PERFECT DODGE",
                new Color(0.55f, 0.95f, 1f), 1.5f, 1.1f);
            CombatFeed.Post("PERFECT DODGE — dodge refunded, next hit empowered",
                new Color(0.55f, 0.95f, 1f));
            PrimitiveFx.Ring(transform.position, 1.6f, new Color(0.6f, 0.95f, 1f, 0.5f), 0.4f, 0.4f, 1.9f);
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            float yaw = cameraPivot != null ? cameraPivot.eulerAngles.y : 45f;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 direction = rotation * new Vector3(input.x, 0f, input.y);
            return Vector3.ClampMagnitude(direction, 1f);
        }

        private void FaceMovementDirection()
        {
            if (IsActionLocked) return;

            Vector3 flat = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
            if (flat.sqrMagnitude < 0.05f) return;

            Quaternion target = Quaternion.LookRotation(flat, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, turnSpeedDegrees * Time.deltaTime);
        }
    }
}
