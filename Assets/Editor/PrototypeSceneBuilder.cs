using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Vela.CameraRig;
using Vela.Combat;
using Vela.Config;
using Vela.Core;
using Vela.Gameplay;
using Vela.Player;
using Vela.UI;
using Vela.Visual;
using Vela.World;

namespace Vela.EditorTools
{
    /// Vela ▸ Build Combat Prototype Scene. Generates art, configs, materials and the level.
    /// Rebuilding keeps your config assets (and any sprites you've swapped in).
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/CombatPrototype.unity";
        private const string MaterialsFolder = "Assets/Materials";

        private static Material grass;
        private static Material path;
        private static Material stone;
        private static Material arena;
        private static Material bark;
        private static Material leaves;
        private static Material rock;

        [MenuItem("Vela/Build Combat Prototype Scene", priority = 0)]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        [MenuItem("Vela/Regenerate Placeholder Art", priority = 20)]
        public static void RegenerateArt()
        {
            PlaceholderArt.Regenerate = true;
            try
            {
                BuildMaterials();
                ConfigDefaults.CreateAll(BuildFxMaterials());
            }
            finally
            {
                PlaceholderArt.Regenerate = false;
            }
            Debug.Log("Vela: placeholder art regenerated in " + PlaceholderArt.Folder);
        }

        [MenuItem("Vela/Reset Configs To Defaults", priority = 21)]
        public static void ResetConfigs()
        {
            if (!EditorUtility.DisplayDialog("Reset configs",
                    "Overwrite every asset in Assets/Config with the default values? Your tuning will be lost.",
                    "Reset", "Cancel")) return;

            ConfigDefaults.Overwrite = true;
            try
            {
                ConfigDefaults.CreateAll(BuildFxMaterials());
            }
            finally
            {
                ConfigDefaults.Overwrite = false;
            }
            Debug.Log("Vela: configs reset to defaults.");
        }

        private static void Build()
        {
            PlaceholderArt.EnsureFolder("Assets/Scenes");
            PlaceholderArt.EnsureFolder(MaterialsFolder);

            BuildMaterials();
            var configs = ConfigDefaults.CreateAll(BuildFxMaterials());

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetUpLighting();
            var level = new GameObject("Level").transform;
            BuildGround(level);
            BuildPaths(level);
            BuildBorder(level);
            BuildLandmarks(level);
            var bossEntrance = BuildBossArena(level);

            var player = BuildPlayer(configs, new Vector3(0f, 0f, -34f));
            BuildCamera(configs, player.transform);
            BuildSpawns(configs);

            var manager = new GameObject("GameManager").AddComponent<CombatGameManager>();
            manager.Configure(configs.Feel, configs.Camera, configs.Fx, bossEntrance);
            new GameObject("HUD").AddComponent<CombatHUD>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            Selection.activeObject = configs.Player;
            Debug.Log($"Vela: combat prototype built at {ScenePath}. Press Play. Configs live in {ConfigDefaults.Root}.");
        }

        // ------------------------------------------------------------------ materials

        private static void BuildMaterials()
        {
            grass = WorldMaterial("Grass", PlaceholderArt.Grass(), Color.white);
            path = WorldMaterial("Path", PlaceholderArt.Path(), Color.white);
            stone = WorldMaterial("Stone", PlaceholderArt.Stone(), Color.white);
            arena = WorldMaterial("ArenaFloor", PlaceholderArt.ArenaFloor(), Color.white);
            bark = WorldMaterial("Bark", PlaceholderArt.Bark(), Color.white);
            leaves = WorldMaterial("Leaves", PlaceholderArt.Leaves(), Color.white);
            rock = WorldMaterial("Rock", PlaceholderArt.Stone(), new Color(0.75f, 0.7f, 0.65f));
        }

