using System.Collections.Generic;
using UnityEngine;

namespace Vela.Audio
{
    /// Placeholder sounds synthesized at runtime (noise sweeps, pitch-drop thumps, chimes) so the
    /// timing of every combat event can be heard before real SFX exist. Replace them by adding
    /// clips to SfxLibrary — nothing else needs to change.
    public static class ProceduralSfx
    {
        private const int Rate = 44100;
        private static readonly Dictionary<SfxEvent, AudioClip> Cache = new Dictionary<SfxEvent, AudioClip>();

        public static AudioClip Get(SfxEvent sfx)
        {
            if (Cache.TryGetValue(sfx, out var clip) && clip != null) return clip;
            clip = Build(sfx);
            Cache[sfx] = clip;
            return clip;
        }

        private static AudioClip Build(SfxEvent sfx)
        {
            switch (sfx)
            {
                case SfxEvent.SwingLight: return Make(sfx, 0.13f, (t, d) => Whoosh(t, d, 5000f, 1500f) * 0.5f);
                case SfxEvent.SwingHeavy: return Make(sfx, 0.26f, (t, d) => Whoosh(t, d, 2200f, 500f) * 0.7f);
                case SfxEvent.HitLight: return Make(sfx, 0.1f, (t, d) => Thump(t, 240f, 120f, 0.08f) * 0.6f + Noise(t, 0.02f) * 0.4f);
                case SfxEvent.HitMedium: return Make(sfx, 0.14f, (t, d) => Thump(t, 180f, 80f, 0.11f) * 0.75f + Noise(t, 0.03f) * 0.45f);
                case SfxEvent.HitHeavy: return Make(sfx, 0.22f, (t, d) => Thump(t, 130f, 55f, 0.18f) * 0.9f + Noise(t, 0.05f) * 0.5f);
                case SfxEvent.HitFinisher:
                    return Make(sfx, 0.32f, (t, d) => Thump(t, 110f, 40f, 0.26f) + Noise(t, 0.08f) * 0.6f + Tone(t, 1600f, 0.12f) * 0.15f);
                case SfxEvent.Crit:
                    return Make(sfx, 0.35f, (t, d) => Tone(t, 1320f, 0.3f) * 0.35f + Tone(t, 1980f, 0.22f) * 0.25f + Noise(t, 0.02f) * 0.3f);
                case SfxEvent.Break:
                    return Make(sfx, 0.4f, (t, d) => Noise(t, 0.15f) * 0.6f + Sweep(t, 900f, 300f, 0.35f) * 0.4f);
                case SfxEvent.Punish: return Make(sfx, 0.18f, (t, d) => Tone(t, 880f, 0.15f) * 0.3f + Thump(t, 160f, 70f, 0.12f) * 0.6f);
                case SfxEvent.PlayerHurt:
                    return Make(sfx, 0.2f, (t, d) => Square(t, Mathf.Lerp(200f, 90f, t / d)) * Env(t, 0.18f) * 0.35f + Noise(t, 0.05f) * 0.4f);
                case SfxEvent.Dash: return Make(sfx, 0.12f, (t, d) => Whoosh(t, d, 7000f, 3000f) * 0.35f);
                case SfxEvent.Jump: return Make(sfx, 0.16f, (t, d) => Sweep(t, 300f, 700f, 0.15f) * 0.35f);
                case SfxEvent.Land: return Make(sfx, 0.12f, (t, d) => Thump(t, 120f, 60f, 0.1f) * 0.6f + Noise(t, 0.04f) * 0.3f);
                case SfxEvent.PerfectDodge:
                    return Make(sfx, 0.5f, (t, d) => Sweep(t, 700f, 1700f, 0.45f) * 0.3f * (1f + 0.3f * Mathf.Sin(t * 60f)) + Tone(t, 2400f, 0.4f) * 0.12f);
                case SfxEvent.EnemyWindup: return Make(sfx, 0.22f, (t, d) => Square(t, Mathf.Lerp(260f, 420f, t / d)) * Env(t, 0.2f) * 0.12f);
                case SfxEvent.EnemyWindupHeavy: return Make(sfx, 0.4f, (t, d) => Square(t, Mathf.Lerp(120f, 220f, t / d)) * Env(t, 0.38f) * 0.18f);
                case SfxEvent.Kill: return Make(sfx, 0.3f, (t, d) => Noise(t, 0.06f) * 0.5f + Sweep(t, 500f, 120f, 0.25f) * 0.35f);
                case SfxEvent.BossPhase: return Make(sfx, 1.2f, (t, d) => Noise(t, 1.1f) * 0.35f * LowWobble(t) + Sweep(t, 90f, 50f, 1.1f) * 0.5f);
                case SfxEvent.SkillCast: return Make(sfx, 0.3f, (t, d) => (Tone(t, 660f, 0.28f) + Tone(t, 990f, 0.28f)) * 0.2f);
                case SfxEvent.ChargeReady: return Make(sfx, 0.25f, (t, d) => Tone(t, 1050f, 0.22f) * 0.35f);
                case SfxEvent.Pickup: return Make(sfx, 0.14f, (t, d) => Tone(t, t < 0.06f ? 700f : 1050f, 0.14f) * 0.3f);
                case SfxEvent.PickupRare:
                    return Make(sfx, 0.4f, (t, d) => Tone(t, t < 0.1f ? 660f : t < 0.2f ? 880f : 1320f, 0.4f) * 0.3f);
                case SfxEvent.Gold: return Make(sfx, 0.18f, (t, d) => (Tone(t, 2100f, 0.16f) + Tone(t, 2700f, 0.12f)) * 0.18f);
                case SfxEvent.InventoryFull: return Make(sfx, 0.18f, (t, d) => Square(t, 120f) * Env(t, 0.16f) * 0.25f);
                case SfxEvent.Shoot: return Make(sfx, 0.12f, (t, d) => Whoosh(t, d, 8000f, 4000f) * 0.3f + Tone(t, 1200f, 0.05f) * 0.1f);
                default: return Make(sfx, 0.1f, (t, d) => Tone(t, 800f, 0.08f) * 0.2f);
            }
        }

