using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vela.Config;
using Vela.Core;
using Vela.Items;
using Vela.Visual;

namespace Vela.EditorTools
{
    /// Paper-doll rig, equipment, items, loot tables and skills.
    /// Same policy as the rest of ConfigDefaults: existing assets are kept; only empty fields on
    /// existing assets are filled in, so upgrading a project doesn't overwrite your tuning.
    public static partial class ConfigDefaults
    {
        private static void CreateGameplayContent(Set set)
        {
            // ---------------- paper doll
            var rig = GetOrCreate<CharacterRig>($"{Root}/Characters/PlayerRig.asset", r => { });
            if (rig.body.IsEmpty || Overwrite)
            {
                rig.body = PlaceholderArt.BodySprites();
                rig.hair = PlaceholderArt.HairSprites();
                rig.hairTint = new Color(0.45f, 0.3f, 0.22f);
                EditorUtility.SetDirty(rig);
            }

            var hood = Visual("Hood", EquipmentSlot.Head, PlaceholderArt.HoodSprites(), new Color(0.62f, 0.45f, 0.3f), true);
            var helm = Visual("IronHelm", EquipmentSlot.Head, PlaceholderArt.HelmSprites(), new Color(0.78f, 0.82f, 0.9f), true);
            var crown = Visual("WardenCrown", EquipmentSlot.Head, PlaceholderArt.CrownSprites(), new Color(1f, 0.8f, 0.3f), false);
            var vest = Visual("LeatherVest", EquipmentSlot.Chest, PlaceholderArt.VestSprites(), new Color(0.65f, 0.45f, 0.3f), false);
            var plate = Visual("PlateArmor", EquipmentSlot.Chest, PlaceholderArt.PlateSprites(), new Color(0.82f, 0.86f, 0.95f), false);
            var wardenPlate = Visual("WardenPlate", EquipmentSlot.Chest, PlaceholderArt.PlateSprites(), new Color(0.45f, 0.55f, 0.85f), false);
            var gloves = Visual("LeatherGloves", EquipmentSlot.Hands, PlaceholderArt.GloveSprites(), new Color(0.6f, 0.42f, 0.28f), false);
            var emberGloves = Visual("EmberGloves", EquipmentSlot.Hands, PlaceholderArt.GloveSprites(), new Color(1f, 0.5f, 0.25f), false);
            var boots = Visual("LeatherBoots", EquipmentSlot.Feet, PlaceholderArt.BootSprites(), new Color(0.5f, 0.35f, 0.25f), false);
            var greaves = Visual("IronGreaves", EquipmentSlot.Feet, PlaceholderArt.BootSprites(), new Color(0.8f, 0.84f, 0.92f), false);

            var swordVisual = Visual("Sword", EquipmentSlot.Weapon, PlaceholderArt.SwordSprites(), Color.white, false);
            var bowVisual = Visual("Bow", EquipmentSlot.Weapon, PlaceholderArt.BowSprites(), Color.white, false);
            var greatswordVisual = Visual("Greatsword", EquipmentSlot.Weapon, PlaceholderArt.GreatswordSprites(), Color.white, false);
            FillWeaponVisual(set.Sword, swordVisual);
            FillWeaponVisual(set.Bow, bowVisual);
            FillWeaponVisual(set.Greatsword, greatswordVisual);

            // ---------------- items
            var gold = Item("Gold", "Gold", PlaceholderArt.GoldIcon(), Rarity.Common, ItemCategory.Currency, 9999, 0, null, "Walk over it to collect.");
            var potion = Item("HealthPotion", "Health Potion", PlaceholderArt.PotionIcon(), Rarity.Common, ItemCategory.Consumable, 10, 40, null, "Right-click to drink.");
            var gel = Item("SlimeGel", "Slime Gel", PlaceholderArt.GelIcon(), Rarity.Common, ItemCategory.Material, 99, 0, null, "Sticky. Crafting material.");
            var wing = Item("BatWing", "Bat Wing", PlaceholderArt.WingIcon(), Rarity.Common, ItemCategory.Material, 99, 0, null, "Crafting material.");
            var bone = Item("Bone", "Old Bone", PlaceholderArt.BoneIcon(), Rarity.Common, ItemCategory.Material, 99, 0, null, "Crafting material.");
            var ember = Item("EmberShard", "Ember Shard", PlaceholderArt.EmberIcon(), Rarity.Uncommon, ItemCategory.Material, 50, 0, null, "Still warm.");

            var hoodItem = Equip("LeatherHood", "Leather Hood", hood, Rarity.Common);
            var helmItem = Equip("IronHelm", "Iron Helm", helm, Rarity.Rare);
            var crownItem = Equip("WardenCrown", "Warden's Crown", crown, Rarity.Legendary);
            var vestItem = Equip("LeatherVest", "Leather Vest", vest, Rarity.Common);
            var plateItem = Equip("PlateArmor", "Plate Armor", plate, Rarity.Rare);
            var wardenPlateItem = Equip("WardenPlate", "Warden's Plate", wardenPlate, Rarity.Epic);
            var glovesItem = Equip("LeatherGloves", "Leather Gloves", gloves, Rarity.Common);
            var emberGlovesItem = Equip("EmberGloves", "Ember Gloves", emberGloves, Rarity.Uncommon);
            var bootsItem = Equip("LeatherBoots", "Leather Boots", boots, Rarity.Common);
            var greavesItem = Equip("IronGreaves", "Iron Greaves", greaves, Rarity.Uncommon);

            set.Wardrobe = new[]
            {
                hoodItem, helmItem, crownItem, vestItem, plateItem, wardenPlateItem, glovesItem, emberGlovesItem,
                bootsItem, greavesItem
            };

            // ---------------- loot config
            set.Loot = GetOrCreate<LootConfig>($"{Root}/Loot/LootConfig.asset", l => { });
            if (set.Loot.goldItem == null)
            {
                set.Loot.goldItem = gold;
                EditorUtility.SetDirty(set.Loot);
            }

            // ---------------- loot tables (one per monster)
            var slimeLoot = Loot("SlimeLoot", 0.8f, 1, 5, 1, 1.2f,
                E(gel, 3f, 1, 2), E(potion, 1f), E(glovesItem, 0.35f), E(bootsItem, 0.35f));
            var batLoot = Loot("BatLoot", 0.7f, 1, 3, 1, 1.5f,
                E(wing, 3f), E(potion, 0.6f), E(hoodItem, 0.3f));
            var archerLoot = Loot("ArcherLoot", 0.9f, 3, 8, 1, 1f,
                E(bone, 3f, 1, 3), E(hoodItem, 1f), E(bootsItem, 1f), E(greavesItem, 0.5f), E(potion, 1f));
            var bruteLoot = Loot("BruteLoot", 1f, 10, 20, 2, 0.8f,
                E(helmItem, 1.2f), E(plateItem, 1.2f), E(greavesItem, 1f), E(potion, 1.5f, 1, 2));
            var cultistLoot = Loot("CultistLoot", 0.9f, 5, 12, 1, 0.8f,
                E(ember, 3f, 1, 3), E(emberGlovesItem, 1.2f), E(potion, 1f));
            var bossLoot = Loot("BossLoot", 1f, 80, 120, 3, 0.5f,
                E(crownItem, 1f), E(plateItem, 1f), E(helmItem, 1f), E(potion, 2f, 2, 3));
            AddGuaranteed(bossLoot, E(wardenPlateItem, 1f));

            set.DebugLoot = Loot("DebugLoot", 1f, 20, 60, 8, 0f,
                E(potion, 1f, 1, 3), E(gel, 1f, 5, 20), E(wing, 1f), E(bone, 1f), E(ember, 1f),
                E(hoodItem, 1f), E(helmItem, 1f), E(crownItem, 0.5f), E(vestItem, 1f), E(plateItem, 1f),
                E(wardenPlateItem, 0.5f), E(glovesItem, 1f), E(emberGlovesItem, 1f), E(bootsItem, 1f), E(greavesItem, 1f));

            FillLoot(set.Slime, slimeLoot);
            FillLoot(set.Bat, batLoot);
            FillLoot(set.Archer, archerLoot);
            FillLoot(set.Brute, bruteLoot);
            FillLoot(set.Cultist, cultistLoot);
            FillLoot(set.Boss, bossLoot);

            // ---------------- skills
            var skills = new[]
            {
                GetOrCreate<SkillConfig>($"{Root}/Skills/SpinSlash.asset", SpinSlash),
                GetOrCreate<SkillConfig>($"{Root}/Skills/PiercingShot.asset", PiercingShot),
                GetOrCreate<SkillConfig>($"{Root}/Skills/GroundSlam.asset", GroundSlam),
                GetOrCreate<SkillConfig>($"{Root}/Skills/FanOfKnives.asset", FanOfKnives)
            };

            // ---------------- player
            var player = set.Player;
            if (player.rig == null || Overwrite) player.rig = rig;
            if (player.skills == null || player.skills.Length == 0 || Overwrite) player.skills = skills;
            if (player.startingEquipment == null || player.startingEquipment.Length == 0 || Overwrite)
            {
                player.startingEquipment = new[] { vestItem, bootsItem };
            }
            if (player.startingItems == null || player.startingItems.Length == 0 || Overwrite)
            {
                player.startingItems = new[] { potion, potion, hoodItem };
            }
            EditorUtility.SetDirty(player);
        }

