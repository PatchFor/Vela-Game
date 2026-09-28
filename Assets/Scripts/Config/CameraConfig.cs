using UnityEngine;

namespace Vela.Config
{
    /// Locked 3/4 camera. Angle and lens are fixed; only zoom (mouse wheel / +/-) changes.
    [CreateAssetMenu(menuName = "Vela/Camera Config", fileName = "Camera")]
    public class CameraConfig : ScriptableObject
    {
        [Header("Angle (locked)")]
        [Tooltip("Degrees looking down. ~50 is the classic top-down 3/4 view.")]
        [Range(20f, 89f)] public float pitch = 50f;
        [Tooltip("0 = looking north, straight on, like Alabaster Dawn.")]
        public float yaw;

        [Header("Lens")]
        [Tooltip("Orthographic = flat, no perspective. Perspective with a narrow FOV is closer to the reference.")]
        public bool orthographic;
        [Range(10f, 70f)] public float fieldOfView = 30f;
        [Tooltip("Orthographic only: orthographic size = distance x this.")]
        public float orthoSizePerDistance = 0.28f;

        [Header("Zoom")]
        public float minDistance = 10f;
        public float maxDistance = 34f;
        public float startDistance = 20f;
        [Tooltip("Distance change per mouse-wheel notch.")]
        public float zoomStep = 2.5f;
        public float zoomSmoothTime = 0.12f;

        [Header("Follow")]
        public float followSmoothTime = 0.1f;
        public Vector3 targetOffset = new Vector3(0f, 0.8f, 0f);
        [Tooltip("How far the camera leans toward the mouse cursor (world units).")]
        public float aimLookAhead = 1.5f;

        [Header("Shake")]
        public float maxShakeOffset = 0.6f;
        public float maxShakeRoll = 2f;
        public float shakeFrequency = 28f;
        [Tooltip("How fast shake dies out (trauma per second).")]
        public float shakeDecay = 2.2f;
    }
}
