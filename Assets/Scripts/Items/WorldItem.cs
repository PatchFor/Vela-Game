using System.Collections.Generic;
using UnityEngine;
using Vela.FX;
using Vela.Gameplay;
using Vela.Player;

namespace Vela.Items
{
    /// A dropped item lying in the world.
    ///  1. Pops out of the monster in an arc, bounces once, then settles.
    ///  2. Rarity shows as color: glow ring for Uncommon+, a light pillar that gets taller with rarity.
    ///  3. Items wait to be clicked (or F). Gold is the exception: walk near it and it flies to you.
    public class WorldItem : MonoBehaviour
    {
        private enum State
        {
            Waiting,
            Popping,
            Bouncing,
            Settled,
            Collecting
        }

        private static readonly List<WorldItem> AllItems = new List<WorldItem>();

        private ItemStack stack;
        private int gold;
        private State state;
        private float timer;
        private float delay;
        private Vector3 origin;
        private Vector3 landing;
        private float spawnTime;
        private bool hovered;
        private float nudgeTime = -10f;

        private Transform spriteTransform;
        private SpriteRenderer spriteRenderer;
        private MeshRenderer glowRenderer;
        private Transform beam;
        private MeshRenderer beamRenderer;
        private Transform shadow;
        private MaterialPropertyBlock block;

        public static IReadOnlyList<WorldItem> All => AllItems;

        public ItemStack Stack => stack;
        public bool IsGold => gold > 0;
        public int Gold => gold;
        public Rarity Rarity => stack.Item != null ? stack.Item.rarity : Rarity.Common;
        public bool CanPickUp => state == State.Bouncing || state == State.Settled;

        public string Label
        {
            get
            {
                if (IsGold) return $"{gold} Gold";
                if (stack.IsEmpty) return "";
                return stack.Count > 1 ? $"{stack.Item.displayName} x{stack.Count}" : stack.Item.displayName;
            }
        }

        public Color LabelColor => IsGold ? new Color(1f, 0.85f, 0.3f) : VelaSettings.Loot.RarityColor(Rarity);

        /// World point to draw labels / measure mouse distance.
        public Vector3 LabelAnchor => (spriteTransform != null ? spriteTransform.position : transform.position) + Vector3.up * 0.45f;

        public static WorldItem Spawn(ItemStack stack, int gold, Vector3 origin, Vector3 landing, float delay)
        {
            var go = new GameObject(gold > 0 ? "Gold" : $"Item_{stack.Item?.displayName}");
            var item = go.AddComponent<WorldItem>();
            item.stack = stack;
            item.gold = gold;
            item.origin = origin;
            item.landing = landing;
            item.delay = delay;
            item.Build();
            go.transform.position = origin;
            return item;
        }

        private void OnEnable() => AllItems.Add(this);
        private void OnDisable() => AllItems.Remove(this);

        private void Build()
        {
            var loot = VelaSettings.Loot;
            block = new MaterialPropertyBlock();
            spawnTime = Time.time;

            // Shadow
            var shadowQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DestroyImmediate(shadowQuad.GetComponent<Collider>());
            shadow = shadowQuad.transform;
            shadow.SetParent(transform, false);
            shadow.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.localScale = Vector3.one * (loot.itemWorldSize * 0.6f);
            var shadowRenderer = shadowQuad.GetComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = VelaSettings.UnlitMaterial;
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            block.SetTexture("_MainTex", VelaSettings.Fx.softCircle != null ? VelaSettings.Fx.softCircle : Texture2D.whiteTexture);
            block.SetColor("_Color", new Color(0f, 0f, 0f, 0.35f));
            shadowRenderer.SetPropertyBlock(block);
            block.Clear();

            // Sprite
            var spriteGo = new GameObject("Sprite");
            spriteTransform = spriteGo.transform;
            spriteTransform.SetParent(transform, false);
            spriteRenderer = spriteGo.AddComponent<SpriteRenderer>();
            spriteRenderer.sharedMaterial = VelaSettings.UnlitMaterial;
            var itemDef = IsGold ? loot.goldItem : stack.Item;
            spriteRenderer.sprite = itemDef != null ? itemDef.icon : VelaSettings.Fx.orbSprite;
            if (!IsGold && stack.Item != null && stack.Item.equipment != null) spriteRenderer.color = stack.Item.equipment.tint;
            if (spriteRenderer.sprite != null)
            {
                var size = Mathf.Max(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y);
                spriteTransform.localScale = Vector3.one * (loot.itemWorldSize / Mathf.Max(0.01f, size));
            }

            if (IsGold) return;

            var color = loot.RarityColor(Rarity);

            // Ground glow ring for Uncommon and above
            if (Rarity >= Rarity.Uncommon)
            {
                var glow = new GameObject("Glow");
                glow.transform.SetParent(transform, false);
                glow.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                glow.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Ring;
                glowRenderer = glow.AddComponent<MeshRenderer>();
                glowRenderer.sharedMaterial = VelaSettings.AdditiveMaterial;
                glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // Light pillar, taller for rarer items
            var height = loot.BeamHeight(Rarity);
            if (height > 0f)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Beam";
                beam = quad.transform;
                beam.SetParent(transform, false);
                beam.localScale = new Vector3(0.22f + 0.04f * (int)Rarity, height, 1f);
                beam.localPosition = new Vector3(0f, height * 0.5f, 0f);
                beamRenderer = quad.GetComponent<MeshRenderer>();
                beamRenderer.sharedMaterial = VelaSettings.AdditiveMaterial;
                beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                FxManager.SetColor(beamRenderer, block, new Color(color.r, color.g, color.b, 0.35f));
            }

            SetEffectsVisible(false);
        }

