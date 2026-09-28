using System;

namespace Vela.Items
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum ItemCategory
    {
        /// Gold. Never takes an inventory slot; picked up by walking over it.
        Currency,
        Consumable,
        Material,
        Equipment
    }

    /// Which paper-doll layer an equipment item draws on.
    public enum EquipmentSlot
    {
        None,
        Head,
        Chest,
        Hands,
        Feet,
        Weapon
    }

    /// An item and a count. `Item == null` means an empty slot.
    [Serializable]
    public struct ItemStack
    {
        public ItemDefinition Item;
        public int Count;

        public ItemStack(ItemDefinition item, int count)
        {
            Item = item;
            Count = count;
        }

        public bool IsEmpty => Item == null || Count <= 0;

        public static ItemStack Empty => new ItemStack(null, 0);

        public override string ToString() => IsEmpty ? "(empty)" : $"{Item.displayName} x{Count}";
    }
}