        private static ConfigDefaults.Materials BuildFxMaterials()
        {
            var fade = GetOrCreateMaterial("FX_FadeTemplate", LitShader());
            FadeableObject.MakeTransparent(fade);
            EditorUtility.SetDirty(fade);

            return new ConfigDefaults.Materials
            {
                Unlit = GetOrCreateMaterial("FX_Unlit", Shader.Find("Sprites/Default")),
                Additive = GetOrCreateMaterial("FX_Additive",
                    Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default")),
                Flash = GetOrCreateMaterial("FX_SpriteFlash", Shader.Find("GUI/Text Shader")),
                FadeTemplate = fade
            };
        }

        private static Material WorldMaterial(string name, Texture2D texture, Color color)
        {
            var material = GetOrCreateMaterial(name, LitShader());
            material.mainTexture = texture;
            material.color = color;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.05f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateMaterial(string name, Shader shader)
        {
            var assetPath = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null)
            {
                if (material.shader != shader) material.shader = shader;
                return material;
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static Shader LitShader()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                var urp = Shader.Find("Universal Render Pipeline/Lit");
                if (urp != null) return urp;
            }
            return Shader.Find("Standard");
        }

        // ------------------------------------------------------------------ level

        private static void SetUpLighting()
        {
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            light.intensity = 1.05f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.66f);
            RenderSettings.fog = false;
        }

        private static void BuildGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(0f, 0f, 8f);
            ground.transform.localScale = new Vector3(6.6f, 1f, 9.8f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = grass;
            ground.AddComponent<TextureTiling>().Configure(TextureTiling.Mode.Floor, 2f, 10f);
        }

