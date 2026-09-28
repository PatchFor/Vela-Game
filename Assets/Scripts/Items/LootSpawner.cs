using System.Collections.Generic;
using UnityEngine;
using Vela.Core;
using Vela.Gameplay;

namespace Vela.Items
{
    /// Rolls a loot table and scatters the results around a point. Items fan out evenly (so they
    /// don't stack on each other), avoid walls and water, and rarer items land last.
    public static class LootSpawner
    {
        private static readonly List<ItemStack> Buffer = new List<ItemStack>();
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        /// `owner`: who may pick these up. Null = anyone (single-player). Online later, pass each
        /// player and spawn one private copy per player (personal loot).
        public static void Drop(LootTable table, Vector3 origin, GameObject owner = null)
        {
            if (table == null) return;

            var gold = table.Roll(VelaRandom.Source, Buffer);
            var piles = gold <= 0 ? 0 : gold >= 30 ? 3 : gold >= 10 ? 2 : 1;
            var total = Buffer.Count + piles;
            if (total == 0) return;

            var loot = VelaSettings.Loot;
            var startAngle = Random.Range(0f, 360f);
            var index = 0;

            var remainingGold = gold;
            for (var p = 0; p < piles; p++)
            {
                var amount = p == piles - 1 ? remainingGold : Mathf.Max(1, gold / piles);
                remainingGold -= amount;
                WorldItem.Spawn(ItemStack.Empty, amount, origin, Landing(origin, startAngle, index, total, loot),
                    0.02f * index).Owner = owner;
                index++;
            }

            foreach (var stack in Buffer)
            {
                var delay = 0.02f * index + loot.rarityDelay * (int)stack.Item.rarity;
                WorldItem.Spawn(stack, 0, origin, Landing(origin, startAngle, index, total, loot), delay).Owner = owner;
                index++;
            }
        }

        /// Drop a specific stack (e.g. thrown out of the inventory).
        public static WorldItem DropStack(ItemStack stack, Vector3 origin)
        {
            if (stack.IsEmpty) return null;
            var loot = VelaSettings.Loot;
            return WorldItem.Spawn(stack, 0, origin, Landing(origin, Random.Range(0f, 360f), 0, 1, loot), 0f);
        }

        private static Vector3 Landing(Vector3 origin, float startAngle, int index, int total, LootConfig loot)
        {
            var angle = startAngle + 360f / Mathf.Max(1, total) * index + Random.Range(-15f, 15f);
            var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            var distance = Random.Range(loot.scatterDistance.x, loot.scatterDistance.y);

            // Don't throw items through walls, into water or rocks.
            var start = origin + Vector3.up * 0.5f;
            var count = Physics.RaycastNonAlloc(start, direction, Hits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hitHealth = Hits[i].collider.GetComponentInParent<Vela.Core.Health>();
                if (hitHealth != null) continue;
                distance = Mathf.Min(distance, Mathf.Max(0.2f, Hits[i].distance - 0.5f));
            }

            var landing = origin + direction * distance;
            landing.y = origin.y;
            return landing;
        }
    }
}
