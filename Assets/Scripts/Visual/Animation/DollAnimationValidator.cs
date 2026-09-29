using System.Collections.Generic;

namespace Vela.Visual
{
    /// Checks the art contract of an animation set before it reaches the game, so a bad export
    /// is caught by the importer / tests instead of showing up as a flickering layer:
    ///  - every layer clip has exactly as many frames as the body's clip,
    ///  - the body has sprites for every clip,
    ///  - attacks have an Impact frame and their parts run in order
    ///    (Windup → Smear → Impact → Follow → Recovery).
    public static class DollAnimationValidator
    {
        public static List<string> Validate(DollAnimationSet set, params (string name, DollLayerSheet sheet)[] extraLayers)
        {
            var errors = new List<string>();
            if (set == null)
            {
                errors.Add("No animation set.");
                return errors;
            }

            foreach (var clip in set.clips)
            {
                if (clip == null) continue;
                var id = $"{clip.name}/{clip.direction}";
                if (clip.Count == 0) errors.Add($"{id}: has no frames.");
                if (clip.isAttack) CheckAttack(clip, id, errors);

                var body = set.body.Find(clip.name, clip.direction);
                if (body == null || body.direction != clip.direction) errors.Add($"{id}: body layer is missing this clip.");
            }

            CheckLayer(set, "body", set.body, errors);
            CheckLayer(set, "hair", set.hair, errors);
            CheckLayer(set, "smear", set.smear, errors);
            foreach (var (name, sheet) in extraLayers) CheckLayer(set, name, sheet, errors);
            return errors;
        }

        /// Attack parts must never go backwards and an Impact frame must exist.
        public static void CheckAttack(DollClip clip, string id, List<string> errors)
        {
            var hasImpact = false;
            var previous = AttackPart.Windup;
            for (var i = 0; i < clip.Count; i++)
            {
                var part = clip.frames[i].part;
                if (part == AttackPart.Impact) hasImpact = true;
                if (part < previous) errors.Add($"{id}: frame {i} is {part} after {previous} (parts must run in order).");
                previous = part;
            }
            if (!hasImpact) errors.Add($"{id}: attack has no Impact frame.");
        }

        private static void CheckLayer(DollAnimationSet set, string layer, DollLayerSheet sheet, List<string> errors)
        {
            if (sheet?.clips == null) return;
            foreach (var frames in sheet.clips)
            {
                if (frames == null) continue;
                var clip = set.Find(frames.clip, frames.direction);
                var id = $"{layer} {frames.clip}/{frames.direction}";
                if (clip == null || clip.direction != frames.direction)
                {
                    errors.Add($"{id}: no body clip with this name and facing.");
                    continue;
                }
                var count = frames.frames?.Length ?? 0;
                if (count != clip.Count) errors.Add($"{id}: {count} frames, body has {clip.Count}.");
            }
        }
    }
}
