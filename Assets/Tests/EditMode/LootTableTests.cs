using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vela.Items;

namespace Vela.Tests
{
    public class LootTableTests
    {
        private static ItemDefinition MakeItem(string name, Rarity rarity = Rarity.Common)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.displayName = name;
            item.rarity = rarity;
            return item;
        }

        private static System.Func<float> Seeded(int seed)
        {
            var rng = new System.Random(seed);
            return () => (float)rng.NextDouble();
        }

        [Test]
        public void Gold_StaysInsideMinMax()
        {
            var table = ScriptableObject.CreateInstance<LootTable>();
            table.goldChance = 1f;
            table.goldMin = 3;
            table.goldMax = 8;
            table.rolls = 0;
            var random = Seeded(1);
            var results = new List<ItemStack>();

            for (var i = 0; i < 2000; i++)
            {
                var gold = table.Roll(random, results);
                Assert.That(gold, Is.InRange(3, 8));
            }
        }

        [Test]
        public void Guaranteed_AlwaysDrops()
        {
            var crown = MakeItem("Crown", Rarity.Legendary);
            var table = ScriptableObject.CreateInstance<LootTable>();
            table.goldChance = 0f;
            table.rolls = 0;
            table.guaranteed = new[] { new LootTable.Entry { item = crown, weight = 1f } };
            var random = Seeded(2);
            var results = new List<ItemStack>();

            for (var i = 0; i < 100; i++)
            {
                table.Roll(random, results);
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(crown, results[0].Item);
            }
        }

        [Test]
        public void Weights_MatchDropRates()
        {
            var common = MakeItem("Common");
            var rare = MakeItem("Rare", Rarity.Rare);
            var table = ScriptableObject.CreateInstance<LootTable>();
            table.goldChance = 0f;
            table.rolls = 1;
            table.nothingWeight = 2f;
            table.entries = new[]
            {
                new LootTable.Entry { item = common, weight = 7f },
                new LootTable.Entry { item = rare, weight = 1f }
            };

            var random = Seeded(3);
            var results = new List<ItemStack>();
            int commons = 0, rares = 0, nothing = 0;
            const int rolls = 20000;

            for (var i = 0; i < rolls; i++)
            {
                table.Roll(random, results);
                if (results.Count == 0) nothing++;
                else if (results[0].Item == common) commons++;
                else rares++;
            }

            // Expected: common 70%, rare 10%, nothing 20%.
            Assert.AreEqual(0.7, commons / (double)rolls, 0.02);
            Assert.AreEqual(0.1, rares / (double)rolls, 0.02);
            Assert.AreEqual(0.2, nothing / (double)rolls, 0.02);
        }

        [Test]
        public void StackCount_StaysInsideEntryRange()
        {
            var gel = MakeItem("Gel");
            var table = ScriptableObject.CreateInstance<LootTable>();
            table.goldChance = 0f;
            table.rolls = 1;
            table.nothingWeight = 0f;
            table.entries = new[] { new LootTable.Entry { item = gel, weight = 1f, minCount = 2, maxCount = 5 } };
            var random = Seeded(4);
            var results = new List<ItemStack>();

            for (var i = 0; i < 1000; i++)
            {
                table.Roll(random, results);
                Assert.That(results[0].Count, Is.InRange(2, 5));
            }
        }
    }
}
