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
    ///
    /// With an animation set on the rig (and Detail above Procedural) it plays drawn frames:
    ///  - the body clip comes from the state (idle / walk / hurt) or the attack PlayerCombat
    ///    started, and attack frames stretch to the step's windup / active / recovery;
    ///  - shirts / gloves / boots use their own sheet at the same frame index;
    ///  - hats and weapons sit on the frame's head / hand anchors (weapons turn with the blade
    ///    angle and go behind the body on frames that say so).
    /// The frame index never depends on equipment, so swapping gear mid-swing changes only the
    /// look, never the timing.
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(SpriteBillboard))]
    public class PaperDoll : MonoBehaviour
    {
        [SerializeField] private CharacterRig rig;

        private SpriteBillboard billboard;
        private readonly Dictionary<DollLayer, SpriteRenderer> layers = new Dictionary<DollLayer, SpriteRenderer>();
        private readonly Dictionary<EquipmentSlot, EquipmentVisual> equipped = new Dictionary<EquipmentSlot, EquipmentVisual>();
        private readonly List<SpriteRenderer> visibleLayers = new List<SpriteRenderer>();
        private readonly List<DollLayer> drawOrder = new List<DollLayer>();
        private readonly List<AttackPart> partsBuffer = new List<AttackPart>();
        private readonly List<int> weightsBuffer = new List<int>();

        // Animated playback
        private bool detailSet;
        private AnimationDetail detail;
        private bool attacking;
        private string attackClip;
        private float attackWindup;
        private float attackActive;
        private float attackRecovery;
        private float attackElapsed;
        private string loopClip = "idle";
        private float loopTime;
        private DollClip currentClip;
        private int currentFrame;
        private Sprite currentBody;

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

        public AnimationDetail Detail
        {
            get => detailSet ? detail : rig != null ? rig.detail : AnimationDetail.Procedural;
            set
            {
                detail = value;
                detailSet = true;
                Hook();
            }
        }

        /// True when drawn animation frames are playing (not the single-frame doll).
        public bool Animated => rig != null && rig.animationSet != null && Detail != AnimationDetail.Procedural;

        // ------------------------------------------------------------------ debug readout

        public DollClip CurrentClip => Animated ? currentClip : null;
        public int CurrentFrameIndex => currentFrame;
        public DollFrame CurrentFrameData =>
            currentClip != null && currentFrame >= 0 && currentFrame < currentClip.Count ? currentClip.frames[currentFrame] : null;
        public bool IsAttacking => attacking;
        public float AttackElapsed => attackElapsed;
        public Vector3 AttackTiming => new Vector3(attackWindup, attackActive, attackRecovery);

        /// World position of a frame anchor (pixels from the pivot), or null when not animated.
        public Vector3? AnchorWorld(Vector2 pixels)
        {
            if (!Animated || billboard == null || billboard.Renderer == null) return null;
            var local = AnchorMath.LocalPosition(pixels, rig.animationSet.pixelsPerUnit, billboard.Renderer.flipX);
            return billboard.SpriteRoot.TransformPoint(local);
        }

        // ------------------------------------------------------------------ setup

        private void Awake()
        {
            billboard = GetComponent<SpriteBillboard>();
            Hook();
        }

        private void Hook()
        {
            if (billboard == null) billboard = GetComponent<SpriteBillboard>();
            if (billboard == null || rig == null) return;

            if (Animated)
            {
                billboard.DirectionalSprite = _ => currentBody;
                billboard.ReferenceHeightUnits = rig.animationSet.characterHeightPixels * rig.animationSet.UnitsPerPixel;
            }
            else
            {
                billboard.DirectionalSprite = direction => rig.body.Get(direction);
                billboard.ReferenceHeightUnits = 0f;
                if (layers.TryGetValue(DollLayer.Smear, out var smear) && smear != null) smear.enabled = false;
            }
        }

        public void SetEquipment(EquipmentSlot slot, EquipmentVisual visual)
        {
            if (slot == EquipmentSlot.None) return;
            if (visual == null) equipped.Remove(slot);
            else equipped[slot] = visual;
        }

        /// Called by PlayerCombat when a step starts. `clip` empty → the set's fallback attack.
        public void PlayAttack(string clip, float windup, float active, float recovery)
        {
            attacking = true;
            attackClip = clip;
            attackWindup = windup;
            attackActive = active;
            attackRecovery = recovery;
            attackElapsed = 0f;
        }

        public void StopAttack() => attacking = false;

        // ------------------------------------------------------------------ frame choice

        private void Update()
        {
            if (!Animated || billboard == null) return;

            var dt = billboard.IsPaused ? 0f : Time.deltaTime;
            var set = rig.animationSet;
            var direction = billboard.Direction;

            if (attacking)
            {
                attackElapsed += dt;
                var clip = set.Find(string.IsNullOrEmpty(attackClip) ? set.fallbackAttack : attackClip, direction)
                           ?? set.Find(set.fallbackAttack, direction);
                if (clip != null && clip.Count > 0)
                {
                    partsBuffer.Clear();
                    weightsBuffer.Clear();
                    foreach (var f in clip.frames)
                    {
                        partsBuffer.Add(f.part);
                        weightsBuffer.Add(f.weight);
                    }
                    currentClip = clip;
                    currentFrame = PhaseTimeline.FrameAt(partsBuffer, weightsBuffer, attackElapsed,
                        attackWindup, attackActive, attackRecovery, Detail);
                    currentBody = set.body.Get(clip.name, clip.direction, currentFrame);
                    return;
                }
            }

            var wanted = billboard.State switch
            {
                VisualState.Move => "walk",
                VisualState.Hurt => "hurt",
                _ => "idle"
            };
            if (wanted != loopClip)
            {
                loopClip = wanted;
                loopTime = 0f;
            }
            loopTime += dt;

            var loop = set.Find(loopClip, direction) ?? set.Find("idle", direction);
            if (loop == null || loop.Count == 0)
            {
                currentClip = null;
                currentBody = null;
                return;
            }
            currentClip = loop;
            currentFrame = PhaseTimeline.LoopFrame(loop.Count, loop.framesPerSecond, loopTime, loop.loop);
            currentBody = set.body.Get(loop.name, loop.direction, currentFrame);
        }

        // ------------------------------------------------------------------ drawing

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

        private bool HideHair()
        {
            foreach (var pair in equipped)
            {
                if (pair.Value != null && pair.Value.hidesHair) return true;
            }
            return false;
        }

        private void LateUpdate()
        {
            if (billboard == null || rig == null || billboard.Renderer == null) return;
            if (Animated) DrawAnimated();
            else DrawSingleFrame();
        }

        private void DrawSingleFrame()
        {
            var body = billboard.Renderer;
            var direction = billboard.Direction;
            var order = rig.OrderFor(direction);
            var hideHair = HideHair();

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
                ResetPlacement(renderer);
                Show(renderer, sprite, tint, i, body);
            }
        }

        private void DrawAnimated()
        {
            var body = billboard.Renderer;
            var set = rig.animationSet;
            var clip = currentClip;
            var frame = CurrentFrameData;
            var direction = billboard.Direction;
            var mirrored = body.flipX;
            var clipName = clip != null ? clip.name : "idle";
            var clipDirection = clip != null ? clip.direction : direction;

            // Back → front. The weapon goes in front of or behind the body per frame.
            var weaponInFront = frame == null || frame.weaponInFront;
            drawOrder.Clear();
            if (!weaponInFront) drawOrder.Add(DollLayer.Weapon);
            drawOrder.Add(DollLayer.Body);
            drawOrder.Add(DollLayer.Feet);
            drawOrder.Add(DollLayer.Chest);
            drawOrder.Add(DollLayer.Hair);
            drawOrder.Add(DollLayer.Head);
            if (weaponInFront) drawOrder.Add(DollLayer.Weapon);
            drawOrder.Add(DollLayer.Hands);
            drawOrder.Add(DollLayer.Smear);

            visibleLayers.Clear();
            visibleLayers.Add(body);
            var hideHair = HideHair();

            for (var i = 0; i < drawOrder.Count; i++)
            {
                var layer = drawOrder[i];
                if (layer == DollLayer.Body)
                {
                    body.sortingOrder = i;
                    continue;
                }

                var renderer = Layer(layer);
                ResetPlacement(renderer);
                Sprite sprite = null;
                var tint = Color.white;

                switch (layer)
                {
                    case DollLayer.Hair:
                        if (!hideHair) sprite = set.hair.Get(clipName, clipDirection, currentFrame);
                        tint = rig.hairTint;
                        break;

                    case DollLayer.Smear:
                        sprite = set.smear.Get(clipName, clipDirection, currentFrame);
                        break;

                    case DollLayer.Head:
                    {
                        var visual = Get(EquipmentSlot.Head);
                        if (visual != null && frame != null)
                        {
                            sprite = visual.anchored.Get(direction);
                            tint = visual.tint;
                            renderer.transform.localPosition =
                                AnchorMath.LocalPosition(frame.head + visual.anchorOffset, set.pixelsPerUnit, mirrored);
                        }
                        break;
                    }

                    case DollLayer.Weapon:
                    {
                        var visual = Get(EquipmentSlot.Weapon);
                        if (visual != null && frame != null)
                        {
                            // One weapon drawing for every facing: it's placed and turned by the hand anchor.
                            sprite = visual.anchored.side != null ? visual.anchored.side : visual.anchored.Get(direction);
                            tint = visual.tint;
                            renderer.transform.localPosition =
                                AnchorMath.LocalPosition(frame.hand + visual.anchorOffset, set.pixelsPerUnit, mirrored);
                            renderer.transform.localRotation =
                                Quaternion.Euler(0f, 0f, AnchorMath.Angle(frame.hand, frame.tip, mirrored));
                        }
                        break;
                    }

                    default:
                    {
                        var visual = Get(SlotFor(layer));
                        if (visual != null && visual.frames != null)
                        {
                            sprite = visual.frames.Get(clipName, clipDirection, currentFrame);
                            tint = visual.tint;
                        }
                        break;
                    }
                }

                Show(renderer, sprite, tint, i, body);
                if (layer == DollLayer.Smear && sprite != null)
                {
                    // Smear is light, not paint: additive, and it ignores hurt tint / flash.
                    renderer.sharedMaterial = VelaSettings.AdditiveMaterial;
                    var c = rig.smearColor;
                    c.a *= body.color.a;
                    renderer.color = c;
                }
            }
        }

        private static void ResetPlacement(SpriteRenderer renderer)
        {
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localRotation = Quaternion.identity;
        }

        private void Show(SpriteRenderer renderer, Sprite sprite, Color tint, int order, SpriteRenderer body)
        {
            renderer.sprite = sprite;
            renderer.enabled = sprite != null;
            if (sprite == null) return;

            renderer.sortingOrder = order;
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
