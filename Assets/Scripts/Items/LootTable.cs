using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vela.Items
{
    /// What a monster drops. Rolled once on death:
    ///   - gold: `goldChance` to drop between goldMin..goldMax
    ///   - guaranteed entries always drop
    ///   - `rolls` weighted picks from `entries` ("nothingWeight" is the chance of an empty roll)
    [CreateAssetMenu(menuName = "Vela/Loot Table", fileName = "Loot")]
    public class LootTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public ItemDefinition item;
            [Min(0f)] public float weight = 1f;
            [Min(1)] public int minCount = 1;
            [Min(1)] public int maxCount = 1;
        }

        [Header("Gold")]
        [Range(0f, 1f)] public float goldChance = 0.8f;
        public int goldMin = 1;
        public int goldMax = 5;

        [Header("Items")]
        [Tooltip("Always dropped.")]
        public Entry[] guaranteed = new Entry[0];
        [Tooltip("Number of weighted picks from `entries`.")]
        [Min(0)] public int rolls = 1;
        [Tooltip("Weight of an empty roll. Higher = fewer drops.")]
        [Min(0f)] public float nothingWeight = 1f;
        public Entry[] entries = new Entry[0];

        /// Rolls the table. `random01` returns a uniform value in [0,1) — pass UnityEngine.Random.value
        /// in the game, or a seeded source in tests.
        public int Roll(Func<float> random01, List<ItemStack> results)
        {
            results.Clear();
            var gold = 0;

            if (goldMax > 0 && random01() < goldChance)
            {
                var lo = Mathf.Min(goldMin, goldMax);
                var hi = Mathf.Max(goldMin, goldMax);
                gold = lo + Mathf.Min(hi - lo, Mathf.FloorToInt(random01() * (hi - lo + 1)));
            }

            foreach (var entry in guaranteed)
            {
                if (entry?.item != null) results.Add(new ItemStack(entry.item, Count(entry, random01)));
            }

            var total = nothingWeight;
            foreach (var entry in entries)
            {
                if (entry?.item != null) total += entry.weight;
            }

            for (var r = 0; r < rolls && total > 0f; r++)
            {
                var pick = random01() * total - nothingWeight;
                if (pick < 0f) continue;

                foreach (var entry in entries)
                {
                    if (entry?.item == null) continue;
                    pick -= entry.weight;
                    if (pick >= 0f) continue;
                    results.Add(new ItemStack(entry.item, Count(entry, random01)));
                    break;
                }
            }

            return gold;
        }

        private static int Count(Entry entry, Func<float> random01)
        {
            var lo = Mathf.Max(1, Mathf.Min(entry.minCount, entry.maxCount));
            var hi = Mathf.Max(lo, entry.maxCount);
            return lo + Mathf.Min(hi - lo, Mathf.FloorToInt(random01() * (hi - lo + 1)));
        }
    }
}
