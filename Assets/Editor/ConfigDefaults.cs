using UnityEditor;
using UnityEngine;
using Vela.Config;
using Vela.Core;

namespace Vela.EditorTools
{
    /// Creates the config assets under Assets/Config with tuned starting values. Existing
    /// assets are left alone (so your tweaks survive a scene rebuild) unless you use
    /// "Vela ▸ Reset Configs To Defaults".
    public static class ConfigDefaults
    {
        public const string Root = "Assets/Config";

        public static bool Overwrite { get; set; }

        public class Set
        {
            public PlayerConfig Player;
            public CameraConfig Camera;
            public CombatFeelConfig Feel;
            public FxLibrary Fx;
            public WeaponConfig Sword;
            public WeaponConfig Bow;
            public WeaponConfig Greatsword;
            public MonsterConfig Slime;
            public MonsterConfig Archer;
            public MonsterConfig Brute;
            public MonsterConfig Bat;
            public MonsterConfig Cultist;
            public MonsterConfig Dummy;
            public BossConfig Boss;
        }

        public static Set CreateAll(Materials materials)
        {
            var set = new Set();

            set.Fx = GetOrCreate<FxLibrary>($"{Root}/FxLibrary.asset", fx => { });
            // The FX library is plumbing, not tuning: always refresh its references.
            set.Fx.unlitTransparent = materials.Unlit;
            set.Fx.additive = materials.Additive;
            set.Fx.spriteFlash = materials.Flash;
            set.Fx.fadeTemplate = materials.FadeTemplate;
            set.Fx.softCircle = PlaceholderArt.SoftCircle();
            set.Fx.arrowSprite = PlaceholderArt.Arrow();
            set.Fx.orbSprite = PlaceholderArt.Orb();
            EditorUtility.SetDirty(set.Fx);

            set.Camera = GetOrCreate<CameraConfig>($"{Root}/Camera.asset", c => { });
            set.Feel = GetOrCreate<CombatFeelConfig>($"{Root}/CombatFeel.asset", c => { });

            set.Sword = GetOrCreate<WeaponConfig>($"{Root}/Weapons/Sword.asset", Sword);
            set.Bow = GetOrCreate<WeaponConfig>($"{Root}/Weapons/Bow.asset", Bow);
            set.Greatsword = GetOrCreate<WeaponConfig>($"{Root}/Weapons/Greatsword.asset", Greatsword);

            set.Player = GetOrCreate<PlayerConfig>($"{Root}/Player.asset", p => { });
            if (set.Player.weapons == null || set.Player.weapons.Length == 0 || Overwrite)
            {
                set.Player.weapons = new[] { set.Sword, set.Bow, set.Greatsword };
            }
            FillSprite(set.Player.visual, PlaceholderArt.Player(), 1.8f, 0.9f);
            EditorUtility.SetDirty(set.Player);

            set.Slime = GetOrCreate<MonsterConfig>($"{Root}/Monsters/Slime.asset", Slime);
            set.Archer = GetOrCreate<MonsterConfig>($"{Root}/Monsters/SkeletonArcher.asset", Archer);
            set.Brute = GetOrCreate<MonsterConfig>($"{Root}/Monsters/Brute.asset", Brute);
            set.Bat = GetOrCreate<MonsterConfig>($"{Root}/Monsters/Bat.asset", Bat);
            set.Cultist = GetOrCreate<MonsterConfig>($"{Root}/Monsters/Cultist.asset", Cultist);
            set.Dummy = GetOrCreate<MonsterConfig>($"{Root}/Monsters/TrainingDummy.asset", Dummy);

            FillSprite(set.Slime.visual, PlaceholderArt.Slime(), 1.0f, 1.1f);
            FillSprite(set.Archer.visual, PlaceholderArt.SkeletonArcher(), 1.8f, 0.9f);
            FillSprite(set.Brute.visual, PlaceholderArt.Brute(), 2.6f, 1.9f);
            FillSprite(set.Bat.visual, PlaceholderArt.Bat(), 0.9f, 0.8f);
            FillSprite(set.Cultist.visual, PlaceholderArt.Cultist(), 1.9f, 1f);
            FillSprite(set.Dummy.visual, PlaceholderArt.TrainingDummy(), 1.8f, 0.9f);

            set.Boss = GetOrCreate<BossConfig>($"{Root}/Boss/HollowWarden.asset", b => Boss(b, set));
            FillSprite(set.Boss.visual, PlaceholderArt.Warden(), 3.6f, 2.6f);

            foreach (var asset in new Object[] { set.Slime, set.Archer, set.Brute, set.Bat, set.Cultist, set.Dummy, set.Boss })
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            return set;
        }