        // ------------------------------------------------------------------ helpers

        private static EquipmentVisual Visual(string name, EquipmentSlot slot, DirectionalSprites sprites, Color tint, bool hidesHair)
        {
            var visual = GetOrCreate<EquipmentVisual>($"{Root}/Equipment/{name}Visual.asset", v =>
            {
                v.slot = slot;
                v.tint = tint;
                v.hidesHair = hidesHair;
            });
            if (visual.sprites == null || visual.sprites.IsEmpty || Overwrite)
            {
                visual.sprites = sprites;
                EditorUtility.SetDirty(visual);
            }
            return visual;
        }

        private static void FillWeaponVisual(WeaponConfig weapon, EquipmentVisual visual)
        {
            if (weapon == null || (weapon.visual != null && !Overwrite)) return;
            weapon.visual = visual;
            EditorUtility.SetDirty(weapon);
        }

        private static ItemDefinition Item(string file, string displayName, Sprite icon, Rarity rarity, ItemCategory category,
            int maxStack, int heal, EquipmentVisual equipment, string description)
        {
            var item = GetOrCreate<ItemDefinition>($"{Root}/Items/{file}.asset", i =>
            {
                i.displayName = displayName;
                i.rarity = rarity;
                i.category = category;
                i.maxStack = maxStack;
                i.healAmount = heal;
                i.equipment = equipment;
                i.description = description;
            });
            if (item.icon == null || Overwrite)
            {
                item.icon = icon;
                EditorUtility.SetDirty(item);
            }
            return item;
        }

