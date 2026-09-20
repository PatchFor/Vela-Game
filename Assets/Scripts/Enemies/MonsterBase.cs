using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vela.Core;
using Vela.Fx;
using Vela.Gameplay;

namespace Vela.Enemies
{
    /// Shared monster plumbing: senses, motor, tinting, hit reaction, death,
    /// loot and respawn. Each variant only writes its own brain in Think().
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public abstract class MonsterBase : MonoBehaviour
    {
        /// Every monster alive in the scene — the HUD walks this to draw name plates.
        public static readonly List<MonsterBase> All = new List<MonsterBase>();

        [Header("Identity")]
        [SerializeField] protected string displayName = "Monster";
        [SerializeField] protected Color baseTint = new Color(0.78f, 0.26f, 0.32f);

        [Header("Senses")]
        [SerializeField] protected float detectionRange = 14f;
        [SerializeField] protected float loseInterestMultiplier = 1.4f;

        [Header("Motor")]
        [SerializeField] protected float turnSpeedDegrees = 540f;
        [SerializeField] protected float gravity = -25f;

        [Header("Respawn")]
        [Tooltip("Seconds before the monster comes back so you can keep testing. 0 = gone for good.")]
        [SerializeField] protected float respawnSeconds = 8f;

        protected CharacterController body;
        protected Health life;
        protected Transform target;
        protected Health targetLife;

        protected Vector3 planarVelocity;
        protected Vector3 knockbackVelocity;
        protected float verticalVelocity;

        private Renderer[] tintRenderers;
        private Material[] tintMaterials;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Vector3 spawnScale;
        private LootDropper looter;

        private Color currentTint;
        private float flashUntil;
        private float stunnedUntil;
        private bool dead;

        public string DisplayName { get { return displayName; } }
        public Health Life { get { return life; } }
        public bool IsDead { get { return dead; } }
        public bool IsStunned { get { return Time.time < stunnedUntil; } }
        public Color BaseTint { get { return baseTint; } }

        /// Short label the HUD prints over the monster: CHASE, WIND-UP!, PUNISH ME...
        public string PhaseLabel { get; protected set; }
        public Color PhaseColor { get; protected set; }

        /// Set by a variant while its attack can be dodged — the HUD highlights it.
        public bool IsTelegraphing { get; protected set; }

        protected virtual void Awake()
        {
            body = GetComponent<CharacterController>();
            life = GetComponent<Health>();
            looter = GetComponent<LootDropper>();

            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            spawnScale = transform.localScale;

            tintRenderers = GetComponentsInChildren<Renderer>();
            tintMaterials = new Material[tintRenderers.Length];
            for (int i = 0; i < tintRenderers.Length; i++)
            {
                tintMaterials[i] = tintRenderers[i].material;
            }

            PhaseLabel = "IDLE";
            PhaseColor = baseTint;
        }

        protected virtual void OnEnable()
        {
            All.Add(this);

            life.Damaged += HandleDamaged;
            life.Died += HandleDied;
        }

        protected virtual void OnDisable()
        {
            All.Remove(this);

            life.Damaged -= HandleDamaged;
            life.Died -= HandleDied;
        }

        protected virtual void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                targetLife = player.GetComponent<Health>();
            }

