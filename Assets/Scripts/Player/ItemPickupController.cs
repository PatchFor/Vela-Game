using UnityEngine;
using Vela.Core;
using Vela.Gameplay;
using Vela.Items;

namespace Vela.Player
{
    /// Picking things up.
    ///  - Hover an item with the mouse: it grows and shows its name.
    ///  - Click it: picked up if in range, otherwise the player walks over and picks it up.
    ///  - F: pick up the nearest item in range.
    ///  - Gold is never clicked: it flies to you when you walk near (see WorldItem).
    /// Runs first so a click on an item never also becomes an attack.
    [DefaultExecutionOrder(-30)]
    [RequireComponent(typeof(PlayerController), typeof(PlayerInventory))]
    public class ItemPickupController : MonoBehaviour
    {
        private static readonly Color Warning = new Color(1f, 0.45f, 0.4f);

        private PlayerController player;
        private PlayerInventory inventory;
        private PlayerInputReader input;

        public WorldItem Hovered { get; private set; }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            inventory = GetComponent<PlayerInventory>();
            input = PlayerInputReader.For(gameObject);
        }

        private void Update()
        {
            var previous = Hovered;
            Hovered = VelaInput.PointerOverUI || !player.IsAlive ? null : PickUnderPointer();
            if (previous != null && previous != Hovered) previous.SetHovered(false);
            if (Hovered != null) Hovered.SetHovered(true);

            if (!player.IsAlive) return;

            if (Hovered != null && input.Current.LeftDown)
            {
                input.ConsumeLeftClick();
                RequestPickUp(Hovered);
            }

            if (input.Current.PickUp)
            {
                var nearest = Nearest(VelaSettings.Loot.pickupRange);
                if (nearest != null) PickUp(nearest);
                else HudMessages.Show("Nothing to pick up", new Color(1f, 1f, 1f, 0.6f), 1f);
            }
        }

        private void RequestPickUp(WorldItem item)
        {
            var range = VelaSettings.Loot.pickupRange;
            if (Distance(item) <= range)
            {
                PickUp(item);
                return;
            }

            player.WalkTo(item.transform.position, range * 0.7f, () =>
            {
                if (item != null) PickUp(item);
            });
        }

        /// Tries to put the item in the bag. Handles full and partially-full bags.
        public void PickUp(WorldItem item)
        {
            if (item == null || !item.CanPickUp || item.IsGold || !item.CanBeTakenBy(gameObject)) return;

            var stack = item.Stack;
            var added = inventory.Inventory.TryAdd(stack.Item, stack.Count);

            if (added <= 0)
            {
                HudMessages.Show("Inventory full", Warning);
                item.Nudge();
                Audio.Sfx.Play(Audio.SfxEvent.InventoryFull);
                return;
            }

            var color = VelaSettings.Loot.RarityColor(stack.Item.rarity);
            if (added < stack.Count)
            {
                item.SetCount(stack.Count - added);
                HudMessages.Show($"Inventory full — picked up {added}/{stack.Count} {stack.Item.displayName}", Warning);
                return;
            }

            item.Collect();
            Audio.Sfx.Play(stack.Item.rarity >= Rarity.Rare ? Audio.SfxEvent.PickupRare : Audio.SfxEvent.Pickup);
            HudMessages.Show(stack.Count > 1 ? $"+ {stack.Item.displayName} x{stack.Count}" : $"+ {stack.Item.displayName}", color);
        }

        private WorldItem PickUnderPointer()
        {
            var cam = Camera.main;
            if (cam == null) return null;

            var mouse = VelaInput.MousePosition;
            var radius = VelaSettings.Loot.hoverRadiusPixels * Screen.height / 1080f;
            WorldItem best = null;
            var bestDistance = radius;

            foreach (var item in WorldItem.All)
            {
                if (item == null || item.IsGold || !item.CanPickUp) continue;
                var screen = cam.WorldToScreenPoint(item.LabelAnchor);
                if (screen.z < 0f) continue;
                var d = Vector2.Distance(mouse, screen);
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = item;
            }

            return best;
        }

        private WorldItem Nearest(float range)
        {
            WorldItem best = null;
            var bestDistance = range;
            foreach (var item in WorldItem.All)
            {
                if (item == null || item.IsGold || !item.CanPickUp) continue;
                var d = Distance(item);
                if (d > bestDistance) continue;
                bestDistance = d;
                best = item;
            }
            return best;
        }

        private float Distance(WorldItem item)
        {
            var d = item.transform.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }
    }
}
