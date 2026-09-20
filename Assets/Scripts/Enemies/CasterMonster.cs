using UnityEngine;
using Vela.Core;
using Vela.Fx;

namespace Vela.Enemies
{
    /// Variant 2 — the Caster. Never wants to be near you. Keeps a preferred
    /// distance, strafes, and lobs slow orbs you can sidestep or roll through.
    /// Blinks away when you get on top of it, so closing the gap is the puzzle:
    /// use the Lunge Thrust, not a walk.
    public class CasterMonster : MonsterBase
    {
        private enum State
        {
            Idle,
            Reposition,
            Cast,
            Recover
        }

        [Header("Spacing")]
        [SerializeField] private float preferredDistance = 7.5f;
        [SerializeField] private float distanceTolerance = 1.5f;
        [SerializeField] private float tooCloseDistance = 4.5f;
        [SerializeField] private float driftSpeed = 3f;
        [SerializeField] private float retreatSpeed = 5f;
        [SerializeField] private float strafeFlipSeconds = 1.6f;

        [Header("Casting")]
        [SerializeField] private float castWindupSeconds = 0.8f;
        [SerializeField] private float castRecoverSeconds = 1.2f;
        [SerializeField] private float projectileSpeed = 9f;
        [SerializeField] private float projectileRadius = 0.32f;
        [SerializeField] private float projectileLife = 4f;
        [SerializeField] private int projectileDamage = 1;
        [SerializeField] private float projectileKnockback = 5f;
        [Tooltip("Every Nth cast is a three-orb fan instead of a single shot.")]
        [SerializeField] private int fanEveryNthCast = 2;
        [SerializeField] private float fanSpreadDegrees = 22f;

        [Header("Blink")]
        [SerializeField] private float blinkTriggerDistance = 2.8f;
        [SerializeField] private float blinkDistance = 5f;
        [SerializeField] private float blinkCooldown = 5f;

        [Header("Tints")]
        [SerializeField] private Color calmTint = new Color(0.55f, 0.35f, 0.85f);
        [SerializeField] private Color castTint = new Color(1f, 0.55f, 1f);
        [SerializeField] private Color recoverTint = new Color(0.35f, 0.25f, 0.5f);

        private State state = State.Idle;
        private float stateTimer;
        private float strafeTimer;
        private float strafeSign = 1f;
        private float blinkReadyAt;
        private int castCount;

        protected override void Think(float delta)
        {
            stateTimer -= delta;
            strafeTimer -= delta;

            if (strafeTimer <= 0f)
            {
                strafeTimer = strafeFlipSeconds;
                strafeSign = Random.value < 0.5f ? -1f : 1f;
            }

            float distance;
            Vector3 toTarget = FlatDirectionToTarget(out distance);

            switch (state)
            {
                case State.Idle:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 18f * delta);
                    if (distance <= detectionRange) Enter(State.Reposition);
                    break;

                case State.Reposition:
                    FaceDirection(toTarget, turnSpeedDegrees);
                    planarVelocity = SpacingVelocity(toTarget, distance);

                    if (distance <= blinkTriggerDistance && Time.time >= blinkReadyAt) Blink(toTarget);

                    if (distance > detectionRange * loseInterestMultiplier)
                    {
                        Enter(State.Idle);
                    }
                    else if (stateTimer <= 0f && Mathf.Abs(distance - preferredDistance) <= distanceTolerance)
                    {
                        Enter(State.Cast);
                    }
                    break;

                case State.Cast:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 24f * delta);
                    FaceDirection(toTarget, turnSpeedDegrees * 0.6f);
                    if (stateTimer <= 0f)
                    {
                        Fire(toTarget);
                        Enter(State.Recover);
                    }
                    break;