        private static ItemDefinition Equip(string file, string displayName, EquipmentVisual visual, Rarity rarity)
        {
            var item = Item(file, displayName, visual.sprites.down, rarity, ItemCategory.Equipment, 1, 0, visual,
                $"{visual.slot} armor. Changes how you look.");
            if (item.equipment == null)
            {
                item.equipment = visual;
                EditorUtility.SetDirty(item);
            }
            return item;
        }

        private static LootTable.Entry E(ItemDefinition item, float weight, int min = 1, int max = 1) =>
            new LootTable.Entry { item = item, weight = weight, minCount = min, maxCount = max };

        private static LootTable Loot(string name, float goldChance, int goldMin, int goldMax, int rolls, float nothing,
            params LootTable.Entry[] entries)
        {
            return GetOrCreate<LootTable>($"{Root}/Loot/{name}.asset", t =>
            {
                t.goldChance = goldChance;
                t.goldMin = goldMin;
                t.goldMax = goldMax;
                t.rolls = rolls;
                t.nothingWeight = nothing;
                t.entries = entries;
            });
        }

        private static void AddGuaranteed(LootTable table, params LootTable.Entry[] entries)
        {
            if (table.guaranteed != null && table.guaranteed.Length > 0 && !Overwrite) return;
            table.guaranteed = entries;
            EditorUtility.SetDirty(table);
        }

