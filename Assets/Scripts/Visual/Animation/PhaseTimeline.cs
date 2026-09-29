using System;
using System.Collections.Generic;

namespace Vela.Visual
{
    /// Picks the frame of an attack animation for a moment in the attack, stretching the drawn
    /// parts over the weapon config's windup / active / recovery. Pure logic (no Unity), so it is
    /// unit-tested and the same frame is chosen on every machine.
    ///
    ///   config windup    → Windup, then Smear frames
    ///   config active    → Impact frames (starts exactly when damage is dealt)
    ///   config recovery  → Follow, then Recovery frames
    ///
    /// Within a phase, each frame gets time in proportion to its weight. With a zero windup
    /// (e.g. a charged dash strike), the Smear frames move to the start of active.
    public static class PhaseTimeline
    {
        public const int WindupPhase = 0;
        public const int ActivePhase = 1;
        public const int RecoveryPhase = 2;

        public static int PhaseOf(AttackPart part, float windup)
        {
            switch (part)
            {
                case AttackPart.Windup: return WindupPhase;
                case AttackPart.Smear: return windup > 0f ? WindupPhase : ActivePhase;
                case AttackPart.Impact: return ActivePhase;
                default: return RecoveryPhase;
            }
        }

        /// Which config phase `elapsed` falls in (0 windup, 1 active, 2 recovery).
        public static int PhaseAt(float elapsed, float windup, float active)
        {
            if (elapsed < windup) return WindupPhase;
            if (elapsed < windup + active) return ActivePhase;
            return RecoveryPhase;
        }

        public static int FrameAt(IReadOnlyList<AttackPart> parts, IReadOnlyList<int> weights, float elapsed,
            float windup, float active, float recovery, AnimationDetail detail)
        {
            var count = parts?.Count ?? 0;
            if (count == 0) return 0;

            windup = Math.Max(0f, windup);
            active = Math.Max(0f, active);
            recovery = Math.Max(0f, recovery);
            elapsed = Math.Max(0f, elapsed);

            var phase = PhaseAt(elapsed, windup, active);
            var start = phase == WindupPhase ? 0f : phase == ActivePhase ? windup : windup + active;
            var duration = phase == WindupPhase ? windup : phase == ActivePhase ? active : recovery;
            var t = duration > 0f ? Math.Min(1f, (elapsed - start) / duration) : 1f;

            var first = -1;
            var last = -1;
            for (var i = 0; i < count; i++)
            {
                if (PhaseOf(parts[i], windup) != phase) continue;
                if (first < 0) first = i;
                last = i;
            }

            // Nothing drawn for this phase: hold the nearest frame before it (or the first frame).
            if (first < 0) return NearestBefore(parts, windup, phase);

            if (detail == AnimationDetail.KeyPoses) return KeyPose(parts, first, last, phase);

            var total = 0;
            for (var i = first; i <= last; i++)
            {
                if (PhaseOf(parts[i], windup) == phase) total += Weight(weights, i);
            }

            // Past the end of the phase shows its last frame.
            if (t >= 1f) return last;

            var position = t * total;
            for (var i = first; i <= last; i++)
            {
                if (PhaseOf(parts[i], windup) != phase) continue;
                position -= Weight(weights, i);
                if (position < 0f) return i;
            }
            return last;
        }

        /// Looping clips (idle, walk): frame for a time at a fixed frame rate.
        public static int LoopFrame(int count, float framesPerSecond, float time, bool loop)
        {
            if (count <= 0) return 0;
            var index = (int)Math.Floor(Math.Max(0f, time) * Math.Max(0.01f, framesPerSecond));
            return loop ? index % count : Math.Min(index, count - 1);
        }

        private static int Weight(IReadOnlyList<int> weights, int i) =>
            weights != null && i < weights.Count ? Math.Max(1, weights[i]) : 1;

        private static int KeyPose(IReadOnlyList<AttackPart> parts, int first, int last, int phase)
        {
            switch (phase)
            {
                case WindupPhase:
                    // The held anticipation pose: last Windup frame (not the smear).
                    for (var i = last; i >= first; i--)
                        if (parts[i] == AttackPart.Windup) return i;
                    return last;
                case ActivePhase:
                    for (var i = first; i <= last; i++)
                        if (parts[i] == AttackPart.Impact) return i;
                    return first;
                default:
                    // First follow-through pose, held until the attack ends.
                    for (var i = first; i <= last; i++)
                        if (parts[i] == AttackPart.Follow) return i;
                    return first;
            }
        }

        private static int NearestBefore(IReadOnlyList<AttackPart> parts, float windup, int phase)
        {
            var best = 0;
            for (var i = 0; i < parts.Count; i++)
            {
                if (PhaseOf(parts[i], windup) < phase) best = i;
            }
            return best;
        }
    }
}