        private delegate float Wave(float t, float duration);

        private static AudioClip Make(SfxEvent sfx, float duration, Wave wave)
        {
            var count = Mathf.CeilToInt(duration * Rate);
            var data = new float[count];
            noiseState = 22222;
            filter = 0f;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)Rate;
                // Short fade-out tail to avoid clicks.
                var tail = Mathf.Clamp01((duration - t) / 0.01f);
                data[i] = Mathf.Clamp(wave(t, duration) * tail, -1f, 1f);
            }

            var clip = AudioClip.Create($"sfx_{sfx}", count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------------------------------------------------------ building blocks

        private static uint noiseState = 22222;
        private static float filter;

        private static float RawNoise()
        {
            noiseState ^= noiseState << 13;
            noiseState ^= noiseState >> 17;
            noiseState ^= noiseState << 5;
            return (noiseState / (float)uint.MaxValue) * 2f - 1f;
        }

        /// Exponential decay envelope with a 3 ms attack.
        private static float Env(float t, float length) => Mathf.Clamp01(t / 0.003f) * Mathf.Exp(-5f * t / Mathf.Max(0.001f, length));

        private static float Noise(float t, float length) => RawNoise() * Env(t, length);

        private static float Tone(float t, float frequency, float length) => Mathf.Sin(2f * Mathf.PI * frequency * t) * Env(t, length);

        private static float Square(float t, float frequency) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t));

        /// Sine whose pitch drops fast: the body of an impact.
        private static float Thump(float t, float from, float to, float length)
        {
            var f = Mathf.Lerp(to, from, Mathf.Exp(-t * 30f));
            return Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, length);
        }

        private static float Sweep(float t, float from, float to, float length)
        {
            var k = Mathf.Clamp01(t / Mathf.Max(0.001f, length));
            var f = Mathf.Lerp(from, to, k);
            return Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, length * 1.5f) * (1f - k * 0.5f);
        }

        /// Low-passed noise swelling and fading: a blade through air. Cutoff sweeps from→to.
        private static float Whoosh(float t, float duration, float from, float to)
        {
            var k = t / duration;
            var cutoff = Mathf.Lerp(from, to, k);
            var a = Mathf.Clamp01(2f * Mathf.PI * cutoff / Rate);
            filter += a * (RawNoise() - filter);
            var swell = Mathf.Sin(Mathf.PI * Mathf.Clamp01(k));
            return filter * swell * 2.5f;
        }

        private static float LowWobble(float t) => 0.6f + 0.4f * Mathf.Sin(t * 12f);
    }
}
