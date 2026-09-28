using UnityEngine;
using Vela.Gameplay;

namespace Vela.World
{
    /// Tall scenery that turns see-through while it hides the player or a monster.
    /// The OcclusionFader on the camera calls RequestFade() each frame it's in the way.
    public class FadeableObject : MonoBehaviour
    {
        [Range(0f, 1f)] [SerializeField] private float fadedAlpha = 0.28f;
        [SerializeField] private float fadeSpeed = 6f;
        [Tooltip("Keep fading this long after the last request so it doesn't flicker.")]
        [SerializeField] private float holdTime = 0.12f;

        private Renderer[] renderers;
        private Material[][] opaqueMaterials;
        private Material[][] fadeMaterials;
        private float alpha = 1f;
        private float lastRequest = -10f;
        private bool usingFade;

        public void RequestFade() => lastRequest = Time.time;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            opaqueMaterials = new Material[renderers.Length][];
            fadeMaterials = new Material[renderers.Length][];

            for (var i = 0; i < renderers.Length; i++)
            {
                var shared = renderers[i].sharedMaterials;
                opaqueMaterials[i] = shared;
                fadeMaterials[i] = new Material[shared.Length];
                for (var m = 0; m < shared.Length; m++)
                {
                    fadeMaterials[i][m] = CreateFadeCopy(shared[m]);
                }
            }
        }

        private void OnDestroy()
        {
            if (fadeMaterials == null) return;
            foreach (var set in fadeMaterials)
            {
                foreach (var material in set)
                {
                    if (material != null) Destroy(material);
                }
            }
        }

        private void Update()
        {
            var wanted = Time.time - lastRequest < holdTime ? fadedAlpha : 1f;
            alpha = Mathf.MoveTowards(alpha, wanted, fadeSpeed * Time.deltaTime);

            var shouldFade = alpha < 0.995f;
            if (shouldFade != usingFade)
            {
                usingFade = shouldFade;
                for (var i = 0; i < renderers.Length; i++)
                {
                    renderers[i].sharedMaterials = usingFade ? fadeMaterials[i] : opaqueMaterials[i];
                }
            }

            if (!usingFade) return;

            for (var i = 0; i < fadeMaterials.Length; i++)
            {
                for (var m = 0; m < fadeMaterials[i].Length; m++)
                {
                    var material = fadeMaterials[i][m];
                    if (material == null || !material.HasProperty("_Color")) continue;
                    var c = opaqueMaterials[i][m] != null && opaqueMaterials[i][m].HasProperty("_Color")
                        ? opaqueMaterials[i][m].color
                        : Color.white;
                    c.a = alpha;
                    material.color = c;
                }
            }
        }

        private static Material CreateFadeCopy(Material source)
        {
            if (source == null) return null;

            var template = VelaSettings.Fx.fadeTemplate;
            Material copy;
            if (template != null && template.shader == source.shader)
            {
                copy = new Material(template);
                copy.CopyPropertiesFromMaterial(source);
            }
            else
            {
                copy = new Material(source);
            }

            MakeTransparent(copy);
            return copy;
        }

        /// Switches a Standard (built-in) or URP Lit material to alpha-blended.
        public static void MakeTransparent(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            if (material.HasProperty("_Surface"))
            {
                // URP Lit
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                // Built-in Standard, "Fade" mode
                material.SetFloat("_Mode", 2f);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
        }
    }
}
