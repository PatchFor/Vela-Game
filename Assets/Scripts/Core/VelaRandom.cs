using System;

namespace Vela.Core
{
    /// Every gameplay roll (crits, loot, damage variance) goes through here instead of
    /// UnityEngine.Random. Online later: the server swaps `Source` for its own seeded stream so
    /// rolls are authoritative and replayable. Tests: set a seeded source for repeatable results.
    public static class VelaRandom
    {
        private static Func<float> source = () => UnityEngine.Random.value;

        /// Uniform [0, 1).
        public static Func<float> Source
        {
            get => source;
            set => source = value ?? (() => UnityEngine.Random.value);
        }

        public static float Value => source();

        public static float Range(float min, float max) => min + (max - min) * source();

        /// Integer in [min, max] inclusive.
        public static int RangeInclusive(int min, int max) => min + Math.Min(max - min, (int)(source() * (max - min + 1)));

        public static bool Chance(float probability) => source() < probability;

        public static void UseSeed(int seed)
        {
            var rng = new System.Random(seed);
            Source = () => (float)rng.NextDouble();
        }
    }
}
