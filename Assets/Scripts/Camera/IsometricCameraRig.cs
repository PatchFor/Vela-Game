using UnityEngine;
using Vela.Core;

namespace Vela.CameraRig
{
    [ExecuteAlways]
    public class IsometricCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Framing")]
        [SerializeField] private float distance = 16f;
        [SerializeField] private float pitch = 35f;
        [SerializeField] private float yaw = 45f;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);

        [Header("Feel")]
        [SerializeField] private float followSmoothTime = 0.12f;
        [SerializeField] private float yawStepDegrees = 45f;
        [SerializeField] private float yawSmoothSpeed = 8f;

        private Vector3 followVelocity;
        private float currentYaw;
        private float desiredYaw;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void OnEnable()
        {
            currentYaw = yaw;
            desiredYaw = yaw;
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (Application.isPlaying)
            {
                var step = VelaInput.CameraRotateStep;
                if (!Mathf.Approximately(step, 0f)) desiredYaw += step * yawStepDegrees;
                currentYaw = Mathf.LerpAngle(currentYaw, desiredYaw, yawSmoothSpeed * Time.deltaTime);
            }
            else
            {
                currentYaw = yaw;
                desiredYaw = yaw;
            }

            var rotation = Quaternion.Euler(pitch, currentYaw, 0f);
            var focus = target.position + targetOffset;
            var wanted = focus - rotation * Vector3.forward * distance;

            transform.position = Application.isPlaying
                ? Vector3.SmoothDamp(transform.position, wanted, ref followVelocity, followSmoothTime)
                : wanted;
            transform.rotation = rotation;
        }

        private void SnapToTarget()
        {
            if (target == null) return;

            var rotation = Quaternion.Euler(pitch, currentYaw, 0f);
            transform.position = target.position + targetOffset - rotation * Vector3.forward * distance;
            transform.rotation = rotation;
        }
    }
}
