using UnityEngine;
using Vela.Config;
using Vela.Core;
using Vela.Gameplay;
using Vela.Items;
using Vela.Player;

namespace Vela.UI
{
    /// Inventory window (I). IMGUI placeholder, laid out like the final UI will be:
    ///  - Left: worn equipment (head, chest, hands, feet) + current weapon.
    ///  - Right: bag grid.
    ///  - Bottom: what the left / right mouse buttons do.
    /// Drag to move or equip · drag out of the window to drop · right-click to use / equip / unequip.
    /// The game keeps running while it's open.
    [DefaultExecutionOrder(-100)]
    public class InventoryUI : MonoBehaviour
    {
        private enum DragSource
        {
            None,
            Bag,
            Worn
        }

        private static readonly MouseAction[] BindingOptions =
        {
            MouseAction.BasicAttack, MouseAction.ChargedAttack, MouseAction.Skill1, MouseAction.Skill2,
            MouseAction.Skill3, MouseAction.Skill4, MouseAction.None
        };

        private bool open;
        private Rect windowRect;
        private DragSource dragSource;
        private int dragIndex = -1;
        private EquipmentSlot dragSlot;
        private GUIStyle title;
        private GUIStyle text;
        private GUIStyle small;
        private GUIStyle count;

        private string hoverTitle;
        private string hoverBody;
        private Color hoverColor;

        public bool IsOpen => open;

        private static PlayerInventory Inventory =>
            CombatRegistry.Player != null ? CombatRegistry.Player.GetComponent<PlayerInventory>() : null;

        private static PlayerCombat Combat =>
            CombatRegistry.Player != null ? CombatRegistry.Player.GetComponent<PlayerCombat>() : null;

        private void Update()
        {
            if (VelaInput.InventoryPressed) open = !open;
            if (open && VelaInput.CancelPressed) open = false;
            if (!open) dragSource = DragSource.None;

            VelaInput.UIOpen = open;

            var mouse = VelaInput.MousePosition;
            var guiMouse = new Vector2(mouse.x, Screen.height - mouse.y);
            VelaInput.PointerOverUI = open && (windowRect.Contains(guiMouse) || dragSource != DragSource.None);
        }

        private void OnDisable()
        {
            VelaInput.PointerOverUI = false;
            VelaInput.UIOpen = false;
        }

