using UnityEngine;

namespace Vela.Items
{
    /// How loot looks and behaves once it's on the ground, plus inventory rules.
    [CreateAssetMenu(menuName = "Vela/Loot Config", fileName = "LootConfig")]
    public class LootConfig : ScriptableObject
    {
        [Tooltip("The gold item (icon + name). Gold never uses an inventory slot.")]
        public ItemDefinition goldItem;

        [Header("Rarity colors (Common → Legendary)")]
        public Color[] rarityColors =
        {
            new Color(0.85f, 0.85f, 0.85f),
            new Color(0.4f, 0.9f, 0.4f),
            new Color(0.35f, 0.6f, 1f),
            new Color(0.75f, 0.4f, 1f),
            new Color(1f, 0.65f, 0.15f)
        };

        [Tooltip("Height of the light pillar over a dropped item, per rarity. 0 = no pillar.")]
        public float[] beamHeights = { 0f, 0.8f, 1.6f, 2.6f, 4f };

        [Header("Drop animation")]
        [Tooltip("How far items scatter from the monster.")]
        public Vector2 scatterDistance = new Vector2(0.8f, 2.2f);
        public float popHeight = 1.6f;
        public float popDuration = 0.45f;
        [Tooltip("Small second hop when the item lands.")]
        public float bounceHeight = 0.3f;
        [Tooltip("Rare+ drops wait this long per rarity tier, so the good stuff lands last.")]
        public float rarityDelay = 0.08f;
        public float itemWorldSize = 0.7f;

        [Header("Pickup")]
        [Tooltip("Click-to-pick works from this far. Further away, the player walks there first.")]
        public float pickupRange = 2f;
        [Tooltip("Gold is collected automatically inside this radius.")]
        public float goldMagnetRadius = 1.4f;
        public float goldFlySpeed = 14f;
        [Tooltip("Screen-space radius (px at 1080p) for hovering items with the mouse.")]
        public float hoverRadiusPixels = 42f;
        [Tooltip("Dropped items vanish after this many seconds. 0 = never.")]
        public float despawnSeconds = 0f;

        [Header("Inventory")]
        public int inventorySize = 24;

        public Color RarityColor(Rarity rarity)
        {
            var i = (int)rarity;
            return rarityColors != null && i < rarityColors.Length ? rarityColors[i] : Color.white;
        }

        public float BeamHeight(Rarity rarity)
        {
            var i = (int)rarity;
            return beamHeights != null && i < beamHeights.Length ? beamHeights[i] : 0f;
        }
    }
}