        public class Materials
        {
            public Material Unlit;
            public Material Additive;
            public Material Flash;
            public Material FadeTemplate;
        }

        private static void FillSprite(CharacterVisual visual, Sprite sprite, float height, float shadow)
        {
            if (visual.sprite != null && !Overwrite) return;
            visual.sprite = sprite;
            visual.worldHeight = height;
            visual.shadowSize = shadow;
        }

        private static T GetOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            PlaceholderArt.EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));

            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null && !Overwrite) return asset;

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                init(asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                // Reset in place so references from the scene stay valid.
                var fresh = ScriptableObject.CreateInstance<T>();
                init(fresh);
                EditorUtility.CopySerialized(fresh, asset);
                Object.DestroyImmediate(fresh);
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ------------------------------------------------------------------ weapons

        private static void Sword(WeaponConfig w)
        {
            w.displayName = "Sword";
            w.uiColor = new Color(0.55f, 0.85f, 1f);
            w.critChance = 0.15f;
            w.critMultiplier = 1.75f;
            w.comboResetTime = 0.45f;
            w.canDashCancel = true;

            var slash = new Color(0.8f, 0.95f, 1f);
            w.combo = new[]
            {
                new AttackStep
                {
                    name = "Slash", hitWeight = HitWeight.Light, damage = 12, windup = 0.05f, active = 0.08f, recovery = 0.2f,
                    range = 2.3f, arcDegrees = 130f, lungeDistance = 0.5f,
                    knockback = 3f, stagger = 8f, hitStop = 0.04f, cameraShake = 0.08f,
                    moveSpeedMultiplier = 0.2f, slashColor = slash, slashWidth = 0.6f, slashDuration = 0.13f
                },
                new AttackStep
                {
                    name = "Backslash", hitWeight = HitWeight.Light, damage = 12, windup = 0.05f, active = 0.08f, recovery = 0.2f,
                    range = 2.3f, arcDegrees = 130f, lungeDistance = 0.5f,
                    knockback = 3f, stagger = 8f, hitStop = 0.04f, cameraShake = 0.08f,
                    moveSpeedMultiplier = 0.2f, slashColor = slash, slashWidth = 0.6f, slashDuration = 0.13f,
                    reverseSwing = true
                },
                new AttackStep
                {
                    name = "Thrust Finisher", hitWeight = HitWeight.Finisher, damage = 22, windup = 0.1f, active = 0.1f, recovery = 0.35f,
                    range = 2.8f, arcDegrees = 70f, lungeDistance = 1.4f,
                    knockback = 9f, stagger = 25f, hitStop = 0.09f, cameraShake = 0.2f,
                    moveSpeedMultiplier = 0.1f, slashColor = Color.white, slashWidth = 0.9f, slashDuration = 0.16f
                }
            };

            w.hasChargedAttack = true;
            w.chargeTime = 0.55f;
            w.chargeMoveSpeedMultiplier = 0.5f;
            w.chargedAttack = new AttackStep
            {
                name = "Dash Strike", hitWeight = HitWeight.Finisher, damage = 40, windup = 0f, active = 0.18f, recovery = 0.35f,
                range = 2.4f, arcDegrees = 100f, lungeDistance = 5.5f,
                knockback = 12f, stagger = 40f, hitStop = 0.12f, cameraShake = 0.35f,
                moveSpeedMultiplier = 0f, superArmor = true,
                slashColor = new Color(0.6f, 0.9f, 1f), slashWidth = 1f, slashDuration = 0.2f
            };
        }

        private static void Bow(WeaponConfig w)
        {
            w.displayName = "Bow";
            w.uiColor = new Color(0.75f, 1f, 0.5f);
            w.critChance = 0.25f;
            w.critMultiplier = 2f;
            w.comboResetTime = 0.3f;
            w.repeatWhileHeld = true;
            w.canDashCancel = true;

            w.combo = new[]
            {
                new AttackStep
                {
                    name = "Shot", hitWeight = HitWeight.Light, kind = AttackKind.Projectile, damage = 9, windup = 0.08f, active = 0.02f,
                    recovery = 0.28f, projectileCount = 1, projectileSpeed = 26f, projectileRange = 18f,
                    projectileSize = 0.18f, projectileColor = new Color(1f, 0.95f, 0.7f),
                    knockback = 2f, stagger = 5f, hitStop = 0.03f, cameraShake = 0.04f, moveSpeedMultiplier = 0.6f
                }
            };

            w.hasChargedAttack = true;
            w.chargeTime = 0.7f;
            w.chargeMoveSpeedMultiplier = 0.4f;
            w.chargedAttack = new AttackStep
            {
                name = "Piercing Volley", hitWeight = HitWeight.Heavy, kind = AttackKind.Projectile, damage = 22, windup = 0.02f, active = 0.02f,
                recovery = 0.4f, projectileCount = 3, spreadDegrees = 16f, projectileSpeed = 34f,
                projectileRange = 22f, pierce = 3, projectileSize = 0.26f, projectileColor = new Color(0.7f, 1f, 0.6f),
                knockback = 7f, stagger = 25f, hitStop = 0.07f, cameraShake = 0.2f, moveSpeedMultiplier = 0.3f
            };
        }

        private static void Greatsword(WeaponConfig w)
        {
            w.displayName = "Greatsword";
            w.uiColor = new Color(1f, 0.65f, 0.3f);
            w.critChance = 0.1f;
            w.critMultiplier = 2f;
            w.comboResetTime = 0.6f;
            w.inputBufferTime = 0.35f;
            w.canDashCancel = false;

            w.combo = new[]
            {
                new AttackStep
                {
                    name = "Cleave", hitWeight = HitWeight.Heavy, damage = 34, windup = 0.28f, active = 0.12f, recovery = 0.45f,
                    range = 3.2f, arcDegrees = 170f, lungeDistance = 0.8f,
                    knockback = 10f, stagger = 35f, hitStop = 0.11f, cameraShake = 0.3f,
                    moveSpeedMultiplier = 0.05f, superArmor = true,
                    slashColor = new Color(1f, 0.8f, 0.5f), slashWidth = 1.3f, slashDuration = 0.2f
                },
                new AttackStep
                {
                    name = "Overhead Smash", hitWeight = HitWeight.Finisher, damage = 52, windup = 0.36f, active = 0.12f, recovery = 0.6f,
                    range = 3.4f, arcDegrees = 110f, lungeDistance = 1f,
                    knockback = 14f, stagger = 60f, hitStop = 0.16f, cameraShake = 0.45f,
                    moveSpeedMultiplier = 0f, superArmor = true, reverseSwing = true,
                    slashColor = new Color(1f, 0.7f, 0.4f), slashWidth = 1.5f, slashDuration = 0.22f
                }
            };

            w.hasChargedAttack = true;
            w.chargeTime = 0.9f;
            w.chargeMoveSpeedMultiplier = 0.25f;
            w.chargedAttack = new AttackStep
            {
                name = "Whirlwind", hitWeight = HitWeight.Finisher, damage = 60, windup = 0.05f, active = 0.2f, recovery = 0.6f,
                range = 3.6f, arcDegrees = 360f, lungeDistance = 0f,
                knockback = 16f, stagger = 80f, hitStop = 0.14f, cameraShake = 0.5f,
                moveSpeedMultiplier = 0f, superArmor = true,
                slashColor = new Color(1f, 0.6f, 0.3f), slashWidth = 1.6f, slashDuration = 0.3f
            };
        }

        // ------------------------------------------------------------------ monsters

        /// 1. Melee lunger: walks up, telegraphs, hops at you.
        private static void Slime(MonsterConfig m)
        {
            m.displayName = "Green Slime";
            m.maxHealth = 45;
            m.colliderRadius = 0.45f;
            m.colliderHeight = 1f;
            m.poise = 0f;
            m.staggerDuration = 0.3f;
            m.deathBurstColor = new Color(0.5f, 0.9f, 0.3f);
            m.visual.moveBob = 0.14f;
            m.visual.idleBreath = 0.06f;
            m.behaviour = new EnemyBehaviour
            {
                movement = MovementStyle.Chase, moveSpeed = 3.2f, detectionRange = 10f, preferredDistance = 1.8f,
                attackDecisionDelay = 0.4f,
                attacks = new[]
                {
                    new EnemyAttack
                    {
                        name = "Hop Bite", hitWeight = HitWeight.Medium, kind = EnemyAttackKind.Lunge, maxRange = 3.2f, cooldown = 1.6f,
                        windup = 0.55f, active = 0.28f, recovery = 0.7f, damage = 10, knockback = 6f,
                        radius = 0.9f, dashSpeed = 10f, trackTurnSpeed = 300f, color = new Color(0.5f, 1f, 0.3f, 0.55f)
                    }
                }
            };
        }

        /// 2. Ranged kiter: holds distance, aimed shots, a fan volley, and a kick if you get close.
        private static void Archer(MonsterConfig m)
        {
            m.displayName = "Skeleton Archer";
            m.maxHealth = 35;
            m.colliderHeight = 1.7f;
            m.poise = 10f;
            m.staggerDuration = 0.4f;
            m.deathBurstColor = new Color(0.9f, 0.88f, 0.8f);
            m.behaviour = new EnemyBehaviour
            {
                movement = MovementStyle.KeepDistance, moveSpeed = 3f, detectionRange = 13f, preferredDistance = 7f,
                attackDecisionDelay = 0.5f,
                attacks = new[]
                {
                    new EnemyAttack
                    {
                        name = "Aimed Shot", hitWeight = HitWeight.Light, kind = EnemyAttackKind.Projectile, minRange = 3f, maxRange = 14f,
                        cooldown = 1.8f, windup = 0.7f, active = 0.05f, recovery = 0.6f, damage = 12, knockback = 4f,
                        projectileSpeed = 13f, projectileRange = 16f, projectileSize = 0.25f, trackTurnSpeed = 200f,
                        color = new Color(1f, 0.4f, 0.3f, 0.5f)
                    },
                    new EnemyAttack
                    {
                        name = "Scatter Volley", hitWeight = HitWeight.Light, kind = EnemyAttackKind.Projectile, weight = 0.5f, minRange = 3f,
                        maxRange = 10f, cooldown = 5f, windup = 1f, active = 0.05f, recovery = 0.7f, damage = 8,
                        knockback = 3f, projectileCount = 5, spreadDegrees = 50f, volleys = 2, volleyInterval = 0.35f,
                        projectileSpeed = 10f, projectileRange = 12f, projectileSize = 0.25f,
                        trackDuringWindup = true, trackTurnSpeed = 120f, color = new Color(1f, 0.6f, 0.2f, 0.5f)
                    },
                    new EnemyAttack
                    {
                        name = "Kick", hitWeight = HitWeight.Medium, kind = EnemyAttackKind.MeleeArc, weight = 2f, maxRange = 2f, cooldown = 2f,
                        windup = 0.35f, active = 0.1f, recovery = 0.4f, damage = 6, knockback = 10f, radius = 1.8f,
                        arcDegrees = 100f, color = new Color(1f, 0.9f, 0.6f, 0.5f)
                    }
                }
            };
        }

        /// 3. Heavy tank: slow, big telegraphed swings and slams, charges from range, hard to stagger.
        private static void Brute(MonsterConfig m)
        {
            m.displayName = "Stone Brute";
            m.maxHealth = 220;
            m.colliderRadius = 0.75f;
            m.colliderHeight = 2.2f;
            m.poise = 60f;
            m.staggerDuration = 0.6f;
            m.knockbackResistance = 0.7f;
            m.deathBurstColor = new Color(0.65f, 0.5f, 0.75f);
            m.visual.moveBob = 0.05f;
            m.visual.moveLean = 3f;
            m.behaviour = new EnemyBehaviour
            {
                movement = MovementStyle.Chase, moveSpeed = 2.3f, turnSpeed = 240f, detectionRange = 10f,
                preferredDistance = 2.2f, attackDecisionDelay = 0.6f,
                attacks = new[]
                {
                    new EnemyAttack
                    {
                        name = "Club Swing", hitWeight = HitWeight.Heavy, kind = EnemyAttackKind.MeleeArc, maxRange = 3.2f, cooldown = 2f,
                        windup = 0.8f, active = 0.12f, recovery = 0.8f, damage = 22, knockback = 14f, radius = 3.2f,
                        arcDegrees = 140f, trackTurnSpeed = 120f, color = new Color(1f, 0.3f, 0.2f, 0.5f)
                    },
                    new EnemyAttack
                    {
                        name = "Ground Slam", hitWeight = HitWeight.Finisher, kind = EnemyAttackKind.SelfAoE, weight = 0.6f, maxRange = 3.5f,
                        cooldown = 5f, windup = 1.1f, active = 0.1f, recovery = 1.1f, damage = 28, knockback = 16f,
                        radius = 4f, color = new Color(1f, 0.5f, 0.2f, 0.5f)
                    },
                    new EnemyAttack
                    {
                        name = "Charge", hitWeight = HitWeight.Heavy, kind = EnemyAttackKind.Charge, minRange = 5f, maxRange = 12f, cooldown = 6f,
                        windup = 0.9f, active = 0.9f, recovery = 0.9f, damage = 25, knockback = 16f, radius = 1.2f,
                        dashSpeed = 15f, trackTurnSpeed = 90f, color = new Color(1f, 0.2f, 0.2f, 0.45f)
                    }
                }
            };
        }

        /// 4. Fast swarmer: flies erratically, quick weak dives. Dangerous in groups.
        private static void Bat(MonsterConfig m)
        {
            m.displayName = "Cave Bat";
            m.maxHealth = 18;
            m.colliderRadius = 0.35f;
            m.colliderHeight = 0.9f;
            m.poise = 0f;
            m.staggerDuration = 0.25f;
            m.groupAlertRadius = 10f;
            m.deathBurstColor = new Color(0.6f, 0.4f, 0.9f);
            m.visual.hoverHeight = 0.7f;
            m.visual.moveBob = 0f;
            m.visual.moveLean = 12f;
            m.behaviour = new EnemyBehaviour
            {
                movement = MovementStyle.Erratic, moveSpeed = 5.5f, erraticInterval = 0.45f, detectionRange = 11f,
                preferredDistance = 2.5f, attackDecisionDelay = 0.3f,
                attacks = new[]
                {
                    new EnemyAttack
                    {
                        name = "Dive", hitWeight = HitWeight.Light, kind = EnemyAttackKind.Lunge, maxRange = 4f, cooldown = 1.2f, windup = 0.35f,
                        active = 0.22f, recovery = 0.45f, damage = 6, knockback = 3f, radius = 0.7f, dashSpeed = 14f,
                        trackTurnSpeed = 400f, color = new Color(0.8f, 0.4f, 1f, 0.5f)
                    }
                }
            };
        }

        /// 5. Caster: keeps away and drops fire circles on you, plus a close-range nova.
        private static void Cultist(MonsterConfig m)
        {
            m.displayName = "Ember Cultist";
            m.maxHealth = 55;
            m.poise = 20f;
            m.staggerDuration = 0.45f;
            m.deathBurstColor = new Color(1f, 0.5f, 0.2f);
            m.behaviour = new EnemyBehaviour
            {
                movement = MovementStyle.KeepDistance, moveSpeed = 2.6f, detectionRange = 14f, preferredDistance = 8f,
                attackDecisionDelay = 0.6f,
                attacks = new[]
                {
                    new EnemyAttack
                    {
                        name = "Fire Circles", hitWeight = HitWeight.Medium, kind = EnemyAttackKind.GroundAoE, maxRange = 14f, cooldown = 3.5f,
                        windup = 1.2f, active = 0.1f, recovery = 0.6f, damage = 18, knockback = 6f, radius = 1.8f,
                        aoeCount = 3, aoeScatter = 3f, trackDuringWindup = false, color = new Color(1f, 0.45f, 0.1f, 0.5f)
                    },
                    new EnemyAttack
                    {
                        name = "Ember Nova", hitWeight = HitWeight.Light, kind = EnemyAttackKind.RadialBurst, weight = 1.5f, maxRange = 4f,
                        cooldown = 5f, windup = 0.8f, active = 0.1f, recovery = 0.7f, damage = 10, knockback = 5f,
                        projectileCount = 10, volleys = 2, volleyInterval = 0.3f, projectileSpeed = 7f,
                        projectileRange = 9f, projectileSize = 0.28f, color = new Color(1f, 0.6f, 0.2f, 0.5f)
                    }
                }
            };
        }

        /// Punching bag near the spawn: never dies, heals when left alone, doesn't move.
        private static void Dummy(MonsterConfig m)
        {
            m.displayName = "Training Dummy";
            m.maxHealth = 500;
            m.immortal = true;
            m.poise = 0f;
            m.staggerDuration = 0.2f;
            m.knockbackResistance = 1f;
            m.colliderRadius = 0.4f;
            m.colliderHeight = 1.7f;
            m.deathBurstColor = new Color(0.85f, 0.65f, 0.4f);
            m.visual.idleBreath = 0f;
            m.behaviour = new EnemyBehaviour
            {
                movement = MovementStyle.Stationary, detectionRange = 0f, attacks = new EnemyAttack[0]
            };
        }

        // ------------------------------------------------------------------ boss

        private static void Boss(BossConfig b, Set set)
        {
            b.displayName = "The Hollow Warden";
            b.maxHealth = 1600;
            b.colliderRadius = 1f;
            b.colliderHeight = 3f;
            b.poise = 150f;
            b.staggerDuration = 0.8f;
            b.knockbackResistance = 0.9f;
            b.groupAlertRadius = 0f;
            b.deathBurstColor = new Color(0.5f, 0.9f, 1f);
            b.visual.moveBob = 0.04f;
            b.visual.moveLean = 2f;

            var sweepColor = new Color(1f, 0.3f, 0.2f, 0.5f);

            var phase1 = new BossPhase
            {
                name = "Phase I — The Warden Wakes",
                startsAtHealthFraction = 1f,
                tint = Color.white,
                spriteScale = 1f,
                transitionDuration = 0f,
                shockwave = false,
                behaviour = new EnemyBehaviour
                {
                    movement = MovementStyle.Chase, moveSpeed = 2.8f, turnSpeed = 200f, detectionRange = 14f,
                    leashRange = 40f, preferredDistance = 2.8f, attackDecisionDelay = 0.6f,
                    attacks = new[]
                    {
                        new EnemyAttack
                        {
                            name = "Great Sweep", hitWeight = HitWeight.Heavy, kind = EnemyAttackKind.MeleeArc, weight = 2f, maxRange = 4f,
                            cooldown = 2.2f, windup = 0.75f, active = 0.12f, recovery = 0.7f, damage = 20,
                            knockback = 14f, radius = 4f, arcDegrees = 160f, trackTurnSpeed = 150f, color = sweepColor
                        },
                        new EnemyAttack
                        {
                            name = "Shoulder Charge", hitWeight = HitWeight.Finisher, kind = EnemyAttackKind.Charge, weight = 1.5f, minRange = 6f,
                            maxRange = 16f, cooldown = 5f, windup = 1f, active = 1f, recovery = 1f, damage = 24,
                            knockback = 18f, radius = 1.5f, dashSpeed = 17f, trackTurnSpeed = 90f,
                            color = new Color(1f, 0.2f, 0.2f, 0.45f)
                        },
                        new EnemyAttack
                        {
                            name = "Blade Fan", hitWeight = HitWeight.Medium, kind = EnemyAttackKind.Projectile, minRange = 4f, maxRange = 14f,
                            cooldown = 4f, windup = 0.8f, active = 0.05f, recovery = 0.7f, damage = 12, knockback = 5f,
                            projectileCount = 5, spreadDegrees = 60f, projectileSpeed = 11f, projectileRange = 16f,
                            projectileSize = 0.35f, trackTurnSpeed = 120f, color = new Color(0.5f, 0.9f, 1f, 0.5f)
                        }
                    }
                }
            };

            var phase2 = new BossPhase
            {
                name = "Phase II — Unbound",
                startsAtHealthFraction = 0.5f,
                tint = new Color(1f, 0.55f, 0.45f),
                spriteScale = 1.15f,
                transitionDuration = 1.6f,
                announcement = "The Warden breaks its chains!",
                shockwave = true,
                shockwaveRadius = 6f,
                shockwaveDamage = 15,
                shockwaveKnockback = 18f,
                summonOnEnter = new[] { set.Bat, set.Bat },
                behaviour = new EnemyBehaviour
                {
                    movement = MovementStyle.Chase, moveSpeed = 3.6f, turnSpeed = 300f, detectionRange = 20f,
                    leashRange = 40f, preferredDistance = 3f, attackDecisionDelay = 0.35f,
                    attacks = new[]
                    {
                        new EnemyAttack
                        {
                            name = "Great Sweep", hitWeight = HitWeight.Heavy, kind = EnemyAttackKind.MeleeArc, weight = 2f, maxRange = 4.2f,
                            cooldown = 1.6f, windup = 0.55f, active = 0.12f, recovery = 0.55f, damage = 24,
                            knockback = 15f, radius = 4.2f, arcDegrees = 180f, trackTurnSpeed = 200f, color = sweepColor
                        },
                        new EnemyAttack
                        {
                            name = "Rampage Charge", hitWeight = HitWeight.Finisher, kind = EnemyAttackKind.Charge, weight = 1.5f, minRange = 5f,
                            maxRange = 18f, cooldown = 3.5f, windup = 0.75f, active = 1f, recovery = 0.8f, damage = 26,
                            knockback = 20f, radius = 1.6f, dashSpeed = 20f, trackTurnSpeed = 120f,
                            color = new Color(1f, 0.2f, 0.2f, 0.45f)
                        },
                        new EnemyAttack
                        {
                            name = "Nova Burst", hitWeight = HitWeight.Light, kind = EnemyAttackKind.RadialBurst, weight = 1.2f, maxRange = 8f,
                            cooldown = 5f, windup = 0.9f, active = 0.1f, recovery = 0.8f, damage = 12, knockback = 6f,
                            projectileCount = 14, volleys = 3, volleyInterval = 0.35f, projectileSpeed = 8f,
                            projectileRange = 14f, projectileSize = 0.35f, color = new Color(1f, 0.4f, 0.3f, 0.5f)
                        },
                        new EnemyAttack
                        {
                            name = "Meteor Rain", hitWeight = HitWeight.Heavy, kind = EnemyAttackKind.GroundAoE, maxRange = 16f, cooldown = 6f,
                            windup = 1.1f, active = 0.1f, recovery = 0.7f, damage = 20, knockback = 8f, radius = 2.2f,
                            aoeCount = 5, aoeScatter = 4f, trackDuringWindup = false,
                            color = new Color(1f, 0.3f, 0.1f, 0.5f)
                        },
                        new EnemyAttack
                        {
                            name = "Call the Swarm", hitWeight = HitWeight.Light, kind = EnemyAttackKind.Summon, weight = 0.5f, maxRange = 16f,
                            cooldown = 14f, windup = 1f, active = 0.1f, recovery = 0.6f, summon = set.Bat,
                            summonCount = 2, color = new Color(0.8f, 0.4f, 1f, 0.5f)
                        }
                    }
                }
            };

            b.phases = new[] { phase1, phase2 };
        }
    }
}
