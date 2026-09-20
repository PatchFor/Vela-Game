using UnityEngine;
using Vela.Core;
using Vela.Fx;

namespace Vela.Enemies
{
    /// Variant 1 — the Rusher. Fast, fragile, relentless. Closes the gap, flashes
    /// yellow, then commits to a straight lunge with a locked direction.
    /// Teaches dodge timing: roll through the lunge, punish the long recovery.
    public class RusherMonster : MonsterBase
    {
        private enum State
        {
            Idle,
            Chase,
            Windup,
            Strike,
            Recover
        }

        [Header("Rusher")]
        [SerializeField] private float chaseSpeed = 4.4f;
        [SerializeField] private float strikeRange = 3.2f;
        [SerializeField] private float windupSeconds = 0.5f;
        [SerializeField] private float strikeSeconds = 0.26f;
        [SerializeField] private float strikeSpeed = 17f;
        [SerializeField] private float recoverSeconds = 1f;
        [SerializeField] private float hitRadius = 1.6f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float knockback = 7f;

        [Header("Tints")]
        [SerializeField] private Color chaseTint = new Color(0.85f, 0.30f, 0.34f);
        [SerializeField] private Color windupTint = new Color(1f, 0.92f, 0.35f);
        [SerializeField] private Color strikeTint = new Color(1f, 0.38f, 0.18f);
        [SerializeField] private Color recoverTint = new Color(0.45f, 0.26f, 0.32f);

        private State state = State.Idle;
        private float stateTimer;
        private Vector3 lockedDirection;
        private bool strikeLanded;

        protected override void Think(float delta)
        {
            stateTimer -= delta;

            float distance;
            Vector3 toTarget = FlatDirectionToTarget(out distance);

            switch (state)
            {
                case State.Idle:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 20f * delta);
                    if (distance <= detectionRange) Enter(State.Chase);
                    break;

                case State.Chase:
                    planarVelocity = toTarget * chaseSpeed;
                    FaceDirection(toTarget, turnSpeedDegrees);
                    if (distance <= strikeRange) Enter(State.Windup);
                    else if (distance > detectionRange * loseInterestMultiplier) Enter(State.Idle);
                    break;

                case State.Windup:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 30f * delta);
                    // Tracks slowly, so walking away is not a free answer — but it
                    // cannot follow a roll.
                    FaceDirection(toTarget, turnSpeedDegrees * 0.35f);
                    if (stateTimer <= 0f)
                    {
                        lockedDirection = transform.forward;
                        strikeLanded = false;
                        Enter(State.Strike);
                    }
                    break;

                case State.Strike:
                    planarVelocity = lockedDirection * strikeSpeed;
                    TryLand();
                    if (stateTimer <= 0f) Enter(State.Recover);
                    break;

                case State.Recover:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 25f * delta);
                    if (stateTimer <= 0f) Enter(State.Chase);
                    break;
            }
        }

        private void TryLand()
        {
            if (strikeLanded) return;

            DamageInfo info = DamageInfo.Create(damage, Team.Monster, lockedDirection, displayName + " lunge")
                .WithFeedback(knockback, 0.05f, 0.25f, false);

            DamageResult result = TryHitTarget(transform.position, hitRadius, info);
            if (result == DamageResult.Hit || result == DamageResult.Killed) strikeLanded = true;
        }

        private void Enter(State next)
        {
            if (state == next) return;

            state = next;

            switch (next)
            {
                case State.Windup:
                    stateTimer = windupSeconds;
                    SetPhase("WIND-UP — dodge!", windupTint, true);
                    PrimitiveFx.Ring(transform.position, hitRadius, new Color(1f, 0.9f, 0.3f, 0.35f),
                        windupSeconds, 0.3f, 1f);
                    break;

                case State.Strike:
                    stateTimer = strikeSeconds;
                    SetPhase("LUNGE", strikeTint, false);
                    break;

                case State.Recover:
                    stateTimer = recoverSeconds;
                    SetPhase("RECOVER — punish!", recoverTint, false);
                    break;

                case State.Chase:
                    stateTimer = 0f;
                    SetPhase("CHASE", chaseTint, false);
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
            strikeLanded = false;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, strikeRange);
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
    }
}
