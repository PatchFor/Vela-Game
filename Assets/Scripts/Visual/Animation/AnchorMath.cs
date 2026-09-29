using UnityEngine;

namespace Vela.Visual
{
    /// Converts a frame's pixel anchors to a layer's local placement under the sprite root.
    /// Mirroring (facing left) reflects across the pivot: x flips and angles become -angle,
    /// with the attached sprite drawn flipped (a reflection composed with a rotation).
    public static class AnchorMath
    {
        public static Vector3 LocalPosition(Vector2 pixels, float pixelsPerUnit, bool mirrored)
        {
            var scale = 1f / Mathf.Max(1f, pixelsPerUnit);
            return new Vector3((mirrored ? -pixels.x : pixels.x) * scale, pixels.y * scale, 0f);
        }

        /// Degrees counter-clockwise from "pointing right" for the hand → tip direction.
        public static float Angle(Vector2 hand, Vector2 tip, bool mirrored)
        {
            var d = tip - hand;
            if (d.sqrMagnitude < 0.0001f) return 0f;
            var angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            return mirrored ? -angle : angle;
        }
    }
}
