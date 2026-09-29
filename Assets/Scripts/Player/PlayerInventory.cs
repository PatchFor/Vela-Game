using System;
using System.Collections.Generic;
using UnityEngine;
using Vela.Core;
using Vela.FX;
using Vela.Gameplay;
using Vela.Items;
using Vela.Visual;

namespace Vela.Player
{
    /// The player's bag and worn equipment. Equipping updates the paper doll immediately.
    /// Rules (each has a HUD message when it fails):
    ///  - Equip: the old piece goes back into the slot the new one came from.
    ///  - Unequip: needs a free slot ("Inventory full").
    ///  - Use potion: refused at full HP.
    ///  - Drop: the stack is thrown on the ground next to the player.
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInventory : MonoBehaviour
    {
        private static readonly Color Warning = new Color(1f, 0.45f, 0.4f);

        private readonly Dictionary<EquipmentSlot, ItemDefinition> equipped = new Dictionary<EquipmentSlot, ItemDefinition>();
        private PlayerController player;
        private PaperDoll paperDoll;

        public Inventory Inventory { get; private set; }

        public event Action EquipmentChanged;

        public static readonly EquipmentSlot[] WearableSlots =
            { EquipmentSlot.Head, EquipmentSlot.Chest, EquipmentSlot.Hands, EquipmentSlot.Feet };

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            paperDoll = GetComponent<PaperDoll>();
            Inventory = new Inventory(VelaSettings.Loot.inventorySize);
        }

        private void Start()
        {
            var config = player.Config;
            if (config == null) return;

            if (config.startingEquipment != null)
            {
                foreach (var item in config.startingEquipment)
                {
                    if (item != null && item.IsEquipment) SetEquipped(item.Slot, item);
                }
            }

            if (config.startingItems != null)
            {
                foreach (var item in config.startingItems)
                {
                    if (item != null) Inventory.TryAdd(item, 1);
                }
            }
        }

        public ItemDefinition GetEquipped(EquipmentSlot slot) => equipped.TryGetValue(slot, out var item) ? item : null;

        private void SetEquipped(EquipmentSlot slot, ItemDefinition item)
        {
            if (item == null) equipped.Remove(slot);
            else equipped[slot] = item;

            if (paperDoll != null) paperDoll.SetEquipment(slot, item != null ? item.equipment : null);
            EquipmentChanged?.Invoke();
        }

        /// Equip the item in an inventory slot (swapping out what's worn).
        public bool EquipFromSlot(int index)
        {
            var stack = Inventory[index];
            if (stack.IsEmpty || !stack.Item.IsEquipment) return false;

            var slot = stack.Item.Slot;
            var previous = GetEquipped(slot);
            var taken = Inventory.RemoveAt(index, 1);

            if (previous != null && !Inventory.PlaceAt(index, new ItemStack(previous, 1)) &&
                Inventory.TryAdd(previous, 1) == 0)
            {
                // No room for the old piece: undo.
                Inventory.TryAdd(taken.Item, 1);
                HudMessages.Show("Inventory full", Warning);
                return false;
            }

            SetEquipped(slot, taken.Item);
            FxManager.Ring(transform.position, 0.3f, 1.4f, 0.25f, VelaSettings.Loot.RarityColor(taken.Item.rarity));
            if (player.Billboard != null) player.Billboard.Punch(new Vector2(0.85f, 1.2f));
            return true;
        }

        public bool Unequip(EquipmentSlot slot, int targetIndex = -1)
        {
            var item = GetEquipped(slot);
            if (item == null) return false;

            var placed = targetIndex >= 0 ? Inventory.PlaceAt(targetIndex, new ItemStack(item, 1)) : Inventory.TryAdd(item, 1) > 0;
            if (!placed)
            {
                HudMessages.Show("Inventory full", Warning);
                return false;
            }

            SetEquipped(slot, null);
            return true;
        }

        /// Right-click: equip equipment, drink potions.
        public bool UseSlot(int index)
        {
            var stack = Inventory[index];
            if (stack.IsEmpty) return false;

            if (stack.Item.IsEquipment) return EquipFromSlot(index);

            if (stack.Item.category == ItemCategory.Consumable && stack.Item.healAmount > 0)
            {
                var health = player.Health;
                if (health.Current >= health.Max)
                {
                    HudMessages.Show("HP is already full", new Color(1f, 1f, 1f, 0.8f));
                    return false;
                }

                health.Heal(stack.Item.healAmount);
                Inventory.RemoveAt(index, 1);
                FxManager.Ring(transform.position, 0.3f, 1.2f, 0.3f, VelaSettings.Feel.healColor);
                return true;
            }

            HudMessages.Show($"{stack.Item.displayName} can't be used", new Color(1f, 1f, 1f, 0.8f));
            return false;
        }

        public void DropSlot(int index)
        {
            var stack = Inventory.RemoveAt(index);
            if (stack.IsEmpty) return;
            LootSpawner.DropStack(stack, transform.position);
            HudMessages.Show($"Dropped {stack.Item.displayName}", new Color(1f, 1f, 1f, 0.7f));
        }

        public void DropEquipped(EquipmentSlot slot)
        {
            var item = GetEquipped(slot);
            if (item == null) return;
            SetEquipped(slot, null);
            LootSpawner.DropStack(new ItemStack(item, 1), transform.position);
        }

        /// Debug (O): wear a random piece for every slot from the given pool.
        public void RandomOutfit(IReadOnlyList<ItemDefinition> pool)
        {
            if (pool == null || pool.Count == 0) return;
            foreach (var slot in WearableSlots)
            {
                var options = new List<ItemDefinition>();
                foreach (var item in pool) if (item != null && item.Slot == slot) options.Add(item);
                options.Add(null);
                SetEquipped(slot, options[UnityEngine.Random.Range(0, options.Count)]);
            }
            FxManager.Ring(transform.position, 0.3f, 1.6f, 0.3f, Color.white);
        }
    }
}
