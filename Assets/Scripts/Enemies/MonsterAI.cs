using UnityEngine;
using Vela.Core;

namespace Vela.Enemies
{
    [RequireComponent(typeof(CharacterController))]
    public class MonsterAI : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Chase,
            Windup,
            Strike,
            Recover
        }

        [Header("Senses")]
        [SerializeField] private float detectionRange = 16f;
        [SerializeField] private float strikeRange = 3.4f;

        [Header("Movement")]
        [SerializeField] private float chaseSpeed = 3.6f;
        [SerializeField] private float turnSpeedDegrees = 540f;
        [SerializeField] private float gravity = -25f;

        [Header("Attack")]
        [SerializeField] private float windupSeconds = 0.55f;
        [SerializeField] private float strikeSeconds = 0.28f;
        [SerializeField] private float strikeSpeed = 15f;
        [SerializeField] private float recoverSeconds = 0.9f;
        [SerializeField] private float hitRadius = 1.4f;
        [SerializeField] private int damage = 1;

        [Header("Telegraph")]
        [SerializeField] private Color chaseTint = new Color(0.78f, 0.26f, 0.32f);
        [SerializeField] private Color windupTint = new Color(1f, 0.92f, 0.35f);
        [SerializeField] private Color strikeTint = new Color(1f, 0.35f, 0.20f);
        [SerializeField] private Color recoverTint = new Color(0.42f, 0.24f, 0.30f);

        private CharacterController controller;
        private Renderer tintRenderer;
        private Transform target;
        private Health targetHealth;

        private State state = State.Idle;
        private float stateTimer;
        private Vector3 planarVelocity;
        private Vector3 lockedStrikeDirection;
        private float verticalVelocity;
        private bool strikeLanded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            tintRenderer = GetComponentInChildren<Renderer>();
        }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            target = player.transform;
            targetHealth = player.GetComponent<Health>();
            ApplyTint(chaseTint);
        }

        private void Update()
        {
            stateTimer -= Time.deltaTime;

            if (target == null || (targetHealth != null && !targetHealth.IsAlive))
            {
                EnterState(State.Idle);
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 20f * Time.deltaTime);
                Move();
                return;
            }

            var toTarget = FlatDirectionToTarget(out var distance);

            switch (state)
            {
                case State.Idle:
                    planarVelocity = Vector3.zero;
                    if (distance <= detectionRange) EnterState(State.Chase);
                    break;

                case State.Chase:
                    planarVelocity = toTarget * chaseSpeed;
                    FaceDirection(toTarget, turnSpeedDegrees);
                    if (distance <= strikeRange) EnterState(State.Windup);
                    else if (distance > detectionRange * 1.3f) EnterState(State.Idle);
                    break;

                case State.Windup:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 30f * Time.deltaTime);
                    FaceDirection(toTarget, turnSpeedDegrees * 0.35f);
                    if (stateTimer <= 0f)
                    {
                        lockedStrikeDirection = transform.forward;
                        strikeLanded = false;
                        EnterState(State.Strike);
                    }
                    break;

                case State.Strike:
                    planarVelocity = lockedStrikeDirection * strikeSpeed;
                    TryLandHit(distance);
                    if (stateTimer <= 0f) EnterState(State.Recover);
                    break;

                case State.Recover:
                    planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 25f * Time.deltaTime);
                    if (stateTimer <= 0f) EnterState(State.Chase);
                    break;
            }

            Move();
        }

        private void TryLandHit(float distance)
        {
            if (strikeLanded || targetHealth == null) return;
            if (distance > hitRadius) return;

            if (targetHealth.TryDamage(damage)) strikeLanded = true;
        }

        private void EnterState(State next)
        {
            if (state == next) return;

            state = next;
            stateTimer = next switch
            {
                State.Windup => windupSeconds,
                State.Strike => strikeSeconds,
                State.Recover => recoverSeconds,
                _ => 0f
            };

            ApplyTint(next switch
            {
                State.Windup => windupTint,
                State.Strike => strikeTint,
                State.Recover => recoverTint,
                _ => chaseTint
            });
        }

        private Vector3 FlatDirectionToTarget(out float distance)
        {
            var offset = target.position - transform.position;
            offset.y = 0f;
            distance = offset.magnitude;
            return distance > 0.001f ? offset / distance : transform.forward;
        }

        private void FaceDirection(Vector3 direction, float degreesPerSecond)
        {
            if (direction.sqrMagnitude < 0.001f) return;

            var wanted = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, wanted, degreesPerSecond * Time.deltaTime);
        }

        private void Move()
        {
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += gravity * Time.deltaTime;

            var motion = planarVelocity;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        private void ApplyTint(Color color)
        {
            if (tintRenderer == null) return;
            tintRenderer.material.color = color;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, strikeRange);
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
    }
}
