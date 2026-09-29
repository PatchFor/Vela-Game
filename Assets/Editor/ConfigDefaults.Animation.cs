using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vela.Config;
using Vela.Items;
using Vela.Visual;

namespace Vela.EditorTools
{
    /// The 64×64 animated doll: builds the placeholder mannequin animation set, then gives every
    /// existing equipment visual its animated art:
    ///  - hats and weapons get anchored sprites;
    ///  - shirts, gloves and boots get per-frame sheets.
    /// Items keep working in both the old single-frame doll and the animated one.
    /// Same policy as the rest of ConfigDefaults: only empty fields are filled, so an artist's
    /// real sheets or anchors are never replaced (Overwrite rebuilds everything).
    public static partial class ConfigDefaults
    {
        public const string MannequinPath = Root + "/Characters/MannequinAnimation.asset";

        public class AnimatedGear
        {
            public EquipmentVisual Hood, Helm, Crown;
            public EquipmentVisual Vest, Plate, WardenPlate;
            public EquipmentVisual Gloves, EmberGloves;
            public EquipmentVisual Boots, Greaves;
            public EquipmentVisual Sword, Bow, Greatsword;
        }

        /// Returns the sword looks for the F7 test key: long (the Sword's own), short, broad.
        private static EquipmentVisual[] BuildAnimatedDoll(CharacterRig rig, AnimatedGear gear)
        {
            var set = AssetDatabase.LoadAssetAtPath<DollAnimationSet>(MannequinPath);
            if (set == null || Overwrite || set.clips == null || set.clips.Length == 0) set = GenerateMannequin(set);

            if (rig.animationSet == null || Overwrite)
            {
                rig.animationSet = set;
                EditorUtility.SetDirty(rig);
            }

            var sprites = SpritesByLayer(set);
            Garment(gear.Vest, set, sprites, PlaceholderArt.DollArtLayer.Vest);
            Garment(gear.Plate, set, sprites, PlaceholderArt.DollArtLayer.Plate);
            Garment(gear.WardenPlate, set, sprites, PlaceholderArt.DollArtLayer.Plate);
            Garment(gear.Gloves, set, sprites, PlaceholderArt.DollArtLayer.Gloves);
            Garment(gear.EmberGloves, set, sprites, PlaceholderArt.DollArtLayer.Gloves);
            Garment(gear.Boots, set, sprites, PlaceholderArt.DollArtLayer.Boots);
            Garment(gear.Greaves, set, sprites, PlaceholderArt.DollArtLayer.Greaves);

            Anchored(gear.Hood, PlaceholderArt.HoodAnchored(), new Vector2(0f, -6f));
            Anchored(gear.Helm, PlaceholderArt.HelmAnchored(), new Vector2(0f, -2f));
            Anchored(gear.Crown, PlaceholderArt.CrownAnchored(), new Vector2(0f, 3f));
            Anchored(gear.Sword, Single(PlaceholderArt.SwordLongGrip()), Vector2.zero);
            Anchored(gear.Bow, Single(PlaceholderArt.BowGrip()), Vector2.zero);
            Anchored(gear.Greatsword, Single(PlaceholderArt.GreatswordGrip()), Vector2.zero);

            var shortSword = WeaponLook("SwordShort", PlaceholderArt.SwordShortGrip());
            var broadSword = WeaponLook("SwordBroad", PlaceholderArt.SwordBroadGrip());
            return new[] { gear.Sword, shortSword, broadSword };
        }

        /// Sword combo → clip names in the animation set (only fills empty ones).
        private static void FillSwordAnimations(WeaponConfig sword)
        {
            if (sword == null) return;
            var names = new[] { "slash", "backslash", "thrust" };
            var changed = false;
            for (var i = 0; i < sword.combo.Length && i < names.Length; i++)
            {
                if (!string.IsNullOrEmpty(sword.combo[i].animation) && !Overwrite) continue;
                sword.combo[i].animation = names[i];
                changed = true;
            }
            if (sword.chargedAttack != null && (string.IsNullOrEmpty(sword.chargedAttack.animation) || Overwrite))
            {
                sword.chargedAttack.animation = "dash_strike";
                changed = true;
            }
            if (changed) EditorUtility.SetDirty(sword);
        }

        // ------------------------------------------------------------------ generation

