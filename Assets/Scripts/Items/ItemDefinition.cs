using UnityEngine;
using Vela.Visual;

namespace Vela.Items
{
    /// One kind of item. Create with Assets ▸ Create ▸ Vela ▸ Item.
    [CreateAssetMenu(menuName = "Vela/Item", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string displayName = "Item";
        [TextArea] public string description = "";
        public Sprite icon;
        public Rarity rarity = Rarity.Common;
        public ItemCategory category = ItemCategory.Material;

        [Tooltip("How many fit in one inventory slot. 1 = not stackable.")]
        [Min(1)] public int maxStack = 99;

        [Header("Consumable")]
        public int healAmount;

        [Header("Equipment")]
        [Tooltip("What the character wears when this is equipped (slot + sprites).")]
        public EquipmentVisual equipment;

        public bool IsStackable => maxStack > 1;
        public bool IsEquipment => category == ItemCategory.Equipment && equipment != null;
        public EquipmentSlot Slot => equipment != null ? equipment.slot : EquipmentSlot.None;
    }
}
