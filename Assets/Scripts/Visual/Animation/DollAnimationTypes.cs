using System;
using UnityEngine;

namespace Vela.Visual
{
    /// The five parts of an attack animation, in play order. Unity maps them onto the attack's
    /// config timing: windup → Windup + Smear, active → Impact, recovery → Follow + Recovery.
    public enum AttackPart
    {
        Windup,
        Smear,
        Impact,
        Follow,
        Recovery
    }

    /// A/B test switch: how much of the drawn animation to show.
    public enum AnimationDetail
    {
        /// Old single-frame paper doll + code squash/lean (no animation set used).
        Procedural,
        /// One pose per config phase: last windup pose, impact, settled follow-through.
        KeyPoses,
        /// Every drawn frame, stretched to the config timing.
        Full
    }

    /// Per-frame data of the body animation. Positions are in art pixels from the sprite pivot
    /// (bottom center of the canvas, x right, y up), measured at pixel centers.
    [Serializable]
    public class DollFrame
    {
        [Tooltip("Which part of an attack this frame belongs to (ignored for idle/walk loops).")]
        public AttackPart part = AttackPart.Windup;
        [Tooltip("How long this frame shows compared to the others in the same phase. 3 = holds three times as long.")]
        [Min(1)] public int weight = 1;
        [Tooltip("Where hats sit (EquipmentVisual.anchorOffset is added).")]
        public Vector2 head;
        [Tooltip("Weapon grip. The weapon sprite's pivot goes here.")]
        public Vector2 hand;
        [Tooltip("A point along the blade; hand → tip gives the weapon angle.")]
        public Vector2 tip;
        [Tooltip("Draw the weapon in front of the body on this frame (off = behind).")]
        public bool weaponInFront = true;
    }

    /// One animation (idle, walk, slash...) in one facing.
    [Serializable]
    public class DollClip
    {
        public string name = "idle";
        public Direction4 direction = Direction4.Down;
        [Tooltip("Attacks follow the weapon config's timing; loops (idle, walk) play at framesPerSecond.")]
        public bool isAttack;
        public bool loop = true;
        [Min(0.1f)] public float framesPerSecond = 8f;
        public DollFrame[] frames = new DollFrame[0];

        public int Count => frames != null ? frames.Length : 0;
    }

    /// The sprites of one layer for one clip + facing. Index i matches DollClip.frames[i].
    [Serializable]
    public class DollLayerFrames
    {
        public string clip = "idle";
        public Direction4 direction = Direction4.Down;
        [Tooltip("Same count as the body's clip. Empty entries = nothing drawn on that frame.")]
        public Sprite[] frames = new Sprite[0];
    }

    /// Every frame of one layer (body, hair, a shirt, boots...). A garment only needs the clips
    /// it changes; a missing clip draws nothing.
    [Serializable]
    public class DollLayerSheet
    {
        public DollLayerFrames[] clips = new DollLayerFrames[0];

        public bool IsEmpty => clips == null || clips.Length == 0;

        public DollLayerFrames Find(string clip, Direction4 direction)
        {
            if (clips == null) return null;
            DollLayerFrames fallback = null;
            foreach (var c in clips)
            {
                if (c == null || c.clip != clip) continue;
                if (c.direction == direction) return c;
                if (c.direction == Direction4.Down) fallback = c;
            }
            return fallback;
        }

        /// The sprite for frame `index`, or null (layer hidden on that frame).
        public Sprite Get(string clip, Direction4 direction, int index)
        {
            var frames = Find(clip, direction)?.frames;
            if (frames == null || index < 0 || index >= frames.Length) return null;
            return frames[index];
        }
    }
}