        private static DollAnimationSet GenerateMannequin(DollAnimationSet existing)
        {
            var art = PlaceholderArt.MannequinAtlases();

            var set = existing;
            if (set == null)
            {
                PlaceholderArt.EnsureFolder(System.IO.Path.GetDirectoryName(MannequinPath)?.Replace('\\', '/'));
                set = ScriptableObject.CreateInstance<DollAnimationSet>();
                AssetDatabase.CreateAsset(set, MannequinPath);
            }
            else
            {
                foreach (var old in AssetDatabase.LoadAllAssetRepresentationsAtPath(MannequinPath))
                {
                    if (old is Sprite) Object.DestroyImmediate(old, true);
                }
            }

            set.canvasPixels = PlaceholderArt.DollCanvas;
            set.characterHeightPixels = 44f;
            set.pixelsPerUnit = 16f;
            set.fallbackAttack = "slash";

            // Clips: one per name × facing, frames in cell order.
            var clips = new List<DollClip>();
            foreach (var clip in PlaceholderArt.MannequinClips)
            foreach (var dir in PlaceholderArt.DollDirections)
            {
                var frames = new List<DollFrame>();
                foreach (var cell in art.Cells)
                {
                    if (cell.Clip == clip.Name && cell.Direction == dir) frames.Add(cell.Data);
                }
                clips.Add(new DollClip
                {
                    name = clip.Name,
                    direction = dir,
                    isAttack = clip.Attack,
                    loop = clip.Loop,
                    framesPerSecond = clip.Attack ? 12f : clip.Fps,
                    frames = frames.ToArray()
                });
            }
            set.clips = clips.ToArray();

            // One sprite per non-empty cell per layer, stored inside the set asset.
            foreach (var pair in art.Atlases)
            {
                foreach (var cell in art.Cells)
                {
                    if (art.Empty[pair.Key].Contains(cell.Cell)) continue;
                    var sprite = Sprite.Create(pair.Value, art.CellRect(cell.Cell), new Vector2(0.5f, 0f),
                        set.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    sprite.name = SpriteName(pair.Key, cell.Clip, cell.Direction, cell.Frame);
                    AssetDatabase.AddObjectToAsset(sprite, set);
                }
            }

            AssetDatabase.SaveAssets();
            var sprites = SpritesByLayer(set);
            set.body = Sheet(set, sprites, PlaceholderArt.DollArtLayer.Body);
            set.hair = Sheet(set, sprites, PlaceholderArt.DollArtLayer.Hair);
            set.smear = Sheet(set, sprites, PlaceholderArt.DollArtLayer.Smear);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();

            var errors = DollAnimationValidator.Validate(set);
            foreach (var error in errors) Debug.LogWarning($"[Vela] Mannequin animation: {error}");
            return set;
        }

        private static string SpriteName(PlaceholderArt.DollArtLayer layer, string clip, Direction4 dir, int frame) =>
            $"{layer}|{clip}|{dir}|{frame}";

        /// Sprites inside the set asset, keyed by layer, then by "clip|dir|frame".
        private static Dictionary<string, Dictionary<string, Sprite>> SpritesByLayer(DollAnimationSet set)
        {
            var result = new Dictionary<string, Dictionary<string, Sprite>>();
            foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(set)))
            {
                if (!(obj is Sprite sprite)) continue;
                var split = sprite.name.IndexOf('|');
                if (split <= 0) continue;
                var layer = sprite.name.Substring(0, split);
                if (!result.TryGetValue(layer, out var map)) result[layer] = map = new Dictionary<string, Sprite>();
                map[sprite.name.Substring(split + 1)] = sprite;
            }
            return result;
        }

        /// A layer sheet with the same frame count as each body clip (missing cells stay empty).
        private static DollLayerSheet Sheet(DollAnimationSet set, Dictionary<string, Dictionary<string, Sprite>> sprites,
            PlaceholderArt.DollArtLayer layer)
        {
            sprites.TryGetValue(layer.ToString(), out var map);
            var clips = new List<DollLayerFrames>();
            foreach (var clip in set.clips)
            {
                var frames = new Sprite[clip.Count];
                var any = false;
                for (var i = 0; i < frames.Length; i++)
                {
                    if (map != null && map.TryGetValue($"{clip.name}|{clip.direction}|{i}", out var s))
                    {
                        frames[i] = s;
                        any = true;
                    }
                }
                if (any) clips.Add(new DollLayerFrames { clip = clip.name, direction = clip.direction, frames = frames });
            }
            return new DollLayerSheet { clips = clips.ToArray() };
        }

        private static void Garment(EquipmentVisual visual, DollAnimationSet set,
            Dictionary<string, Dictionary<string, Sprite>> sprites, PlaceholderArt.DollArtLayer layer)
        {
            if (visual == null || (visual.frames != null && !visual.frames.IsEmpty && !Overwrite)) return;
            visual.frames = Sheet(set, sprites, layer);
            EditorUtility.SetDirty(visual);
        }

        private static void Anchored(EquipmentVisual visual, DirectionalSprites sprites, Vector2 offset)
        {
            if (visual == null || (visual.anchored != null && !visual.anchored.IsEmpty && !Overwrite)) return;
            visual.anchored = sprites;
            visual.anchorOffset = offset;
            EditorUtility.SetDirty(visual);
        }

        private static DirectionalSprites Single(Sprite sprite) => new DirectionalSprites { down = sprite, up = sprite, side = sprite };

        /// An extra weapon look (anchored art only) for the F7 skin test.
        private static EquipmentVisual WeaponLook(string name, Sprite grip)
        {
            var visual = GetOrCreate<EquipmentVisual>($"{Root}/Equipment/{name}Visual.asset", v =>
            {
                v.slot = EquipmentSlot.Weapon;
                v.tint = Color.white;
            });
            Anchored(visual, Single(grip), Vector2.zero);
            return visual;
        }
    }
}
