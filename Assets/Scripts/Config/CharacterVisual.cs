using System;
using UnityEngine;

namespace Vela.Config
{
    /// How a character is drawn. Drop your own sprites in here to replace the placeholders:
    /// a single `sprite` is enough; fill the frame arrays when you have animation.
    [Serializable]
    public class CharacterVisual
    {
        [Tooltip("Fallback sprite used when a state has no frames.")]
        public Sprite sprite;

        [Tooltip("Optional animation frames. Empty = use `sprite`.")]
        public Sprite[] idleFrames = new Sprite[0];
        public Sprite[] moveFrames = new Sprite[0];
        public Sprite[] attackFrames = new Sprite[0];
        public Sprite[] hurtFrames = new Sprite[0];
        public float framesPerSecond = 8f;

        [Tooltip("Multiplied into the sprite color.")]
        public Color tint = Color.white;

        [Tooltip("The sprite is scaled so it is this tall in world units, whatever its pixel size.")]
        public float worldHeight = 1.8f;

        [Tooltip("Tick if the art faces right. The sprite is mirrored when the character faces the other way.")]
        public bool artFacesRight = true;

        [Tooltip("Width of the blob shadow under the feet, in world units.")]
        public float shadowSize = 1f;

        [Tooltip("Lift the sprite off the ground (flyers). The shadow stays on the floor.")]
        public float hoverHeight;

        [Header("Procedural motion (for single-frame art)")]
        [Tooltip("Hop height while moving.")]
        public float moveBob = 0.08f;
        [Tooltip("Idle breathing squash amount.")]
        public float idleBreath = 0.03f;
        [Tooltip("Tilt (degrees) while moving.")]
        public float moveLean = 6f;
    }
}