                case State.Recover:
                    planarVelocity = SpacingVelocity(toTarget, distance) * 0.6f;
                    FaceDirection(toTarget, turnSpeedDegrees * 0.5f);
                    if (distance <= blinkTriggerDistance && Time.time >= blinkReadyAt) Blink(toTarget);
                    if (stateTimer <= 0f) Enter(State.Reposition);
                    break;
            }
        }

        private Vector3 SpacingVelocity(Vector3 toTarget, float distance)
        {
            Vector3 strafe = Vector3.Cross(Vector3.up, toTarget) * strafeSign;

            if (distance < tooCloseDistance)
            {
                return (-toTarget * retreatSpeed) + strafe * (driftSpeed * 0.4f);
            }

            if (distance > preferredDistance + distanceTolerance)
            {
                return (toTarget * driftSpeed * 0.8f) + strafe * (driftSpeed * 0.3f);
            }

            return strafe * driftSpeed;
        }

        private void Blink(Vector3 toTarget)
        {
            blinkReadyAt = Time.time + blinkCooldown;

            Vector3 away = -toTarget;
            float travel = blinkDistance;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, away, out hit, blinkDistance + 1f, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                travel = Mathf.Max(0f, hit.distance - 1f);
            }

            if (travel < 0.5f) return;

            PrimitiveFx.Ring(transform.position, 1.1f, new Color(0.7f, 0.4f, 1f, 0.5f), 0.3f, 0.4f, 1.6f);

            body.enabled = false;
            transform.position += away * travel;
            body.enabled = true;

            PrimitiveFx.Ring(transform.position, 1.1f, new Color(0.7f, 0.4f, 1f, 0.5f), 0.3f, 1.6f, 0.4f);
            CombatFeed.Post(displayName + " blinked away", new Color(0.72f, 0.5f, 1f));
        }

        private void Fire(Vector3 toTarget)
        {
            castCount++;
            bool fan = fanEveryNthCast > 0 && castCount % fanEveryNthCast == 0;

            Vector3 origin = transform.position + Vector3.up * 1.1f + toTarget * 0.8f;
            DamageInfo payload = DamageInfo
                .Create(projectileDamage, Team.Monster, toTarget, displayName + " orb")
                .WithFeedback(projectileKnockback, 0.05f, 0.2f, false);

            if (fan)
            {
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 direction = Quaternion.Euler(0f, i * fanSpreadDegrees, 0f) * toTarget;
                    Projectile.Spawn(origin, direction, projectileSpeed, projectileRadius, payload,
                        new Color(1f, 0.45f, 0.95f, 0.95f), projectileLife);
                }

                CombatFeed.Post(displayName + " fans three orbs", new Color(1f, 0.55f, 1f));
            }
            else
            {
                Projectile.Spawn(origin, toTarget, projectileSpeed, projectileRadius, payload,
                    new Color(1f, 0.45f, 0.95f, 0.95f), projectileLife);
            }

            ScreenShake.Add(0.05f, 0.15f);
        }

        private void Enter(State next)
        {
            if (state == next) return;

            state = next;

            switch (next)
            {
                case State.Cast:
                    stateTimer = castWindupSeconds;
                    SetPhase("CASTING — sidestep!", castTint, true);
                    PrimitiveFx.Beam(transform.position, new Color(1f, 0.5f, 1f, 0.45f), castWindupSeconds);
                    break;

                case State.Recover:
                    stateTimer = castRecoverSeconds;
                    SetPhase("COOLDOWN — close in!", recoverTint, false);
                    break;

                case State.Reposition:
                    stateTimer = 0.4f;
                    SetPhase("KITING", calmTint, false);
                    break;

                default:
                    stateTimer = 0f;
                    SetPhase("IDLE", baseTint, false);
                    break;
            }
        }

        protected override void OnRespawned()
        {
            state = State.Idle;
            stateTimer = 0f;
            castCount = 0;
            blinkReadyAt = 0f;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.7f, 0.4f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, preferredDistance);
            Gizmos.color = new Color(1f, 0.3f, 0.9f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, tooCloseDistance);
        }
    }
}
