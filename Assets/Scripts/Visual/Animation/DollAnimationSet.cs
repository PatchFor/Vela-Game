using UnityEngine;

namespace Vela.Visual
{
    /// A body animation set on a big canvas (64×64): every clip in every facing, the body and
    /// hair frames, the smear FX layer, and per-frame anchors for hats and weapons.
    /// Equipment only supplies its own layer (EquipmentVisual.frames / anchored), so one set of
    /// animations works with every outfit and weapon skin.
    [CreateAssetMenu(menuName = "Vela/Doll Animation Set", fileName = "DollAnimationSet")]
    public class DollAnimationSet : ScriptableObject
    {
        [Tooltip("Canvas size in pixels (square). The pivot is the bottom center.")]
        public int canvasPixels = 64;
        [Tooltip("Height of the character inside the canvas, in pixels. The game scales this to the player's worldHeight, so the extra canvas is swing room.")]
        public float characterHeightPixels = 44f;
        [Tooltip("Sprite pixels per unit used when the sprites were made (anchors are converted with it).")]
        public float pixelsPerUnit = 16f;
        [Tooltip("Clip played for an attack whose AttackStep.animation is empty or missing (bow, skills...).")]
        public string fallbackAttack = "slash";

        public DollClip[] clips = new DollClip[0];

        [Header("Layers that belong to the body, not to equipment")]
        public DollLayerSheet body = new DollLayerSheet();
        public DollLayerSheet hair = new DollLayerSheet();
        [Tooltip("Motion smear drawn on Smear frames (additive).")]
        public DollLayerSheet smear = new DollLayerSheet();

        /// The clip for a name + facing; falls back to the Down facing, then to null.
        public DollClip Find(string clipName, Direction4 direction)
        {
            if (clips == null || string.IsNullOrEmpty(clipName)) return null;
            DollClip fallback = null;
            foreach (var c in clips)
            {
                if (c == null || c.name != clipName) continue;
                if (c.direction == direction) return c;
                if (c.direction == Direction4.Down) fallback = c;
            }
            return fallback;
        }

        public float UnitsPerPixel => 1f / Mathf.Max(1f, pixelsPerUnit);
    }
}