        private static void BuildPaths(Transform parent)
        {
            var root = new GameObject("FootPaths").transform;
            root.SetParent(parent, false);

            // Main north-south road from the spawn to the boss arena.
            Floor(root, "Path_Main", new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 72f));
            // East-west crossings.
            Floor(root, "Path_CrossSouth", new Vector3(0f, 0f, -8f), new Vector3(48f, 0f, 2.5f));
            Floor(root, "Path_CrossNorth", new Vector3(0f, 0f, 14f), new Vector3(44f, 0f, 2.5f));
            // Spurs into the side zones.
            Floor(root, "Path_TrainingYard", new Vector3(-6f, 0f, -30f), new Vector3(10f, 0f, 2f));
            Floor(root, "Path_WestSpur", new Vector3(-22f, 0f, 3f), new Vector3(2f, 0f, 22f));
            Floor(root, "Path_EastSpur", new Vector3(22f, 0f, 3f), new Vector3(2f, 0f, 22f));
        }

        private static void Floor(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var box = CreateBox(parent, name, new Vector3(center.x, 0.01f, center.z), new Vector3(size.x, 0.02f, size.z),
                path, false, TextureTiling.Mode.Floor, 2f);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void BuildBorder(Transform parent)
        {
            var root = new GameObject("Border").transform;
            root.SetParent(parent, false);

            const float h = 3f;
            Wall(root, "Border_South", new Vector3(0f, 0f, -41f), new Vector3(66f, h, 1f));
            Wall(root, "Border_North", new Vector3(0f, 0f, 57f), new Vector3(66f, h, 1f));
            Wall(root, "Border_West", new Vector3(-33f, 0f, 8f), new Vector3(1f, h, 98f));
            Wall(root, "Border_East", new Vector3(33f, 0f, 8f), new Vector3(1f, h, 98f));
        }

        private static void BuildLandmarks(Transform parent)
        {
            var root = new GameObject("Landmarks").transform;
            root.SetParent(parent, false);

            // Spawn: a ruined wall right in front of the camera to show the see-through effect,
            // and a low-fenced training yard with dummies to the west.
            Wall(root, "Ruin_SpawnWall", new Vector3(4.5f, 0f, -30f), new Vector3(4f, 4f, 1f));
            Wall(root, "Ruin_SpawnPillar", new Vector3(-3f, 0f, -26f), new Vector3(1.2f, 4.5f, 1.2f));
            LowWall(root, "Fence_Yard_S", new Vector3(-9f, 0f, -35f), new Vector3(8f, 0.6f, 0.4f));
            LowWall(root, "Fence_Yard_W", new Vector3(-13f, 0f, -31f), new Vector3(0.4f, 0.6f, 8f));
            LowWall(root, "Fence_Yard_N", new Vector3(-9f, 0f, -27f), new Vector3(8f, 0.6f, 0.4f));

            // South hub: slimes among broken pillars.
            Wall(root, "Pillar_Hub_A", new Vector3(-5f, 0f, -5f), new Vector3(1.2f, 4.5f, 1.2f));
            Wall(root, "Pillar_Hub_B", new Vector3(5f, 0f, -5f), new Vector3(1.2f, 4.5f, 1.2f));
            Wall(root, "Pillar_Hub_C", new Vector3(-5f, 0f, -12f), new Vector3(1.2f, 2f, 1.2f));
            Wall(root, "Pillar_Hub_D", new Vector3(5f, 0f, -12f), new Vector3(1.2f, 4.5f, 1.2f));

            // West: bat grove.
            Tree(root, new Vector3(-14f, 0f, -3f));
            Tree(root, new Vector3(-20f, 0f, -1f));
            Tree(root, new Vector3(-25f, 0f, -6f));
            Tree(root, new Vector3(-23f, 0f, -13f));
            Tree(root, new Vector3(-15f, 0f, -14f));
            Tree(root, new Vector3(-28f, 0f, -18f));
            Tree(root, new Vector3(-10f, 0f, -18f));

            // East: archers behind low cover, a tall wall behind them.
            LowWall(root, "Cover_East_A", new Vector3(16f, 0f, -5f), new Vector3(0.6f, 0.9f, 4f));
            LowWall(root, "Cover_East_B", new Vector3(19f, 0f, -12f), new Vector3(4f, 0.9f, 0.6f));
            Wall(root, "Ruin_East", new Vector3(25f, 0f, -8f), new Vector3(1f, 3.5f, 8f));
            Rock(root, new Vector3(12f, 0f, -16f), new Vector3(2.5f, 2.2f, 2f));

            // Crossroads: the brute's ruined courtyard.
            Wall(root, "Ruin_Court_W", new Vector3(-6f, 0f, 19f), new Vector3(6f, 3.2f, 1f));
            Wall(root, "Ruin_Court_E", new Vector3(7f, 0f, 10f), new Vector3(5f, 3.2f, 1f));
            Wall(root, "Pillar_Court_A", new Vector3(-4f, 0f, 9f), new Vector3(1.2f, 4.5f, 1.2f));
            Wall(root, "Pillar_Court_B", new Vector3(5f, 0f, 20f), new Vector3(1.2f, 4.5f, 1.2f));

            // North-east: cultists among boulders.
            Rock(root, new Vector3(15f, 0f, 21f), new Vector3(3f, 2.6f, 2.5f));
            Rock(root, new Vector3(25f, 0f, 21f), new Vector3(2.5f, 3f, 3f));
            Rock(root, new Vector3(20f, 0f, 9f), new Vector3(2f, 2.4f, 2f));
            Rock(root, new Vector3(28f, 0f, 13f), new Vector3(2f, 2f, 2.5f));

            // North-west: mixed pack in a thicket.
            Tree(root, new Vector3(-14f, 0f, 21f));
            Tree(root, new Vector3(-26f, 0f, 20f));
            Tree(root, new Vector3(-27f, 0f, 10f));
            Tree(root, new Vector3(-12f, 0f, 9f));
            Wall(root, "Ruin_NW", new Vector3(-20f, 0f, 24f), new Vector3(6f, 3f, 1f));
        }

        private static Transform BuildBossArena(Transform parent)
        {
            var root = new GameObject("BossArena").transform;
            root.SetParent(parent, false);

            var center = new Vector3(0f, 0f, 42f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(root, false);
            floor.transform.position = center + Vector3.up * 0.015f;
            floor.transform.localScale = new Vector3(26f, 0.01f, 26f);
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.GetComponent<MeshRenderer>().sharedMaterial = arena;
            floor.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            floor.AddComponent<TextureTiling>().Configure(TextureTiling.Mode.Floor, 2f, 1f);

            // Ring of pillars with a gap at the south for the entrance.
            const int count = 14;
            for (var i = 0; i < count; i++)
            {
                var angle = 360f / count * i;
                if (Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) < 20f) continue;
                var offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 14f;
                var height = i % 2 == 0 ? 5f : 3.5f;
                Wall(root, $"ArenaPillar_{i}", center + offset, new Vector3(1.6f, height, 1.6f));
            }

            var entrance = new GameObject("BossArenaEntrance").transform;
            entrance.SetParent(root, false);
            entrance.position = new Vector3(0f, 0.1f, 26f);
            return entrance;
        }

        private static GameObject Wall(Transform parent, string name, Vector3 foot, Vector3 size)
        {
            return CreateBox(parent, name, foot + Vector3.up * (size.y * 0.5f), size, stone, true,
                TextureTiling.Mode.Wall, 2f);
        }

        private static void LowWall(Transform parent, string name, Vector3 foot, Vector3 size)
        {
            CreateBox(parent, name, foot + Vector3.up * (size.y * 0.5f), size, bark, false, TextureTiling.Mode.Wall, 1f);
        }

        private static void Rock(Transform parent, Vector3 foot, Vector3 size)
        {
            var box = CreateBox(parent, "Rock", foot + Vector3.up * (size.y * 0.5f), size, rock, true,
                TextureTiling.Mode.Wall, 2f);
            box.transform.rotation = Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f);
        }

        private static void Tree(Transform parent, Vector3 foot)
        {
            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = foot;
            tree.AddComponent<FadeableObject>();

            CreateBox(tree.transform, "Trunk", foot + Vector3.up * 1.25f, new Vector3(0.7f, 2.5f, 0.7f),
                bark, false, TextureTiling.Mode.Wall, 1f);
            CreateBox(tree.transform, "Canopy", foot + Vector3.up * 3.6f, new Vector3(3f, 2.4f, 3f),
                leaves, false, TextureTiling.Mode.Wall, 1.5f);
            CreateBox(tree.transform, "CanopyTop", foot + Vector3.up * 5.1f, new Vector3(1.8f, 1.2f, 1.8f),
                leaves, false, TextureTiling.Mode.Wall, 1.5f);
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 center, Vector3 size, Material material,
            bool fadeable, TextureTiling.Mode tiling, float tileSize)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = center;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            box.AddComponent<TextureTiling>().Configure(tiling, tileSize, 1f);
            if (fadeable) box.AddComponent<FadeableObject>();
            return box;
        }

        // ------------------------------------------------------------------ actors

        private static GameObject BuildPlayer(ConfigDefaults.Set configs, Vector3 position)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = position;

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 50f;

            player.AddComponent<Health>();
            player.AddComponent<SpriteBillboard>();
            player.AddComponent<DamageFeedback>();

            var playerController = player.AddComponent<PlayerController>();
            var serialized = new SerializedObject(playerController);
            serialized.FindProperty("config").objectReferenceValue = configs.Player;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            player.AddComponent<PlayerCombat>();
            return player;
        }

        private static void BuildCamera(ConfigDefaults.Set configs, Transform target)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.12f, 0.16f);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<AudioListener>();

            var rig = cameraObject.AddComponent<CombatCameraRig>();
            rig.Config = configs.Camera;
            rig.Target = target;
            cameraObject.AddComponent<OcclusionFader>();
        }

        private static void BuildSpawns(ConfigDefaults.Set c)
        {
            var root = new GameObject("EnemySpawns").transform;

            // Training yard
            Spawn(root, c.Dummy, -10f, -32f);
            Spawn(root, c.Dummy, -7f, -29f);
            Spawn(root, c.Dummy, -11f, -28.5f);

            // South hub: slimes
            Spawn(root, c.Slime, -2.5f, -9f);
            Spawn(root, c.Slime, 2.5f, -10f);
            Spawn(root, c.Slime, 0f, -5.5f);

            // West grove: bats
            Spawn(root, c.Bat, -18f, -7f);
            Spawn(root, c.Bat, -20f, -9f);
            Spawn(root, c.Bat, -17f, -10f);

            // East cover: archers
            Spawn(root, c.Archer, 20f, -4f);
            Spawn(root, c.Archer, 22f, -11f);

            // Crossroads: the brute
            Spawn(root, c.Brute, 0f, 15f);

            // North-east: cultists
            Spawn(root, c.Cultist, 18f, 17f);
            Spawn(root, c.Cultist, 23f, 13f);

            // North-west: one of each
            Spawn(root, c.Slime, -16f, 15f);
            Spawn(root, c.Bat, -20f, 17f);
            Spawn(root, c.Archer, -23f, 13f);
            Spawn(root, c.Cultist, -18f, 19f);

            // Boss
            Spawn(root, c.Boss, 0f, 44f);
        }

        private static void Spawn(Transform parent, EnemyConfigBase config, float x, float z)
        {
            var go = new GameObject($"Spawn_{config.name}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, 0.05f, z);
            go.AddComponent<EnemySpawnPoint>().Config = config;
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(entry => entry.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
