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

        /// Current zoom-in kick as a fraction of camera distance (0 = none).
        public static float ZoomPunch { get; private set; }

        /// Quick zoom-in kick on big hits. `amount` is a fraction of the camera distance.
        public static void Punch(float amount)
        {
            if (amount <= 0f) return;
            ZoomPunch = Mathf.Clamp(Mathf.Max(ZoomPunch, amount * VelaSettings.Feel.zoomPunchScale), 0f, 0.4f);
        }

        public static void Decay(float amount) => Trauma = Mathf.Max(0f, Trauma - amount);

        /// Zoom punch eases back out over ~0.2 s (real time).
        public static void DecayPunch(float dt) => ZoomPunch = Mathf.MoveTowards(ZoomPunch * Mathf.Exp(-12f * dt), 0f, 0.02f * dt);

        public static void Reset()
        {
            Trauma = 0f;
            ZoomPunch = 0f;
        }
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
                // Lean toward the locked target if there is one, else a little toward the cursor.
                var targeting = player.GetComponent<Player.PlayerTargeting>();
                var lookAt = targeting != null && targeting.Target != null ? targeting.Target.transform.position : player.AimPoint;
                var toAim = lookAt - target.position;
                toAim.y = 0f;
                wanted += Vector3.ClampMagnitude(toAim * (targeting != null && targeting.Target != null ? 0.4f : 0.25f),
                    settings.aimLookAhead);
            }

            focus = Vector3.SmoothDamp(focus, wanted, ref focusVelocity, settings.followSmoothTime, Mathf.Infinity, dt);

            shakeTime += dt;
            CameraShake.Decay(settings.shakeDecay * dt);
            CameraShake.DecayPunch(dt);
            Apply(CameraShake.Trauma, CameraShake.ZoomPunch);
        }

        private void Apply(float trauma, float zoomPunch = 0f)
        {
            var settings = Settings;
            if (cam == null) cam = GetComponent<Camera>();

            var effectiveDistance = distance * (1f - zoomPunch);
            cam.orthographic = settings.orthographic;
            cam.fieldOfView = settings.fieldOfView;
            cam.orthographicSize = effectiveDistance * settings.orthoSizePerDistance;

            var rotation = Quaternion.Euler(settings.pitch, settings.yaw, 0f);
            var position = focus - rotation * Vector3.forward * effectiveDistance;

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
