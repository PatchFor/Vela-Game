using UnityEngine;

namespace Vela.Fx
{
    /// Throwaway visual effects built from Unity primitives — no prefabs, no
    /// particle assets, nothing to import. Ugly on purpose; readable on purpose.
    public static class PrimitiveFx
    {
        private static Shader cachedShader;

        /// A flat blade sweep in front of an attacker.
        public static void Slash(Transform owner, float range, float arcDegrees, Color color, float life)
        {
            if (owner == null) return;

            GameObject go = CreateShape(PrimitiveType.Cube, color);
            float width = Mathf.Lerp(range * 0.6f, range * 1.6f, Mathf.Clamp01(arcDegrees / 180f));

            go.transform.position = owner.position + Vector3.up * 0.2f + owner.forward * (range * 0.5f);
            go.transform.rotation = owner.rotation;
            go.transform.localScale = new Vector3(width, 0.08f, range * 0.75f);

            FxFade fade = go.AddComponent<FxFade>();
            fade.Play(life, 1f, 1.25f);
        }

        /// A flat disc on the ground: shockwaves, spin attacks, slam telegraphs.
        public static GameObject Ring(Vector3 center, float radius, Color color, float life, float startScale, float endScale)
        {
            GameObject go = CreateShape(PrimitiveType.Cylinder, color);
            go.transform.position = new Vector3(center.x, center.y - 0.9f, center.z);
            go.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

            FxFade fade = go.AddComponent<FxFade>();
            fade.Play(life, startScale, endScale);
            return go;
        }

        /// A small pop at the point of contact.
        public static void Spark(Vector3 position, Color color, float size)
        {
            GameObject go = CreateShape(PrimitiveType.Cube, color);
            go.transform.position = position;
            go.transform.rotation = Random.rotation;
            go.transform.localScale = Vector3.one * size;

            FxFade fade = go.AddComponent<FxFade>();
            fade.Play(0.18f, 1.4f, 0.1f);
        }

        /// A vertical column — used to mark a caster's incoming cast.
        public static void Beam(Vector3 position, Color color, float life)
        {
            GameObject go = CreateShape(PrimitiveType.Cylinder, color);
            go.transform.position = position + Vector3.up * 1.5f;
            go.transform.localScale = new Vector3(0.35f, 1.5f, 0.35f);

            FxFade fade = go.AddComponent<FxFade>();
            fade.Play(life, 1f, 0.2f);
        }

        /// A bare, collider-less, unlit primitive — the building block for every effect here.
        public static GameObject CreateShape(PrimitiveType type, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = "Fx";

            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            Renderer renderer = go.GetComponent<Renderer>();
            Material material = new Material(GetShader());
            material.color = color;
            SetTransparent(material);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return go;
        }

        private static Shader GetShader()
        {
            if (cachedShader != null) return cachedShader;

            cachedShader = Shader.Find("Sprites/Default");
            if (cachedShader == null) cachedShader = Shader.Find("Unlit/Color");
            if (cachedShader == null) cachedShader = Shader.Find("Standard");
            return cachedShader;
        }

        private static void SetTransparent(Material material)
        {
            if (material.shader == null || material.shader.name != "Standard") return;

            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
        }
    }

    /// Scales and fades a throwaway effect, then deletes itself and its material.
    public class FxFade : MonoBehaviour
    {
        private Material material;
        private Vector3 baseScale;
        private Color baseColor;
        private float life = 0.2f;
        private float age;
        private float startScale = 1f;
        private float endScale = 1.2f;

        public void Play(float lifeSeconds, float from, float to)
        {
            life = Mathf.Max(0.02f, lifeSeconds);
            startScale = from;
            endScale = to;
            age = 0f;

            baseScale = transform.localScale;

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                material = renderer.sharedMaterial;
                if (material != null) baseColor = material.color;
            }

            transform.localScale = baseScale * startScale;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);

            transform.localScale = baseScale * Mathf.Lerp(startScale, endScale, t);

            if (material != null)
            {
                Color color = baseColor;
                color.a = baseColor.a * (1f - t);
                material.color = color;
            }

            if (t >= 1f) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
