using UnityEngine;

namespace Vela.FX
{
    /// Spinning ground ring under a monster: bright for the locked target, faint for hover.
    public class TargetReticle : MonoBehaviour
    {
        private MeshRenderer ringRenderer;
        private MeshRenderer innerRenderer;
        private MaterialPropertyBlock block;
        private Transform target;
        private float radius = 1f;
        private Color color = Color.white;
        private float spin;
        private float appearTime;

        public static TargetReticle Create(string name)
        {
            var go = new GameObject(name);
            var reticle = go.AddComponent<TargetReticle>();
            reticle.Build();
            go.SetActive(false);
            return reticle;
        }

        private void Build()
        {
            block = new MaterialPropertyBlock();
            ringRenderer = Part("Ring", ProceduralMeshes.Ring);
            innerRenderer = Part("Ticks", ProceduralMeshes.Sector(40f));
        }

        private MeshRenderer Part(string partName, Mesh mesh)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = part.AddComponent<MeshRenderer>();
            r.sharedMaterial = FxManager.SlashMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        public void Show(Transform newTarget, float newRadius, Color newColor)
        {
            if (target != newTarget || !gameObject.activeSelf) appearTime = Time.unscaledTime;
            target = newTarget;
            radius = newRadius;
            color = newColor;
            gameObject.SetActive(newTarget != null);
        }

        public void Hide()
        {
            target = null;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                gameObject.SetActive(false);
                return;
            }

            spin += 90f * Time.unscaledDeltaTime;
            // Snap-in: starts big and shrinks onto the target.
            var age = Time.unscaledTime - appearTime;
            var snap = Mathf.Lerp(1.8f, 1f, Mathf.Clamp01(age / 0.15f));
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f;
            var r = radius * snap * pulse;

            transform.SetPositionAndRotation(target.position + Vector3.up * 0.07f, Quaternion.Euler(0f, spin, 0f));
            ringRenderer.transform.localScale = new Vector3(r, 1f, r);
            innerRenderer.transform.localScale = new Vector3(r * 1.12f, 1f, r * 1.12f);

            FxManager.SetColor(ringRenderer, block, color);
            var tick = color;
            tick.a *= 0.5f;
            FxManager.SetColor(innerRenderer, block, tick);
        }
    }
}
