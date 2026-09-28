using UnityEngine;
using Vela.Config;

namespace Vela.Gameplay
{
    /// Global access to the scene's shared configs. Set by CombatGameManager; falls back to
    /// defaults so components still work in a scene without one.
    public static class VelaSettings
    {
        private static CombatFeelConfig feel;
        private static CameraConfig camera;
        private static FxLibrary fx;
        private static Items.LootConfig loot;
        private static Material fallbackUnlit;
        private static Material fallbackAdditive;
        private static Material fallbackFlash;

        public static CombatFeelConfig Feel
        {
            get
            {
                if (feel == null) feel = ScriptableObject.CreateInstance<CombatFeelConfig>();
                return feel;
            }
            set => feel = value;
        }

        public static CameraConfig Camera
        {
            get
            {
                if (camera == null) camera = ScriptableObject.CreateInstance<CameraConfig>();
                return camera;
            }
            set => camera = value;
        }

        public static FxLibrary Fx
        {
            get
            {
                if (fx == null) fx = ScriptableObject.CreateInstance<FxLibrary>();
                return fx;
            }
            set => fx = value;
        }

        public static Items.LootConfig Loot
        {
            get
            {
                if (loot == null) loot = ScriptableObject.CreateInstance<Items.LootConfig>();
                return loot;
            }
            set => loot = value;
        }

        public static Material UnlitMaterial
        {
            get
            {
                if (Fx.unlitTransparent != null) return Fx.unlitTransparent;
                if (fallbackUnlit == null) fallbackUnlit = new Material(Shader.Find("Sprites/Default"));
                return fallbackUnlit;
            }
        }

        public static Material AdditiveMaterial
        {
            get
            {
                if (Fx.additive != null) return Fx.additive;
                if (fallbackAdditive == null)
                {
                    var shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
                    fallbackAdditive = new Material(shader);
                }
                return fallbackAdditive;
            }
        }

        public static Material FlashMaterial
        {
            get
            {
                if (Fx.spriteFlash != null) return Fx.spriteFlash;
                if (fallbackFlash == null)
                {
                    var shader = Shader.Find("GUI/Text Shader") ?? Shader.Find("Sprites/Default");
                    fallbackFlash = new Material(shader);
                }
                return fallbackFlash;
            }
        }
    }
}
