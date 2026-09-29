using NUnit.Framework;
using UnityEngine;
using Vela.Items;

namespace Vela.Tests
{
    /// Inventory rules from docs/specs/loot-and-inventory.md.
    public class InventoryTests
    {
        private static ItemDefinition MakeItem(string name, int maxStack, ItemCategory category = ItemCategory.Material)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.displayName = name;
            item.maxStack = maxStack;
            item.category = category;
            return item;
        }

        [Test]
        public void Add_FillsExistingStackBeforeEmptySlots()
        {
            var gel = MakeItem("Gel", 10);
            var inv = new Inventory(4);

            inv.TryAdd(gel, 6);
            inv.TryAdd(gel, 3);

            Assert.AreEqual(9, inv[0].Count);
            Assert.IsTrue(inv[1].IsEmpty);
        }

        [Test]
        public void Add_OverflowSpillsIntoNextSlot()
        {
            var gel = MakeItem("Gel", 10);
            var inv = new Inventory(4);

            var added = inv.TryAdd(gel, 15);

            Assert.AreEqual(15, added);
            Assert.AreEqual(10, inv[0].Count);
            Assert.AreEqual(5, inv[1].Count);
        }

        [Test]
        public void Add_WhenFull_ReturnsZeroAndChangesNothing()
        {
            var helm = MakeItem("Helm", 1);
            var inv = new Inventory(2);
            inv.TryAdd(helm, 1);
            inv.TryAdd(helm, 1);

            Assert.IsTrue(inv.IsFull);
            Assert.AreEqual(0, inv.TryAdd(helm, 1));
            Assert.AreEqual(2, inv.Count(helm));
        }

        [Test]
        public void Add_WhenFullButStackHasRoom_StillAdds()
        {
            var gel = MakeItem("Gel", 10);
            var helm = MakeItem("Helm", 1);
            var inv = new Inventory(2);
            inv.TryAdd(gel, 4);
            inv.TryAdd(helm, 1);

            Assert.IsTrue(inv.IsFull);
            Assert.AreEqual(6, inv.TryAdd(gel, 6));
            Assert.AreEqual(10, inv[0].Count);
        }

        [Test]
        public void Add_PartialFit_ReportsHowManyWereAdded()
        {
            var gel = MakeItem("Gel", 10);
            var inv = new Inventory(1);
            inv.TryAdd(gel, 7);

            var added = inv.TryAdd(gel, 5);

            Assert.AreEqual(3, added, "only 3 fit; the other 2 stay on the ground");
            Assert.AreEqual(10, inv[0].Count);
        }

        [Test]
        public void Gold_NeverUsesASlot()
        {
            var gold = MakeItem("Gold", 9999, ItemCategory.Currency);
            var inv = new Inventory(1);
            inv.TryAdd(MakeItem("Helm", 1), 1);

            Assert.AreEqual(50, inv.TryAdd(gold, 50));
            Assert.AreEqual(50, inv.Gold);
            Assert.IsTrue(inv[0].Item.displayName == "Helm");
        }

        [Test]
        public void Move_SameStackableItem_Merges_OverflowStays()
        {
            var gel = MakeItem("Gel", 10);
            var inv = new Inventory(3);
            inv.PlaceAt(0, new ItemStack(gel, 8));
            inv.PlaceAt(1, new ItemStack(gel, 5));

            inv.Move(0, 1);

            Assert.AreEqual(10, inv[1].Count);
            Assert.AreEqual(3, inv[0].Count);
        }

        [Test]
        public void Move_DifferentItems_Swap()
        {
            var a = MakeItem("A", 1);
            var b = MakeItem("B", 1);
            var inv = new Inventory(2);
            inv.TryAdd(a, 1);
            inv.TryAdd(b, 1);

            inv.Move(0, 1);

            Assert.AreEqual(b, inv[0].Item);
            Assert.AreEqual(a, inv[1].Item);
        }

        [Test]
        public void RemoveAt_EmptiesSlotWhenCountReachesZero()
        {
            var potion = MakeItem("Potion", 10);
            var inv = new Inventory(1);
            inv.TryAdd(potion, 2);

            inv.RemoveAt(0, 1);
            Assert.AreEqual(1, inv[0].Count);
            inv.RemoveAt(0, 1);
            Assert.IsTrue(inv[0].IsEmpty);
        }

        [Test]
        public void SpaceFor_CountsStacksAndEmptySlots()
        {
            var gel = MakeItem("Gel", 10);
            var inv = new Inventory(3);
            inv.TryAdd(gel, 4);

            Assert.AreEqual(6 + 10 + 10, inv.SpaceFor(gel));
        }
    }
}
