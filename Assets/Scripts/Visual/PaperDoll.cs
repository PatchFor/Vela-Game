using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Vela.Gameplay;
using Vela.Items;

namespace Vela.Visual
{
    /// Layered character: body + hair from a CharacterRig, and one renderer per equipment slot.
    /// Runs after SpriteBillboard each frame and copies its pose (scale, squash, flash, blink,
    /// mirror), then picks each layer's sprite and draw order for the current facing.
    /// A SortingGroup keeps the whole stack sorting as one object against the 3D world.
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(SpriteBillboard))]
    public class PaperDoll : MonoBehaviour
    {
        [SerializeField] private CharacterRig rig;

        private SpriteBillboard billboard;
        private readonly Dictionary<DollLayer, SpriteRenderer> layers = new Dictionary<DollLayer, SpriteRenderer>();
        private readonly Dictionary<EquipmentSlot, EquipmentVisual> equipped = new Dictionary<EquipmentSlot, EquipmentVisual>();
        private readonly List<SpriteRenderer> visibleLayers = new List<SpriteRenderer>();

        public CharacterRig Rig
        {
            get => rig;
            set
            {
                rig = value;
                Hook();
            }
        }

        /// Renderers currently showing something (for afterimages).
        public IReadOnlyList<SpriteRenderer> VisibleLayers => visibleLayers;

        public EquipmentVisual Get(EquipmentSlot slot) => equipped.TryGetValue(slot, out var v) ? v : null;

        private void Awake()
        {
            billboard = GetComponent<SpriteBillboard>();
            Hook();
        }

        private void Hook()
        {
            if (billboard == null) billboard = GetComponent<SpriteBillboard>();
            if (billboard == null || rig == null) return;
            billboard.DirectionalSprite = direction => rig.body.Get(direction);
        }

        public void SetEquipment(EquipmentSlot slot, EquipmentVisual visual)
        {
            if (slot == EquipmentSlot.None) return;
            if (visual == null) equipped.Remove(slot);
            else equipped[slot] = visual;
        }

        private SpriteRenderer Layer(DollLayer layer)
        {
            if (layers.TryGetValue(layer, out var renderer) && renderer != null) return renderer;

            var root = billboard.SpriteRoot;
            if (root.GetComponent<SortingGroup>() == null) root.gameObject.AddComponent<SortingGroup>();

            var go = new GameObject($"Layer_{layer}");
            go.transform.SetParent(root, false);
            renderer = go.AddComponent<SpriteRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            layers[layer] = renderer;
            return renderer;
        }

        private void LateUpdate()
        {
            if (billboard == null || rig == null || billboard.Renderer == null) return;

            var body = billboard.Renderer;
            var direction = billboard.Direction;
            var order = rig.OrderFor(direction);
            var hideHair = false;
            foreach (var pair in equipped)
            {
                if (pair.Value != null && pair.Value.hidesHair) hideHair = true;
            }

            visibleLayers.Clear();
            visibleLayers.Add(body);

            for (var i = 0; i < order.Length; i++)
            {
                var layer = order[i];
                if (layer == DollLayer.Body)
                {
                    body.sortingOrder = i;
                    continue;
                }

                Sprite sprite = null;
                var tint = Color.white;

                if (layer == DollLayer.Hair)
                {
                    if (!hideHair) sprite = rig.hair.Get(direction);
                    tint = rig.hairTint;
                }
                else
                {
                    var visual = Get(SlotFor(layer));
                    if (visual != null)
                    {
                        sprite = visual.sprites.Get(direction);
                        tint = visual.tint;
                    }
                }

                var renderer = Layer(layer);
                renderer.sprite = sprite;
                renderer.enabled = sprite != null;
                if (sprite == null) continue;

                renderer.sortingOrder = i;
                renderer.flipX = body.flipX;
                renderer.sharedMaterial = body.sharedMaterial;
                // The body color carries flash / hurt tint / blink / fade. Flash is a solid color,
                // so don't tint it (the whole silhouette flashes as one).
                var flashing = body.sharedMaterial == VelaSettings.FlashMaterial;
                var c = flashing ? body.color : body.color * tint;
                c.a = body.color.a * (flashing ? 1f : tint.a);
                renderer.color = c;
                visibleLayers.Add(renderer);
            }
        }

        private static EquipmentSlot SlotFor(DollLayer layer) => layer switch
        {
            DollLayer.Head => EquipmentSlot.Head,
            DollLayer.Chest => EquipmentSlot.Chest,
            DollLayer.Hands => EquipmentSlot.Hands,
            DollLayer.Feet => EquipmentSlot.Feet,
            DollLayer.Weapon => EquipmentSlot.Weapon,
            _ => EquipmentSlot.None
        };
    }
}
