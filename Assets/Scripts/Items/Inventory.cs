using System;
using UnityEngine;

namespace Vela.Items
{
    /// Plain C# inventory (no MonoBehaviour) so every rule is unit-testable.
    ///  - Adding fills existing stacks first, then empty slots, left to right.
    ///  - If it doesn't all fit, the rest is reported back (the caller leaves it on the ground).
    ///  - Gold is a separate counter and never uses a slot.
    public class Inventory
    {
        private readonly ItemStack[] slots;

        public event Action Changed;

        public Inventory(int capacity)
        {
            slots = new ItemStack[Mathf.Max(1, capacity)];
        }

        public int Capacity => slots.Length;
        public int Gold { get; private set; }

        public ItemStack this[int index] => slots[index];

        public int FreeSlots
        {
            get
            {
                var free = 0;
                foreach (var s in slots) if (s.IsEmpty) free++;
                return free;
            }
        }

        public bool IsFull => FreeSlots == 0;

        /// How many of `item` could be added right now.
        public int SpaceFor(ItemDefinition item)
        {
            if (item == null) return 0;
            var space = 0;
            foreach (var s in slots)
            {
                if (s.IsEmpty) space += item.maxStack;
                else if (s.Item == item) space += Mathf.Max(0, item.maxStack - s.Count);
            }
            return space;
        }

        /// Adds up to `count`. Returns how many were actually added (0 if full).
        public int TryAdd(ItemDefinition item, int count)
        {
            if (item == null || count <= 0) return 0;

            if (item.category == ItemCategory.Currency)
            {
                AddGold(count);
                return count;
            }

            var remaining = count;

            // 1) Top up existing stacks.
            if (item.IsStackable)
            {
                for (var i = 0; i < slots.Length && remaining > 0; i++)
                {
                    if (slots[i].IsEmpty || slots[i].Item != item) continue;
                    var room = item.maxStack - slots[i].Count;
                    if (room <= 0) continue;
                    var moved = Mathf.Min(room, remaining);
                    slots[i].Count += moved;
                    remaining -= moved;
                }
            }

            // 2) Fill empty slots.
            for (var i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty) continue;
                var moved = Mathf.Min(item.maxStack, remaining);
                slots[i] = new ItemStack(item, moved);
                remaining -= moved;
            }

            var added = count - remaining;
            if (added > 0) Changed?.Invoke();
            return added;
        }

        /// Removes up to `count` from one slot. Returns what was removed.
        public ItemStack RemoveAt(int index, int count = int.MaxValue)
        {
            if (!InRange(index) || slots[index].IsEmpty || count <= 0) return ItemStack.Empty;

            var taken = Mathf.Min(count, slots[index].Count);
            var result = new ItemStack(slots[index].Item, taken);
            slots[index].Count -= taken;
            if (slots[index].Count <= 0) slots[index] = ItemStack.Empty;
            Changed?.Invoke();
            return result;
        }

        /// Puts a stack into a specific empty slot. Returns false if the slot is taken.
        public bool PlaceAt(int index, ItemStack stack)
        {
            if (!InRange(index) || stack.IsEmpty || !slots[index].IsEmpty) return false;
            slots[index] = stack;
            Changed?.Invoke();
            return true;
        }

        /// Drag-and-drop between slots: same stackable item merges (overflow stays behind),
        /// otherwise the two slots swap.
        public void Move(int from, int to)
        {
            if (!InRange(from) || !InRange(to) || from == to || slots[from].IsEmpty) return;

            var a = slots[from];
            var b = slots[to];

            if (!b.IsEmpty && a.Item == b.Item && a.Item.IsStackable)
            {
                var room = a.Item.maxStack - b.Count;
                var moved = Mathf.Min(room, a.Count);
                slots[to].Count += moved;
                slots[from].Count -= moved;
                if (slots[from].Count <= 0) slots[from] = ItemStack.Empty;
            }
            else
            {
                slots[from] = b;
                slots[to] = a;
            }

            Changed?.Invoke();
        }

        public int FirstEmptySlot()
        {
            for (var i = 0; i < slots.Length; i++) if (slots[i].IsEmpty) return i;
            return -1;
        }

        public int Count(ItemDefinition item)
        {
            var total = 0;
            foreach (var s in slots) if (!s.IsEmpty && s.Item == item) total += s.Count;
            return total;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            Changed?.Invoke();
        }

        public bool SpendGold(int amount)
        {
            if (amount < 0 || amount > Gold) return false;
            Gold -= amount;
            Changed?.Invoke();
            return true;
        }

        private bool InRange(int index) => index >= 0 && index < slots.Length;
    }
}