        private void SetEffectsVisible(bool visible)
        {
            if (glowRenderer != null) glowRenderer.enabled = visible;
            if (beamRenderer != null) beamRenderer.enabled = visible;
        }

        public void SetHovered(bool value) => hovered = value;

        /// "Can't pick this up" wiggle (inventory full).
        public void Nudge() => nudgeTime = Time.unscaledTime;

        public void SetCount(int count)
        {
            stack.Count = Mathf.Max(1, count);
            Nudge();
        }

        /// Picked up: fly into the player and disappear.
        public void Collect()
        {
            state = State.Collecting;
            SetEffectsVisible(false);
        }

        private void Update()
        {
            var loot = VelaSettings.Loot;
            var dt = Time.deltaTime;

            switch (state)
            {
                case State.Waiting:
                    delay -= dt;
                    if (delay <= 0f) state = State.Popping;
                    PlaceSprite(0f, 0f);
                    spriteRenderer.enabled = false;
                    return;

                case State.Popping:
                {
                    spriteRenderer.enabled = true;
                    timer += dt;
                    var t = Mathf.Clamp01(timer / Mathf.Max(0.05f, loot.popDuration));
                    transform.position = Vector3.Lerp(origin, landing, t);
                    PlaceSprite(4f * loot.popHeight * t * (1f - t), t * 720f);
                    if (t >= 1f)
                    {
                        state = State.Bouncing;
                        timer = 0f;
                        OnLanded(loot);
                    }
                    break;
                }

                case State.Bouncing:
                {
                    timer += dt;
                    var t = Mathf.Clamp01(timer / 0.22f);
                    PlaceSprite(4f * loot.bounceHeight * t * (1f - t), 0f);
                    if (t >= 1f)
                    {
                        state = State.Settled;
                        SetEffectsVisible(true);
                    }
                    break;
                }

                case State.Settled:
                    UpdateSettled(loot);
                    break;

                case State.Collecting:
                    UpdateCollecting(loot);
                    break;
            }
        }

        private void OnLanded(LootConfig loot)
        {
            FxManager.Dust(transform.position, 4, new Color(0.85f, 0.82f, 0.75f, 0.7f));
            if (IsGold) return;

            if (Rarity >= Rarity.Rare)
            {
                var color = loot.RarityColor(Rarity);
                FxManager.Ring(transform.position, 0.2f, 1.2f + 0.3f * (int)Rarity, 0.35f, color);
                FxManager.HitSpark(transform.position + Vector3.up * 0.4f, Vector3.up, color, 6 + 4 * (int)Rarity);
            }
        }

        private void UpdateSettled(LootConfig loot)
        {
            var age = Time.time - spawnTime;
            var bob = 0.08f + Mathf.Sin(age * 3f) * 0.06f;

            // Wiggle when the pickup was refused.
            var nudge = Time.unscaledTime - nudgeTime < 0.3f ? Mathf.Sin((Time.unscaledTime - nudgeTime) * 60f) * 12f : 0f;
            PlaceSprite(bob, nudge);

            var scale = hovered ? 1.25f : 1f;
            spriteTransform.localScale = Vector3.one * (BaseScale(loot) * scale);

            if (glowRenderer != null)
            {
                var pulse = 0.55f + Mathf.Sin(age * 4f) * 0.15f + (hovered ? 0.3f : 0f);
                var r = loot.itemWorldSize * (0.7f + 0.1f * Mathf.Sin(age * 4f));
                glowRenderer.transform.localScale = new Vector3(r, 1f, r);
                var c = loot.RarityColor(Rarity);
                FxManager.SetColor(glowRenderer, block, new Color(c.r, c.g, c.b, pulse));
            }

            if (beam != null)
            {
                var cam = Camera.main;
                if (cam != null) beam.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            }

            if (loot.despawnSeconds > 0f && age > loot.despawnSeconds) Destroy(gameObject);

            // Gold: auto-collect when the player walks near.
            var player = CombatRegistry.Player;
            if (IsGold && player != null && player.IsAlive)
            {
                var d = player.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude <= loot.goldMagnetRadius) Collect();
            }
        }

        private void UpdateCollecting(LootConfig loot)
        {
            var player = CombatRegistry.Player;
            if (player == null)
            {
                Destroy(gameObject);
                return;
            }

            var target = player.transform.position + Vector3.up * 1f;
            var current = spriteTransform.position;
            var next = Vector3.MoveTowards(current, target, loot.goldFlySpeed * Time.deltaTime);
            spriteTransform.position = next;
            spriteTransform.localScale *= 1f - 3f * Time.deltaTime;
            if (shadow != null) shadow.gameObject.SetActive(false);

            if ((next - target).sqrMagnitude > 0.05f) return;

            if (IsGold)
            {
                var inventory = player.GetComponent<PlayerInventory>();
                if (inventory != null) inventory.Inventory.AddGold(gold);
                DamageNumbers.Spawn(player.transform.position + Vector3.up * 2f, $"+{gold} G", LabelColor, 0.85f);
            }

            Destroy(gameObject);
        }

        private float BaseScale(LootConfig loot)
        {
            var sprite = spriteRenderer.sprite;
            if (sprite == null) return 1f;
            var size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            return loot.itemWorldSize / Mathf.Max(0.01f, size);
        }

        private void PlaceSprite(float height, float spin)
        {
            var cam = Camera.main;
            var rotation = cam != null ? cam.transform.rotation : Quaternion.identity;
            spriteTransform.SetPositionAndRotation(transform.position + Vector3.up * (0.35f + height),
                rotation * Quaternion.Euler(0f, 0f, spin));
        }
    }
}
