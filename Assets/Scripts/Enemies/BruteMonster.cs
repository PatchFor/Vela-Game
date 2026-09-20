using UnityEngine;
using Vela.Core;
using Vela.Fx;

namespace Vela.Enemies
{
    /// Variant 3 — the Brute. Slow, heavy, and armoured across the front, so
    /// trading blows head-on barely scratches it. Its ground slam is huge and
    /// slow: roll through it, come out behind, and hit the unarmoured back
    /// during the long recovery. This is the positioning fight.
    public class BruteMonster : MonsterBase
    {
        private enum State
        {
            Idle,
            Approach,
            Windup,
            Slam,
            Recover
        }

        [Header("Brute")]
        [SerializeField] private float walkSpeed = 2.3f;
        [SerializeField] private float slamTriggerRange = 3.4f;
        [SerializeField] private float windupSeconds = 1.15f;
        [SerializeField] private float slamSeconds = 0.18f;
        [SerializeField] private float recoverSeconds = 1.8f;
        [SerializeField] private float slamRadius = 4.4f;
        [SerializeField] private int slamDamage = 2;
        [SerializeField] private float slamKnockback = 13f;

        [Header("Front armour")]
        [Tooltip("Total width of the armoured arc, centred on the Brute's facing.")]
        [SerializeField] private float armorArcDegrees = 130f;
        [Range(0f, 1f)]
        [SerializeField] private float armorReduction = 0.75f;
        [Tooltip("How much of an incoming knockback the Brute shrugs off.")]
        [Range(0f, 1f)]
        [SerializeField] private float knockbackResistance = 0.8f;

        [Header("Tints")]
        [SerializeField] private Color calmTint = new Color(0.32f, 0.45f, 0.62f);
        [SerializeField] private Color windupTint = new Color(1f, 0.75f, 0.25f);
        [SerializeField] private Color slamTint = new Color(1f, 0.35f, 0.2f);
        [SerializeField] private Color recoverTint = new Color(0.25f, 0.32f, 0.45f);

        private State state = State.Idle;
        private float stateTimer;
        private bool slamLanded;
        private bool lastHitWasArmored;
        private GameObject telegraphRing;

        /// True while the player stands inside the armoured arc — the HUD warns about it.
        public bool PlayerInArmorArc { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            life.IncomingDamageFilter = FrontArmorFilter;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (life != null) life.IncomingDamageFilter = null;
        }

        private int FrontArmorFilter(DamageInfo info)
        {
            lastHitWasArmored = false;
            if (info.direction == Vector3.zero) return info.amount;

            // info.direction travels attacker -> victim, so the attacker sits the other way.
            float angle = Vector3.Angle(transform.forward, -info.direction);
            if (angle > armorArcDegrees * 0.5f) return info.amount;

            lastHitWasArmored = true;
            return Mathf.Max(1, Mathf.RoundToInt(info.amount * (1f - armorReduction)));
        }

        protected override void Think(float delta)
        {
            stateTimer -= delta;

            float distance;
            Vector3 toTarget = FlatDirectionToTarget(out distance);
            PlayerInArmorArc = Vector3.Angle(transform.forward, toTarget) <= armorArcDegrees * 0.5f;

            switch (state)
            {
                case State.Idle:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 12f * delta);
                    if (distance <= detectionRange) Enter(State.Approach);
                    break;

                case State.Approach:
                    planarVelocity = toTarget * walkSpeed;
                    FaceDirection(toTarget, turnSpeedDegrees * 0.5f);
                    if (distance <= slamTriggerRange) Enter(State.Windup);
                    else if (distance > detectionRange * loseInterestMultiplier) Enter(State.Idle);
                    break;

                case State.Windup:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 16f * delta);
                    // Barely turns once committed: rolling behind it beats the slam.
                    FaceDirection(toTarget, turnSpeedDegrees * 0.12f);
                    if (stateTimer <= 0f) Enter(State.Slam);
                    break;

                case State.Slam:
                    planarVelocity = Vector3.zero;
                    if (!slamLanded) LandSlam();
                    if (stateTimer <= 0f) Enter(State.Recover);
                    break;

                case State.Recover:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 10f * delta);
                    if (stateTimer <= 0f) Enter(State.Approach);
                    break;
            }
        }

        private void LandSlam()
        {
            slamLanded = true;

            PrimitiveFx.Ring(transform.position, slamRadius, new Color(1f, 0.45f, 0.2f, 0.55f), 0.35f, 0.4f, 1.15f);
            ScreenShake.Add(0.35f, 0.4f);
            Hitstop.Request(0.06f, 0.1f);

            DamageInfo info = DamageInfo.Create(slamDamage, Team.Monster, Vector3.zero, displayName + " slam")
                .WithFeedback(slamKnockback, 0.08f, 0.4f, true);

            TryHitTarget(transform.position, slamRadius, info);
        }

        private void Enter(State next)
        {
            if (state == next) return;

            state = next;

            switch (next)
            {
                case State.Windup:
                    stateTimer = windupSeconds;
                    slamLanded = false;
                    SetPhase("SLAM INCOMING — roll behind!", windupTint, true);
                    CombatFeed.Post(displayName + " winds up a slam", new Color(1f, 0.75f, 0.3f));
                    telegraphRing = PrimitiveFx.Ring(transform.position, slamRadius,
                        new Color(1f, 0.6f, 0.2f, 0.28f), windupSeconds, 0.15f, 1f);
                    break;

                case State.Slam:
                    stateTimer = slamSeconds;
                    SetPhase("SLAM", slamTint, false);
                    if (telegraphRing != null) Destroy(telegraphRing);
                    break;

                case State.Recover:
                    stateTimer = recoverSeconds;
                    SetPhase("EXHAUSTED — hit its back!", recoverTint, false);
                    break;

                case State.Approach:
                    stateTimer = 0f;
                    SetPhase("STOMPING", calmTint, false);
                    break;

                default:
                    stateTimer = 0f;
                    SetPhase("IDLE", baseTint, false);
                    break;
            }
        }

        protected override void OnHurt(DamageInfo info)
        {
            knockbackVelocity *= 1f - knockbackResistance;

            if (lastHitWasArmored)
            {
                CombatFeed.Post("Front armour absorbed most of that — get behind it!",
                    new Color(0.7f, 0.78f, 0.9f));
            }
        }

        protected override string DamageText(DamageInfo info)
        {
            return lastHitWasArmored ? "ARMOUR -" + info.amount : "-" + info.amount;
        }

        protected override Color DamageColor(DamageInfo info)
        {
            return lastHitWasArmored ? new Color(0.72f, 0.78f, 0.9f) : new Color(1f, 0.92f, 0.55f);
        }

        protected override void OnRespawned()
        {
            state = State.Idle;
            stateTimer = 0f;
            slamLanded = false;
            lastHitWasArmored = false;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            Gizmos.color = new Color(1f, 0.45f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, slamRadius);

            Gizmos.color = new Color(0.5f, 0.75f, 1f, 0.8f);
            Vector3 left = Quaternion.Euler(0f, -armorArcDegrees * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, armorArcDegrees * 0.5f, 0f) * transform.forward;
            Gizmos.DrawRay(transform.position, left * 3f);
            Gizmos.DrawRay(transform.position, right * 3f);
        }
    }
}
