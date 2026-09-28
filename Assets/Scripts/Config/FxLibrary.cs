using UnityEngine;

namespace Vela.Config
{
    /// Materials and textures the runtime effects share. Referenced from the scene so the
    /// shaders (and the transparent keyword variants) are included in builds. Every field
    /// falls back to Shader.Find at runtime if it's empty.
    [CreateAssetMenu(menuName = "Vela/FX Library", fileName = "FxLibrary")]
    public class FxLibrary : ScriptableObject
    {
        [Tooltip("Unlit, alpha-blended, vertex colored (Sprites/Default).")]
        public Material unlitTransparent;
        [Tooltip("Additive glow for slashes and sparks.")]
        public Material additive;
        [Tooltip("Solid-color sprite silhouette for hit flashes and afterimages (GUI/Text Shader).")]
        public Material spriteFlash;
        [Tooltip("Standard material already in Fade mode; the occlusion fader copies it.")]
        public Material fadeTemplate;

        public Texture2D softCircle;
        public Sprite arrowSprite;
        public Sprite orbSprite;
    }
}
