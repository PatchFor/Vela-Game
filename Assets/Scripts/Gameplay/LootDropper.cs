using UnityEngine;
using Vela.Core;

namespace Vela.Gameplay
{
    /// What a monster leaves behind. One roll per row, so a Brute can cough up
    /// gold, a potion and a shard at once.
    [System.Serializable]
    public class LootRoll
    {
        public LootKind kind = LootKind.Gold;
        [Range(0f, 1f)] public float chance = 0.8f;
        public int minCount = 1;
        public int maxCount = 1;
    }

    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private LootRoll[] table = new LootRoll[0];
        [Tooltip("Always drop at least this many of the first row, so a kill never feels empty.")]
        [SerializeField] private int guaranteedFirstRow = 1;

        public void Configure(LootRoll[] rows, int guaranteed)
        {
            table = rows;
            guaranteedFirstRow = guaranteed;
        }

        public static LootRoll[] DefaultTable()
        {
            LootRoll gold = new LootRoll();
            gold.kind = LootKind.Gold;
            gold.chance = 1f;
            gold.minCount = 1;
            gold.maxCount = 2;

            LootRoll potion = new LootRoll();
            potion.kind = LootKind.Potion;
            potion.chance = 0.35f;
            potion.minCount = 1;
            potion.maxCount = 1;

            LootRoll shard = new LootRoll();
            shard.kind = LootKind.Shard;
            shard.chance = 0.15f;
            shard.minCount = 1;
            shard.maxCount = 1;

            return new LootRoll[] { gold, potion, shard };
        }

        public void Drop(Vector3 origin)
        {
            if (table == null || table.Length == 0) table = DefaultTable();

            int dropped = 0;

            for (int i = 0; i < table.Length; i++)
            {
                LootRoll row = table[i];
                if (row == null) continue;

                bool forced = i == 0 && guaranteedFirstRow > 0;
                if (!forced && Random.value > row.chance) continue;

                int count = Mathf.Max(forced ? guaranteedFirstRow : 1,
                    Random.Range(row.minCount, row.maxCount + 1));

                for (int n = 0; n < count; n++)
                {
                    LootItem.Spawn(row.kind, origin);
                    dropped++;
                }
            }

            if (dropped > 0)
            {
                DamagePopups.Spawn(origin + Vector3.up * 1.6f, "DROP x" + dropped,
                    new Color(1f, 0.85f, 0.4f), 1.05f, 0.9f);
                CombatFeed.Post("Dropped " + dropped + " item" + (dropped == 1 ? "" : "s") + " — press F to pick up",
                    new Color(1f, 0.85f, 0.4f));
            }
        }
    }
}
