using UnityEngine;
using Vela.Config;
using Vela.Core;
using Vela.Gameplay;

namespace Vela.CameraRig
{
    /// Trauma-based screen shake. Add trauma (0..1); shake strength is trauma squared.
    public static class CameraShake
    {
        public static float Trauma { get; private set; }

        public static void Add(float amount)
        {
            if (amount <= 0f) return;
            Trauma = Mathf.Clamp01(Trauma + amount * VelaSettings.Feel.cameraShakeScale);
        }

        public static void Decay(float amount) => Trauma = Mathf.Max(0f, Trauma - amount);

        public static void Reset() => Trauma = 0f;
    }

    /// Locked 3/4 camera like Alabaster Dawn: fixed angle, follows the player, and the only
    /// input is zoom (mouse wheel or +/-). All numbers live in CameraConfig.
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CombatCameraRig : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;
        [SerializeField] private Transform target;

        private Camera cam;
        private Vector3 focus;
        private Vector3 focusVelocity;
        private float distance;
        private float targetDistance;
        private float zoomVelocity;
        private float shakeTime;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public CameraConfig Config
        {
            get => config;
            set => config = value;
        }

        private CameraConfig Settings => config != null ? config : VelaSettings.Camera;

        private void OnEnable()
        {
            cam = GetComponent<Camera>();
            var settings = Settings;
            targetDistance = distance = Mathf.Clamp(settings.startDistance, settings.minDistance, settings.maxDistance);
            if (target != null) focus = target.position + settings.targetOffset;
            Apply(0f);
        }

        private void LateUpdate()
        {
            var settings = Settings;
            if (target == null) return;

            if (!Application.isPlaying)
            {
                targetDistance = distance = Mathf.Clamp(settings.startDistance, settings.minDistance, settings.maxDistance);
                focus = target.position + settings.targetOffset;
                Apply(0f);
                return;
            }

            var zoom = VelaInput.Zoom;
            if (Mathf.Abs(zoom) > 0.001f)
            {
                targetDistance = Mathf.Clamp(targetDistance - zoom * settings.zoomStep,
                    settings.minDistance, settings.maxDistance);
            }

            var dt = Time.unscaledDeltaTime;
            distance = Mathf.SmoothDamp(distance, targetDistance, ref zoomVelocity, settings.zoomSmoothTime,
                Mathf.Infinity, dt);

            var wanted = target.position + settings.targetOffset;
            var player = CombatRegistry.Player;
            if (player != null && player.transform == target)
            {
                var toAim = player.AimPoint - target.position;
                toAim.y = 0f;
                wanted += Vector3.ClampMagnitude(toAim * 0.25f, settings.aimLookAhead);
            }

            focus = Vector3.SmoothDamp(focus, wanted, ref focusVelocity, settings.followSmoothTime, Mathf.Infinity, dt);

            shakeTime += dt;
            CameraShake.Decay(settings.shakeDecay * dt);
            Apply(CameraShake.Trauma);
        }

        private void Apply(float trauma)
        {
            var settings = Settings;
            if (cam == null) cam = GetComponent<Camera>();

            cam.orthographic = settings.orthographic;
            cam.fieldOfView = settings.fieldOfView;
            cam.orthographicSize = distance * settings.orthoSizePerDistance;

            var rotation = Quaternion.Euler(settings.pitch, settings.yaw, 0f);
            var position = focus - rotation * Vector3.forward * distance;

            if (trauma > 0f)
            {
                var shake = trauma * trauma;
                var f = settings.shakeFrequency;
                var offset = new Vector3(
                    Mathf.PerlinNoise(shakeTime * f, 0.1f) * 2f - 1f,
                    Mathf.PerlinNoise(0.3f, shakeTime * f) * 2f - 1f,
                    0f) * (settings.maxShakeOffset * shake);
                position += rotation * offset;
                var roll = (Mathf.PerlinNoise(shakeTime * f, 7.7f) * 2f - 1f) * settings.maxShakeRoll * shake;
                rotation *= Quaternion.Euler(0f, 0f, roll);
            }

            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
