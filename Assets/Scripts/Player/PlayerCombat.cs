using System.Collections.Generic;
using UnityEngine;
using Vela.Core;
using Vela.Fx;

namespace Vela.Player
{
    /// One tunable melee move. Three of these make the whole player kit.
    [System.Serializable]
    public class AttackMove
    {
        public string moveName = "Slash";
        public string keyLabel = "J / LMB";

        [Header("Timing (seconds)")]
        public float windup = 0.07f;
        public float active = 0.12f;
        public float recovery = 0.15f;
        public float cooldown = 0.05f;

        [Header("Hitbox")]
        public float range = 2.8f;
        [Range(10f, 360f)] public float arcDegrees = 120f;
        public int damage = 1;

        [Header("Impact")]
        public float knockback = 3f;
        public float hitstop = 0.04f;
        public float shake = 0.06f;
        public bool heavy;

        [Header("Motion")]
        public float lungeSpeed;
        public float lungeDuration;
        [Range(0f, 1f)] public float moveScale = 0.25f;

        public Color color = new Color(1f, 0.85f, 0.45f);

        public float TotalSeconds { get { return windup + active + recovery; } }
    }

    [RequireComponent(typeof(IsometricPlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        public enum Phase
        {
            Ready,
            Windup,
            Active,
            Recovery
        }

        [SerializeField]
        private AttackMove[] moves = new AttackMove[0];

        [Header("Combo")]
        [Tooltip("How long after a Slash the next one keeps the chain alive.")]
        [SerializeField] private float comboWindow = 0.85f;
        [SerializeField] private int comboLength = 3;
        [Tooltip("Extra damage the chain finisher deals.")]
        [SerializeField] private int finisherBonusDamage = 1;

        [Header("Input")]
        [SerializeField] private float inputBufferSeconds = 0.25f;

        [Header("Targeting")]
        [SerializeField] private float targetPadding = 0.6f;
        [SerializeField] private LayerMask hitMask = ~0;

        private IsometricPlayerController motor;
        private Health health;

        private readonly List<Health> alreadyHit = new List<Health>(8);
        private readonly Collider[] overlapBuffer = new Collider[32];

        private float[] cooldownRemaining;
        private int currentMove = -1;
        private Phase phase = Phase.Ready;
        private float phaseTimer;

        private int bufferedMove = -1;
        private float bufferedUntil;

        private int comboStep;
        private float comboExpiresAt;

        public int MoveCount { get { return moves != null ? moves.Length : 0; } }
        public int ComboStep { get { return comboStep; } }
        public int ComboLength { get { return comboLength; } }
        public bool ComboActive { get { return comboStep > 0 && Time.time < comboExpiresAt; } }
        public Phase CurrentPhase { get { return phase; } }
        public int CurrentMoveIndex { get { return currentMove; } }

        public AttackMove GetMove(int index)
        {
            if (moves == null || index < 0 || index >= moves.Length) return null;
            return moves[index];
        }

        /// 1 = ready to use, 0 = just used.
        public float CooldownNormalized(int index)
        {
            AttackMove move = GetMove(index);
            if (move == null || cooldownRemaining == null || index >= cooldownRemaining.Length) return 1f;

            float window = move.cooldown + move.TotalSeconds;
            if (window <= 0f) return 1f;
            return 1f - Mathf.Clamp01(cooldownRemaining[index] / window);
        }

        private void Awake()
        {
            motor = GetComponent<IsometricPlayerController>();
            health = GetComponent<Health>();

            if (moves == null || moves.Length == 0) moves = BuildDefaultMoves();
            cooldownRemaining = new float[moves.Length];
        }

        /// Called by the scene builder so a fresh scene starts with tuned values.
        public void ResetToDefaults()
        {
            moves = BuildDefaultMoves();
            cooldownRemaining = new float[moves.Length];
        }

        public static AttackMove[] BuildDefaultMoves()
        {
            AttackMove slash = new AttackMove();
            slash.moveName = "Slash";
            slash.keyLabel = "J / LMB";
            slash.windup = 0.07f;
            slash.active = 0.12f;
            slash.recovery = 0.15f;
            slash.cooldown = 0.05f;
            slash.range = 2.8f;
            slash.arcDegrees = 120f;
            slash.damage = 1;
            slash.knockback = 3f;
            slash.hitstop = 0.04f;
            slash.shake = 0.06f;
            slash.moveScale = 0.25f;
            slash.color = new Color(1f, 0.85f, 0.45f);

            AttackMove thrust = new AttackMove();
            thrust.moveName = "Lunge Thrust";
            thrust.keyLabel = "K / RMB";
            thrust.windup = 0.12f;
            thrust.active = 0.16f;
            thrust.recovery = 0.26f;
            thrust.cooldown = 1.9f;
            thrust.range = 3.4f;
            thrust.arcDegrees = 55f;
            thrust.damage = 2;
            thrust.knockback = 6f;
            thrust.hitstop = 0.06f;
            thrust.shake = 0.12f;
            thrust.lungeSpeed = 16f;
            thrust.lungeDuration = 0.18f;
            thrust.moveScale = 0f;
            thrust.color = new Color(0.55f, 0.9f, 1f);

            AttackMove spin = new AttackMove();
            spin.moveName = "Spin Smash";
            spin.keyLabel = "L / MMB";
            spin.windup = 0.42f;
            spin.active = 0.22f;
            spin.recovery = 0.5f;
            spin.cooldown = 4.5f;
            spin.range = 3.6f;
            spin.arcDegrees = 360f;
            spin.damage = 3;
            spin.knockback = 10f;
            spin.hitstop = 0.11f;
            spin.shake = 0.3f;
            spin.heavy = true;
            spin.moveScale = 0f;
            spin.color = new Color(1f, 0.45f, 0.3f);

            return new AttackMove[] { slash, thrust, spin };
        }

        private void Update()
        {
            float delta = Time.deltaTime;

            for (int i = 0; i < cooldownRemaining.Length; i++)
            {
                cooldownRemaining[i] = Mathf.Max(0f, cooldownRemaining[i] - delta);
            }

            if (comboStep > 0 && Time.time > comboExpiresAt) comboStep = 0;

            ReadInput();

            if (health != null && !health.IsAlive)
            {
                phase = Phase.Ready;
                currentMove = -1;
                return;
            }

            // A dodge roll cancels any attack in progress.
            if (motor != null && motor.IsDodging && phase != Phase.Ready)
            {
                phase = Phase.Ready;
                currentMove = -1;
            }

            TickPhase(delta);
        }

        private void ReadInput()
        {
            int requested = -1;
            if (VelaInput.Attack1Pressed) requested = 0;
            else if (VelaInput.Attack2Pressed) requested = 1;
            else if (VelaInput.Attack3Pressed) requested = 2;

            if (requested < 0 || requested >= MoveCount) return;

            if (phase == Phase.Ready)
            {
                TryStart(requested);
            }
            else
            {
                // Queue it — chaining feels far better than dropping the press.
                bufferedMove = requested;
                bufferedUntil = Time.time + inputBufferSeconds;
            }
        }

        private void TickPhase(float delta)
        {
            if (phase == Phase.Ready)
            {
                if (bufferedMove >= 0 && Time.time <= bufferedUntil)
                {
                    int queued = bufferedMove;
                    bufferedMove = -1;
                    TryStart(queued);
                }
                return;
            }

            phaseTimer -= delta;
            AttackMove move = GetMove(currentMove);
            if (move == null)
            {
                phase = Phase.Ready;
                return;
            }

            if (phase == Phase.Active) SweepForHits(move);

            if (phaseTimer > 0f) return;

            switch (phase)
            {
                case Phase.Windup:
                    phase = Phase.Active;
                    phaseTimer = move.active;
                    alreadyHit.Clear();
                    SpawnSwingFx(move);
                    if (move.lungeSpeed > 0f && motor != null)
                    {
                        motor.AddImpulse(transform.forward * move.lungeSpeed, move.lungeDuration);
                    }
                    break;

                case Phase.Active:
                    phase = Phase.Recovery;
                    phaseTimer = move.recovery;
                    break;

                case Phase.Recovery:
                    phase = Phase.Ready;
                    currentMove = -1;
                    if (motor != null) motor.ClearActionLock();

                    if (bufferedMove >= 0 && Time.time <= bufferedUntil)
                    {
                        int queued = bufferedMove;
                        bufferedMove = -1;
                        TryStart(queued);
                    }
                    break;
            }
        }

        private bool TryStart(int index)
        {
            AttackMove move = GetMove(index);
            if (move == null) return false;
            if (cooldownRemaining[index] > 0f) return false;
            if (motor != null && motor.IsDodging) return false;
            if (health != null && !health.IsAlive) return false;

            currentMove = index;
            phase = Phase.Windup;
            phaseTimer = move.windup;
            cooldownRemaining[index] = move.cooldown + move.TotalSeconds;

            if (index == 0)
            {
                comboStep = Time.time < comboExpiresAt ? Mathf.Min(comboStep + 1, comboLength) : 1;
                comboExpiresAt = Time.time + move.TotalSeconds + comboWindow;
            }
            else
            {
                comboStep = 0;
            }

            if (motor != null)
            {
                motor.FaceAim();
                motor.SetActionLock(move.TotalSeconds, move.moveScale);
            }

            return true;
        }

        private void SpawnSwingFx(AttackMove move)
        {
            if (move.arcDegrees >= 300f)
            {
                PrimitiveFx.Ring(transform.position, move.range, Fade(move.color, 0.5f), 0.3f, 0.25f, 1.1f);
                ScreenShake.Add(move.shake * 0.4f, 0.2f);
            }
            else
            {
                PrimitiveFx.Slash(transform, move.range, move.arcDegrees, Fade(move.color, 0.55f), 0.14f);
            }
        }

        private void SweepForHits(AttackMove move)
        {
            int damage = DamageFor(move);
            float reach = move.range + targetPadding;
            int count = Physics.OverlapSphereNonAlloc(transform.position, reach, overlapBuffer, hitMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider collider = overlapBuffer[i];
                if (collider == null) continue;

                Health target = collider.GetComponentInParent<Health>();
                if (target == null || target == health) continue;
                if (target.Side == Team.Player || !target.IsAlive) continue;
                if (alreadyHit.Contains(target)) continue;

                Vector3 toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.magnitude > reach) continue;

                if (move.arcDegrees < 360f)
                {
                    float angle = Vector3.Angle(transform.forward, toTarget);
                    if (angle > move.arcDegrees * 0.5f) continue;
                }

                alreadyHit.Add(target);
                LandHit(move, target, toTarget, damage);
            }
        }

        private int DamageFor(AttackMove move)
        {
            int damage = move.damage;

            bool finisher = currentMove == 0 && comboStep >= comboLength;
            if (finisher) damage += finisherBonusDamage;
            if (motor != null && motor.HasFocus) damage += 1;

            return damage;
        }

        private void LandHit(AttackMove move, Health target, Vector3 toTarget, int damage)
        {
            bool finisher = currentMove == 0 && comboStep >= comboLength;
            bool focused = motor != null && motor.HasFocus;
            float knockback = finisher ? move.knockback * 2f : move.knockback;

            DamageInfo info = DamageInfo.Create(damage, Team.Player, toTarget, move.moveName)
                .WithFeedback(knockback, move.hitstop, move.shake, move.heavy || finisher);

            DamageResult result = target.Apply(info);
            if (result == DamageResult.None) return;

            Vector3 contact = target.transform.position + Vector3.up * 1f - toTarget.normalized * 0.4f;

            if (result == DamageResult.Dodged)
            {
                DamagePopups.Spawn(contact, "BLOCKED", new Color(0.75f, 0.78f, 0.85f), 1f, 0.7f);
                PrimitiveFx.Spark(contact, new Color(0.8f, 0.85f, 0.95f, 0.8f), 0.35f);
                CombatFeed.Post(move.moveName + " glanced off " + target.DisplayName,
                    new Color(0.75f, 0.78f, 0.85f));
                return;
            }

            PrimitiveFx.Spark(contact, Fade(move.color, 0.9f), move.heavy ? 0.75f : 0.45f);
            Hitstop.Request(move.hitstop, 0.04f);
            ScreenShake.Add(move.shake, 0.22f);

            if (focused) motor.ConsumeFocus();

            string prefix = finisher ? "FINISHER " : (focused ? "FOCUS " : "");
            CombatFeed.Post(prefix + move.moveName + " hit " + target.DisplayName + " for " + damage,
                new Color(1f, 0.87f, 0.5f));
        }

        private static Color Fade(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void OnDrawGizmosSelected()
        {
            AttackMove move = GetMove(0);
            if (move == null) return;

            Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, move.range);
        }
    }
}