        private void EnsureStyles(float s)
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                title.normal.textColor = Color.white;
                text = new GUIStyle(GUI.skin.label) { wordWrap = true };
                text.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
                small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter };
                small.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
                count = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
                count.normal.textColor = Color.white;
            }

            title.fontSize = Mathf.RoundToInt(24 * s);
            text.fontSize = Mathf.RoundToInt(15 * s);
            small.fontSize = Mathf.RoundToInt(12 * s);
            count.fontSize = Mathf.RoundToInt(14 * s);
        }

        private void OnGUI()
        {
            if (!open) return;
            var inventory = Inventory;
            if (inventory == null) return;

            var s = Screen.height / 1080f;
            EnsureStyles(s);
            hoverTitle = null;

            var cell = 62f * s;
            var gap = 6f * s;
            const int columns = 6;
            var rows = Mathf.CeilToInt(inventory.Inventory.Capacity / (float)columns);
            var pad = 18f * s;
            var equipWidth = cell + 70f * s;
            var gridWidth = columns * cell + (columns - 1) * gap;
            var width = pad * 3f + equipWidth + gridWidth;
            var height = pad * 2f + 50f * s + Mathf.Max(rows * (cell + gap), 5 * (cell + gap)) + 120f * s;
            windowRect = new Rect(Screen.width - width - 24f * s, 110f * s, width, height);

            Fill(windowRect, new Color(0.06f, 0.07f, 0.1f, 0.92f));
            Fill(new Rect(windowRect.x, windowRect.y, windowRect.width, 3f * s), new Color(1f, 0.85f, 0.4f, 0.8f));

            var x = windowRect.x + pad;
            var y = windowRect.y + pad;
            GUI.Label(new Rect(x, y, 300f * s, 34f * s), "Inventory", title);
            var goldText = $"Gold  {inventory.Inventory.Gold}";
            GUI.Label(new Rect(windowRect.xMax - pad - 200f * s, y + 6f * s, 200f * s, 26f * s), goldText,
                new GUIStyle(text) { alignment = TextAnchor.UpperRight, normal = { textColor = new Color(1f, 0.85f, 0.3f) } });
            y += 46f * s;

            // Equipment column
            var ey = y;
            foreach (var slot in PlayerInventory.WearableSlots)
            {
                var rect = new Rect(x, ey, cell, cell);
                var item = inventory.GetEquipped(slot);
                DrawSlot(rect, item != null ? new ItemStack(item, 1) : ItemStack.Empty, slot.ToString());
                GUI.Label(new Rect(rect.xMax + 8f * s, rect.y + cell * 0.3f, 70f * s, 24f * s), slot.ToString(), text);
                HandleWornSlot(rect, slot, item, inventory);
                ey += cell + gap;
            }

            var combat = Combat;
            if (combat != null && combat.CurrentWeapon != null)
            {
                GUI.Label(new Rect(x, ey + 4f * s, equipWidth, 40f * s), $"Weapon\n{combat.CurrentWeapon.displayName}  (Tab)", text);
            }

            // Bag grid
            var gx = x + equipWidth + pad;
            for (var i = 0; i < inventory.Inventory.Capacity; i++)
            {
                var col = i % columns;
                var row = i / columns;
                var rect = new Rect(gx + col * (cell + gap), y + row * (cell + gap), cell, cell);
                DrawSlot(rect, dragSource == DragSource.Bag && dragIndex == i ? ItemStack.Empty : inventory.Inventory[i], null);
                HandleBagSlot(rect, i, inventory);
            }

            // Mouse bindings
            var by = windowRect.yMax - pad - 96f * s;
            if (combat != null)
            {
                combat.LeftMouse = BindingRow(new Rect(x, by, windowRect.width - pad * 2f, 30f * s), "Left mouse", combat.LeftMouse, combat);
                combat.RightMouse = BindingRow(new Rect(x, by + 34f * s, windowRect.width - pad * 2f, 30f * s), "Right mouse", combat.RightMouse, combat);
            }

            GUI.Label(new Rect(x, windowRect.yMax - pad - 24f * s, windowRect.width - pad * 2f, 24f * s),
                "Drag: move / equip   ·   Drag outside: drop   ·   Right-click: use / equip / unequip   ·   I / Esc: close", small);

            FinishDrag(inventory);
            DrawDragged(inventory, cell);
            DrawTooltip(s);
        }

        // ------------------------------------------------------------------ slots

        private void DrawSlot(Rect rect, ItemStack stack, string emptyLabel)
        {
            var hoveredSlot = rect.Contains(Event.current.mousePosition);
            Fill(rect, hoveredSlot ? new Color(1f, 1f, 1f, 0.14f) : new Color(1f, 1f, 1f, 0.06f));

            if (stack.IsEmpty)
            {
                if (!string.IsNullOrEmpty(emptyLabel))
                {
                    GUI.Label(new Rect(rect.x, rect.y + rect.height * 0.38f, rect.width, rect.height), emptyLabel, small);
                }
                return;
            }

            var rarity = VelaSettings.Loot.RarityColor(stack.Item.rarity);
            Border(rect, rarity, stack.Item.rarity >= Rarity.Uncommon ? 2f : 1f);
            if (stack.Item.rarity >= Rarity.Rare) Fill(rect, new Color(rarity.r, rarity.g, rarity.b, 0.12f));

            DrawIcon(rect, stack.Item);
            if (stack.Count > 1) GUI.Label(new Rect(rect.x, rect.y, rect.width - 4f, rect.height - 2f), stack.Count.ToString(), count);

            if (hoveredSlot)
            {
                hoverTitle = stack.Item.displayName;
                hoverColor = rarity;
                var action = stack.Item.IsEquipment ? "Right-click: equip" :
                    stack.Item.category == ItemCategory.Consumable ? "Right-click: use" : "";
                hoverBody = $"{stack.Item.rarity} {stack.Item.category}" +
                            (stack.Item.IsEquipment ? $" · {stack.Item.Slot}" : "") +
                            (stack.Item.healAmount > 0 ? $"\nHeals {stack.Item.healAmount} HP" : "") +
                            (string.IsNullOrEmpty(stack.Item.description) ? "" : "\n" + stack.Item.description) +
                            (string.IsNullOrEmpty(action) ? "" : "\n\n" + action);
            }
        }

        private void HandleBagSlot(Rect rect, int index, PlayerInventory inventory)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || !rect.Contains(e.mousePosition)) return;
            if (inventory.Inventory[index].IsEmpty) return;

            if (e.button == 0)
            {
                dragSource = DragSource.Bag;
                dragIndex = index;
                e.Use();
            }
            else if (e.button == 1)
            {
                inventory.UseSlot(index);
                e.Use();
            }
        }

        private void HandleWornSlot(Rect rect, EquipmentSlot slot, ItemDefinition item, PlayerInventory inventory)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || !rect.Contains(e.mousePosition) || item == null) return;

            if (e.button == 0)
            {
                dragSource = DragSource.Worn;
                dragSlot = slot;
                e.Use();
            }
            else if (e.button == 1)
            {
                inventory.Unequip(slot);
                e.Use();
            }
        }

        private void FinishDrag(PlayerInventory inventory)
        {
            var e = Event.current;
            if (dragSource == DragSource.None || e.type != EventType.MouseUp || e.button != 0) return;

            var mouse = e.mousePosition;
            var bagTarget = BagIndexAt(mouse, inventory);
            var wornTarget = WornSlotAt(mouse);

            if (dragSource == DragSource.Bag)
            {
                if (bagTarget >= 0) inventory.Inventory.Move(dragIndex, bagTarget);
                else if (wornTarget != EquipmentSlot.None)
                {
                    var item = inventory.Inventory[dragIndex].Item;
                    if (item != null && item.IsEquipment && item.Slot == wornTarget) inventory.EquipFromSlot(dragIndex);
                    else HudMessages.Show("Can't equip that there", new Color(1f, 0.45f, 0.4f));
                }
                else if (!windowRect.Contains(mouse)) inventory.DropSlot(dragIndex);
            }
            else if (dragSource == DragSource.Worn)
            {
                if (bagTarget >= 0)
                {
                    var there = inventory.Inventory[bagTarget];
                    if (there.IsEmpty) inventory.Unequip(dragSlot, bagTarget);
                    else if (there.Item.IsEquipment && there.Item.Slot == dragSlot) inventory.EquipFromSlot(bagTarget);
                    else inventory.Unequip(dragSlot);
                }
                else if (!windowRect.Contains(mouse)) inventory.DropEquipped(dragSlot);
            }

            dragSource = DragSource.None;
            dragIndex = -1;
            e.Use();
        }

        private int BagIndexAt(Vector2 mouse, PlayerInventory inventory)
        {
            var s = Screen.height / 1080f;
            var cell = 62f * s;
            var gap = 6f * s;
            var pad = 18f * s;
            var equipWidth = cell + 70f * s;
            var gx = windowRect.x + pad + equipWidth + pad;
            var gy = windowRect.y + pad + 46f * s;
            for (var i = 0; i < inventory.Inventory.Capacity; i++)
            {
                var rect = new Rect(gx + i % 6 * (cell + gap), gy + i / 6 * (cell + gap), cell, cell);
                if (rect.Contains(mouse)) return i;
            }
            return -1;
        }

        private EquipmentSlot WornSlotAt(Vector2 mouse)
        {
            var s = Screen.height / 1080f;
            var cell = 62f * s;
            var gap = 6f * s;
            var pad = 18f * s;
            var ey = windowRect.y + pad + 46f * s;
            foreach (var slot in PlayerInventory.WearableSlots)
            {
                if (new Rect(windowRect.x + pad, ey, cell, cell).Contains(mouse)) return slot;
                ey += cell + gap;
            }
            return EquipmentSlot.None;
        }

        private void DrawDragged(PlayerInventory inventory, float cell)
        {
            if (dragSource == DragSource.None) return;
            var item = dragSource == DragSource.Bag ? inventory.Inventory[dragIndex].Item : inventory.GetEquipped(dragSlot);
            if (item == null) return;
            var mouse = Event.current.mousePosition;
            DrawIcon(new Rect(mouse.x - cell * 0.4f, mouse.y - cell * 0.4f, cell * 0.8f, cell * 0.8f), item);
        }

        private void DrawTooltip(float s)
        {
            if (hoverTitle == null || dragSource != DragSource.None) return;
            var mouse = Event.current.mousePosition;
            var w = 260f * s;
            var h = 130f * s;
            var rect = new Rect(Mathf.Min(mouse.x + 18f * s, Screen.width - w - 8f), mouse.y + 18f * s, w, h);
            Fill(rect, new Color(0.02f, 0.02f, 0.04f, 0.95f));
            Border(rect, hoverColor, 1f);
            var t = new GUIStyle(title) { fontSize = Mathf.RoundToInt(17 * s), normal = { textColor = hoverColor } };
            GUI.Label(new Rect(rect.x + 10f * s, rect.y + 6f * s, w - 20f * s, 26f * s), hoverTitle, t);
            GUI.Label(new Rect(rect.x + 10f * s, rect.y + 34f * s, w - 20f * s, h - 40f * s), hoverBody, text);
        }

        private MouseAction BindingRow(Rect rect, string label, MouseAction current, PlayerCombat combat)
        {
            var s = Screen.height / 1080f;
            GUI.Label(new Rect(rect.x, rect.y + 4f * s, 130f * s, rect.height), label, text);
            var index = System.Array.IndexOf(BindingOptions, current);
            if (index < 0) index = 0;

            var buttonW = 34f * s;
            var left = new Rect(rect.x + 130f * s, rect.y, buttonW, rect.height);
            var nameRect = new Rect(left.xMax + 6f * s, rect.y, 210f * s, rect.height);
            var right = new Rect(nameRect.xMax + 6f * s, rect.y, buttonW, rect.height);

            if (GUI.Button(left, "<")) index = (index - 1 + BindingOptions.Length) % BindingOptions.Length;
            if (GUI.Button(right, ">")) index = (index + 1) % BindingOptions.Length;

            Fill(nameRect, new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(new Rect(nameRect.x + 8f * s, nameRect.y + 4f * s, nameRect.width, nameRect.height),
                ActionName(BindingOptions[index], combat), text);
            return BindingOptions[index];
        }

        public static string ActionName(MouseAction action, PlayerCombat combat)
        {
            switch (action)
            {
                case MouseAction.BasicAttack: return "Basic attack";
                case MouseAction.ChargedAttack: return "Charged attack (hold)";
                case MouseAction.None: return "Nothing";
                default:
                    var i = action - MouseAction.Skill1;
                    var skill = combat != null && i < combat.Skills.Count ? combat.Skills[i] : null;
                    return skill != null ? $"Skill {i + 1}: {skill.displayName}" : $"Skill {i + 1} (empty)";
            }
        }

        // ------------------------------------------------------------------ drawing helpers

        public static void DrawIcon(Rect rect, ItemDefinition item)
        {
            if (item == null || item.icon == null) return;
            var sprite = item.icon;
            var tex = sprite.texture;
            var r = sprite.textureRect;
            var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);

            // Keep the aspect ratio, centered, with a little padding.
            var aspect = r.width / Mathf.Max(1f, r.height);
            var inner = new Rect(rect.x + rect.width * 0.1f, rect.y + rect.height * 0.1f, rect.width * 0.8f, rect.height * 0.8f);
            var w = inner.height * aspect;
            var h = inner.height;
            if (w > inner.width)
            {
                w = inner.width;
                h = w / aspect;
            }

            var previous = GUI.color;
            GUI.color = item.equipment != null ? item.equipment.tint : Color.white;
            GUI.DrawTextureWithTexCoords(new Rect(inner.center.x - w * 0.5f, inner.center.y - h * 0.5f, w, h), tex, uv);
            GUI.color = previous;
        }

        public static void Fill(Rect r, Color c)
        {
            var previous = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        public static void Border(Rect r, Color c, float t)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }
    }
}