        private static void FillLoot(EnemyConfigBase enemy, LootTable table)
        {
            if (enemy == null || (enemy.loot != null && !Overwrite)) return;
            enemy.loot = table;
            EditorUtility.SetDirty(enemy);
        }

        // ------------------------------------------------------------------ skills

        private static void SpinSlash(SkillConfig s)
        {
            s.displayName = "Spin Slash";
            s.description = "Spin and hit everything around you.";
            s.color = new Color(0.55f, 0.85f, 1f);
            s.cooldown = 4f;
            s.attack = new AttackStep
            {
                name = "Spin Slash", hitWeight = HitWeight.Heavy, kind = AttackKind.MeleeArc, damage = 28,
                windup = 0.08f, active = 0.15f, recovery = 0.35f, range = 3f, arcDegrees = 360f, lungeDistance = 0f,
                knockback = 8f, stagger = 30f, hitStop = 0.08f, cameraShake = 0.25f, moveSpeedMultiplier = 0.3f,
                superArmor = true, slashColor = new Color(0.7f, 0.95f, 1f), slashWidth = 1.2f, slashDuration = 0.25f
            };
        }

        private static void PiercingShot(SkillConfig s)
        {
            s.displayName = "Piercing Shot";
            s.description = "A heavy bolt that passes through a line of enemies.";
            s.color = new Color(1f, 0.85f, 0.4f);
            s.cooldown = 3f;
            s.critChance = 0.3f;
            s.attack = new AttackStep
            {
                name = "Piercing Shot", hitWeight = HitWeight.Heavy, kind = AttackKind.Projectile, damage = 30,
                windup = 0.15f, active = 0.02f, recovery = 0.3f, projectileCount = 1, projectileSpeed = 36f,
                projectileRange = 22f, pierce = 5, projectileSize = 0.3f, projectileColor = new Color(1f, 0.9f, 0.45f),
                knockback = 6f, stagger = 20f, hitStop = 0.06f, cameraShake = 0.15f, moveSpeedMultiplier = 0.2f
            };
        }

        private static void GroundSlam(SkillConfig s)
        {
            s.displayName = "Ground Slam";
            s.description = "Slow, huge area hit. Breaks armor.";
            s.color = new Color(1f, 0.55f, 0.3f);
            s.cooldown = 8f;
            s.attack = new AttackStep
            {
                name = "Ground Slam", hitWeight = HitWeight.Finisher, kind = AttackKind.MeleeArc, damage = 55,
                windup = 0.35f, active = 0.1f, recovery = 0.6f, range = 4f, arcDegrees = 360f, lungeDistance = 0f,
                knockback = 16f, stagger = 80f, hitStop = 0.14f, cameraShake = 0.5f, moveSpeedMultiplier = 0f,
                superArmor = true, slashColor = new Color(1f, 0.6f, 0.3f), slashWidth = 2f, slashDuration = 0.3f
            };
        }

        private static void FanOfKnives(SkillConfig s)
        {
            s.displayName = "Fan of Knives";
            s.description = "Throw seven knives in a wide fan.";
            s.color = new Color(0.8f, 0.8f, 1f);
            s.cooldown = 5f;
            s.attack = new AttackStep
            {
                name = "Fan of Knives", hitWeight = HitWeight.Medium, kind = AttackKind.Projectile, damage = 10,
                windup = 0.06f, active = 0.02f, recovery = 0.25f, projectileCount = 7, spreadDegrees = 70f,
                projectileSpeed = 24f, projectileRange = 12f, projectileSize = 0.18f,
                projectileColor = new Color(0.85f, 0.85f, 1f), knockback = 3f, stagger = 8f, hitStop = 0.03f,
                cameraShake = 0.08f, moveSpeedMultiplier = 0.5f
            };
        }
    }
}
