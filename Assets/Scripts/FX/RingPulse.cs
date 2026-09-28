using UnityEngine;

namespace Vela.FX
{
    /// Expanding ring on the ground: shockwaves, AoE blasts, charge-ready pings.
    public class RingPulse : MonoBehaviour
    {
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock block;
        private float from;
        private float to;
        private float duration;
        private float age;
        private Color color;

        public void Play(float startRadius, float endRadius, float newDuration, Color newColor)
        {
            if (meshRenderer == null)
            {
                gameObject.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Ring;
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.sharedMaterial = FxManager.SlashMaterial;
                block = new MaterialPropertyBlock();
            }

            from = startRadius;
            to = endRadius;
            duration = Mathf.Max(0.02f, newDuration);
            color = newColor;
            age = 0f;
            gameObject.SetActive(true);
            Apply();
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= duration)
            {
                gameObject.SetActive(false);
                return;
            }

            Apply();
        }

        private void Apply()
        {
            var t = Mathf.Clamp01(age / duration);
            var eased = 1f - (1f - t) * (1f - t);
            var r = Mathf.Lerp(from, to, eased);
            transform.localScale = new Vector3(r, 1f, r);

            var c = color;
            c.a *= 1f - t;
            FxManager.SetColor(meshRenderer, block, c);
        }
    }
}
