using UnityEngine;
using Vela.Core;

namespace Vela.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class IsometricPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 45f;
        [SerializeField] private float deceleration = 60f;
        [SerializeField] private float turnSpeedDegrees = 900f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashCooldown = 0.7f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedStick = -2f;

        [SerializeField] private Transform cameraPivot;

        private CharacterController controller;
        private Vector3 planarVelocity;
        private Vector3 dashDirection;
        private float verticalVelocity;
        private float dashTimeRemaining;
        private float dashCooldownRemaining;

        public Transform CameraPivot
        {
            get => cameraPivot;
            set => cameraPivot = value;
        }

        public bool IsDashing => dashTimeRemaining > 0f;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            var input = VelaInput.Move;
            var desired = CameraRelative(input) * moveSpeed;

            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - Time.deltaTime);

            if (VelaInput.DashPressed && dashCooldownRemaining <= 0f)
            {
                dashDirection = planarVelocity.sqrMagnitude > 0.01f
                    ? planarVelocity.normalized
                    : transform.forward;
                dashTimeRemaining = dashDuration;
                dashCooldownRemaining = dashCooldown;
            }

            if (dashTimeRemaining > 0f)
            {
                dashTimeRemaining -= Time.deltaTime;
                planarVelocity = dashDirection * dashSpeed;
            }
            else
            {
                var rate = desired.sqrMagnitude > 0.01f ? acceleration : deceleration;
                planarVelocity = Vector3.MoveTowards(planarVelocity, desired, rate * Time.deltaTime);
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStick;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            var motion = planarVelocity;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);

            FaceMovementDirection();
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            var yaw = cameraPivot != null ? cameraPivot.eulerAngles.y : 45f;
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var direction = rotation * new Vector3(input.x, 0f, input.y);
            return Vector3.ClampMagnitude(direction, 1f);
        }

        private void FaceMovementDirection()
        {
            var flat = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
            if (flat.sqrMagnitude < 0.05f) return;

            var target = Quaternion.LookRotation(flat, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, turnSpeedDegrees * Time.deltaTime);
        }
    }
}