            ApplyTint(baseTint);
        }

        protected virtual void Update()
        {
            if (dead)
            {
                planarVelocity = Vector3.zero;
                ApplyMotion(Time.deltaTime);
                return;
            }

            if (Time.time >= flashUntil && flashUntil > 0f)
            {
                flashUntil = 0f;
                ApplyTint(currentTint);
            }

            float delta = Time.deltaTime;

            if (IsStunned)
            {
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 30f * delta);
                ApplyMotion(delta);
                return;
            }

            if (!HasLivingTarget())
            {
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 20f * delta);
                SetPhase("IDLE", baseTint, false);
                ApplyMotion(delta);
                return;
            }

            Think(delta);
            ApplyMotion(delta);
        }

        /// Each variant's brain. Movement goes into planarVelocity.
        protected abstract void Think(float delta);

        protected bool HasLivingTarget()
        {
            return target != null && (targetLife == null || targetLife.IsAlive);
        }

        protected Vector3 FlatDirectionToTarget(out float distance)
        {
            if (target == null)
            {
                distance = float.MaxValue;
                return transform.forward;
            }

            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            distance = offset.magnitude;
            return distance > 0.001f ? offset / distance : transform.forward;
        }

        protected void FaceDirection(Vector3 direction, float degreesPerSecond)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion wanted = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, wanted, degreesPerSecond * Time.deltaTime);
        }

        protected void SetPhase(string label, Color tint, bool telegraphing)
        {
            PhaseLabel = label;
            PhaseColor = tint;
            IsTelegraphing = telegraphing;

            currentTint = tint;
            if (flashUntil <= 0f) ApplyTint(tint);
        }

        /// Deals damage to the player if it is inside the given radius of a point.
        protected DamageResult TryHitTarget(Vector3 center, float radius, DamageInfo info)
        {
            if (targetLife == null || target == null) return DamageResult.None;

            Vector3 offset = target.position - center;
            offset.y = 0f;
            if (offset.magnitude > radius) return DamageResult.None;

            if (info.direction == Vector3.zero)
            {
                info.direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : transform.forward;
            }

            return targetLife.Apply(info);
        }

        protected void ApplyMotion(float delta)
        {
            if (body == null || !body.enabled) return;

            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, 22f * delta);

            if (body.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += gravity * delta;

            Vector3 motion = planarVelocity + knockbackVelocity;
            motion.y = verticalVelocity;
            body.Move(motion * delta);
        }

        public void StunFor(float seconds)
        {
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);
        }

        private void HandleDamaged(Health self, DamageInfo info)
        {
            knockbackVelocity = info.direction * info.knockback;

            Flash(new Color(1f, 1f, 1f, 1f), 0.09f);
            DamagePopups.Spawn(transform.position + Vector3.up * 2.1f, DamageText(info),
                DamageColor(info), info.heavy ? 1.5f : 1.1f, 0.8f);

            if (info.heavy) StunFor(0.4f);

            OnHurt(info);
        }

        /// Hook for variants that want to say something extra about the hit.
        protected virtual void OnHurt(DamageInfo info)
        {
        }

        protected virtual string DamageText(DamageInfo info)
        {
            return "-" + info.amount;
        }

        protected virtual Color DamageColor(DamageInfo info)
        {
            return info.heavy ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 0.92f, 0.55f);
        }

        private void HandleDied(Health self, DamageInfo info)
        {
            if (dead) return;

            dead = true;
            planarVelocity = Vector3.zero;
            knockbackVelocity = info.direction * Mathf.Max(4f, info.knockback);
            SetPhase("DEAD", new Color(0.3f, 0.3f, 0.35f), false);

            Hitstop.Request(0.09f, 0.05f);
            ScreenShake.Add(0.22f, 0.3f);
            PrimitiveFx.Ring(transform.position, 1.6f, new Color(1f, 0.8f, 0.35f, 0.55f), 0.45f, 0.3f, 1.8f);
            DamagePopups.Spawn(transform.position + Vector3.up * 2.4f, "DEFEATED",
                new Color(1f, 0.8f, 0.35f), 1.4f, 1.1f);
            CombatFeed.Post(displayName + " defeated", new Color(1f, 0.8f, 0.35f));

            if (PrototypeGameManager.Instance != null) PrototypeGameManager.Instance.RegisterKill(this);
            if (looter != null) looter.Drop(transform.position);

            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            float duration = 0.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(spawnScale, spawnScale * 0.15f, t);
                ApplyTint(Color.Lerp(currentTint, new Color(0.25f, 0.25f, 0.3f), t));
                yield return null;
            }

            SetVisible(false);

            if (respawnSeconds <= 0f)
            {
                Destroy(gameObject);
                yield break;
            }

            yield return new WaitForSeconds(respawnSeconds);
            Respawn();
        }

        public void Respawn()
        {
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            transform.localScale = spawnScale;

            planarVelocity = Vector3.zero;
            knockbackVelocity = Vector3.zero;
            verticalVelocity = 0f;
            stunnedUntil = 0f;

            life.Revive();
            dead = false;

            SetVisible(true);
            SetPhase("IDLE", baseTint, false);
            OnRespawned();

            PrimitiveFx.Ring(transform.position, 1.2f, new Color(0.6f, 0.75f, 1f, 0.5f), 0.4f, 1.6f, 0.3f);
            CombatFeed.Post(displayName + " respawned", new Color(0.65f, 0.7f, 0.8f));
        }

        protected virtual void OnRespawned()
        {
        }

        private void SetVisible(bool visible)
        {
            for (int i = 0; i < tintRenderers.Length; i++)
            {
                if (tintRenderers[i] != null) tintRenderers[i].enabled = visible;
            }

            body.enabled = visible;
        }

        protected void Flash(Color color, float seconds)
        {
            flashUntil = Time.time + seconds;
            ApplyTint(color);
        }

        private void ApplyTint(Color color)
        {
            if (tintMaterials == null) return;

            for (int i = 0; i < tintMaterials.Length; i++)
            {
                if (tintMaterials[i] != null) tintMaterials[i].color = color;
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
