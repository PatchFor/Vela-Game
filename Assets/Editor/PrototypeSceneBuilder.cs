using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Vela.CameraRig;
using Vela.Core;
using Vela.Enemies;
using Vela.Gameplay;
using Vela.Player;

namespace Vela.EditorTools
{
    /// Builds the whole test arena from code: no committed scene file, no prefabs,
    /// nothing to merge. Re-run it any time to get a clean arena back.
    public static class PrototypeSceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string MaterialsFolder = "Assets/Materials";
        private const string ScenePath = ScenesFolder + "/Prototype.unity";

        [MenuItem("Vela/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder("Scenes");
            EnsureFolder("Materials");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Material ground = GetOrCreateMaterial("Ground", new Color(0.20f, 0.23f, 0.29f));
            Material wall = GetOrCreateMaterial("Wall", new Color(0.13f, 0.15f, 0.20f));
            Material obstacle = GetOrCreateMaterial("Obstacle", new Color(0.31f, 0.35f, 0.44f));
            Material playerMaterial = GetOrCreateMaterial("Player", new Color(0.95f, 0.72f, 0.28f));
            Material rusherMaterial = GetOrCreateMaterial("Rusher", new Color(0.85f, 0.30f, 0.34f));
            Material casterMaterial = GetOrCreateMaterial("Caster", new Color(0.55f, 0.35f, 0.85f));
            Material bruteMaterial = GetOrCreateMaterial("Brute", new Color(0.32f, 0.45f, 0.62f));

            BuildArena(ground, wall, obstacle);
            BuildZonePads();

            GameObject player = BuildPlayer(playerMaterial);
            Camera camera = SetUpCamera(player.transform);
            player.GetComponent<IsometricPlayerController>().CameraPivot = camera.transform;

            SetUpLight();
            BuildMonsters(rusherMaterial, casterMaterial, bruteMaterial);
            BuildStarterLoot();

            new GameObject("GameManager").AddComponent<PrototypeGameManager>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            Debug.Log("Vela: test arena built at " + ScenePath + ". Press Play.");
        }

        // ------------------------------------------------------------- arena

        private static void BuildArena(Material ground, Material wall, Material obstacle)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground";
            floor.transform.localScale = new Vector3(3.4f, 1f, 3.4f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = ground;

            const float half = 17f;
            CreateBox("Wall_North", new Vector3(0f, 1.5f, half), new Vector3(34f, 3f, 1f), wall, null);
            CreateBox("Wall_South", new Vector3(0f, 1.5f, -half), new Vector3(34f, 3f, 1f), wall, null);
            CreateBox("Wall_East", new Vector3(half, 1.5f, 0f), new Vector3(1f, 3f, 34f), wall, null);
            CreateBox("Wall_West", new Vector3(-half, 1.5f, 0f), new Vector3(1f, 3f, 34f), wall, null);

            GameObject root = new GameObject("Obstacles");

            Vector3[] blocks =
            {
                new Vector3(-5.5f, 0.75f, 3.5f),
                new Vector3(4.5f, 0.75f, 6.5f),
                new Vector3(8f, 0.75f, -1.5f),
                new Vector3(-9f, 0.75f, -6f),
                new Vector3(0f, 0.75f, -7.5f),
                new Vector3(-13f, 0.75f, 12f),
                new Vector3(13f, 0.75f, 12f),
            };

            for (int i = 0; i < blocks.Length; i++)
            {
                CreateBox("Obstacle_" + i, blocks[i], new Vector3(2f, 1.5f, 2f), obstacle, root.transform);
            }
        }

        /// Flat coloured pads so each monster's territory reads at a glance.
        private static void BuildZonePads()
        {
            GameObject root = new GameObject("ZonePads");

            CreatePad(root.transform, "Pad_Rusher", new Vector3(-8f, 0.02f, 8f), 12f,
                GetOrCreateMaterial("PadRusher", new Color(0.32f, 0.17f, 0.19f)));
            CreatePad(root.transform, "Pad_Caster", new Vector3(10f, 0.02f, 8f), 12f,
                GetOrCreateMaterial("PadCaster", new Color(0.23f, 0.18f, 0.33f)));
            CreatePad(root.transform, "Pad_Brute", new Vector3(8f, 0.02f, -9f), 12f,
                GetOrCreateMaterial("PadBrute", new Color(0.17f, 0.22f, 0.29f)));
        }

        private static void CreatePad(Transform parent, string name, Vector3 center, float size, Material material)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = name;
            pad.transform.SetParent(parent, true);
            pad.transform.position = center;
            pad.transform.localScale = new Vector3(size, 0.04f, size);
            pad.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(pad.GetComponent<BoxCollider>());
        }

        // ------------------------------------------------------------ player

        private static GameObject BuildPlayer(Material material)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.1f, -11f);
            player.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = Vector3.zero;
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.35f;

            AddMarker(player.transform, material, new Vector3(0f, 0.2f, 0.55f),
                new Vector3(0.25f, 0.25f, 0.5f));

            Health health = player.AddComponent<Health>();
            ConfigureHealth(health, 6, 0.8f, Team.Player, "Vela");

            player.AddComponent<IsometricPlayerController>();

            PlayerCombat combat = player.AddComponent<PlayerCombat>();
            combat.ResetToDefaults();
            EditorUtility.SetDirty(combat);

            player.AddComponent<PlayerPickup>();
            return player;
        }

        // ---------------------------------------------------------- monsters

        private static void BuildMonsters(Material rusherMaterial, Material casterMaterial, Material bruteMaterial)
        {
            GameObject root = new GameObject("Monsters");

            // Variant 1 — Rushers: a pack of fast, fragile lungers.
            BuildRusher(root.transform, rusherMaterial, "Rusher_0", new Vector3(-9f, 1.1f, 7f));
            BuildRusher(root.transform, rusherMaterial, "Rusher_1", new Vector3(-5f, 1.1f, 10f));

            // Variant 2 — Casters: keep their distance and shoot.
            BuildCaster(root.transform, casterMaterial, "Caster_0", new Vector3(9f, 1.1f, 9f));
            BuildCaster(root.transform, casterMaterial, "Caster_1", new Vector3(13f, 1.1f, 5f));

            // Variant 3 — Brute: the armoured wall with the big slam.
            BuildBrute(root.transform, bruteMaterial, "Brute_0", new Vector3(8f, 1.6f, -9f));
        }

        private static void BuildRusher(Transform parent, Material material, string name, Vector3 position)
        {
            GameObject body = CreateBody(parent, name, position, material, PrimitiveType.Capsule,
                new Vector3(0.9f, 0.9f, 0.9f), 1.8f, 0.45f);

            AddMarker(body.transform, material, new Vector3(0f, 0.45f, 0.5f), new Vector3(0.2f, 0.2f, 0.5f));

            ConfigureHealth(body.AddComponent<Health>(), 4, 0.05f, Team.Monster, "Rusher");

            LootDropper dropper = body.AddComponent<LootDropper>();
            ConfigureLoot(dropper, 1, new[]
            {
                Roll(LootKind.Gold, 1f, 1, 2),
                Roll(LootKind.Potion, 0.35f, 1, 1),
                Roll(LootKind.Shard, 0.10f, 1, 1),
            });

            RusherMonster brain = body.AddComponent<RusherMonster>();
            ConfigureMonster(brain, "Rusher", new Color(0.85f, 0.30f, 0.34f), 15f, 8f);
        }

        private static void BuildCaster(Transform parent, Material material, string name, Vector3 position)
        {
            GameObject body = CreateBody(parent, name, position, material, PrimitiveType.Capsule,
                new Vector3(0.85f, 1.05f, 0.85f), 2f, 0.45f);

            AddMarker(body.transform, material, new Vector3(0f, 1.15f, 0f), new Vector3(0.45f, 0.45f, 0.45f),
                PrimitiveType.Sphere);
            AddMarker(body.transform, material, new Vector3(0f, 0.3f, 0.5f), new Vector3(0.18f, 0.18f, 0.5f));

            ConfigureHealth(body.AddComponent<Health>(), 3, 0.05f, Team.Monster, "Caster");

            LootDropper dropper = body.AddComponent<LootDropper>();
            ConfigureLoot(dropper, 1, new[]
            {
                Roll(LootKind.Gold, 1f, 1, 1),
                Roll(LootKind.Shard, 0.35f, 1, 1),
                Roll(LootKind.Potion, 0.25f, 1, 1),
            });

            CasterMonster brain = body.AddComponent<CasterMonster>();
            ConfigureMonster(brain, "Caster", new Color(0.55f, 0.35f, 0.85f), 17f, 9f);
        }

        private static void BuildBrute(Transform parent, Material material, string name, Vector3 position)
        {
            GameObject body = CreateBody(parent, name, position, material, PrimitiveType.Cube,
                new Vector3(2.2f, 3f, 2.2f), 3f, 1.05f);

            AddMarker(body.transform, material, new Vector3(0f, 0.15f, 0.62f), new Vector3(0.5f, 0.3f, 0.35f));

            ConfigureHealth(body.AddComponent<Health>(), 14, 0.05f, Team.Monster, "Brute");

            LootDropper dropper = body.AddComponent<LootDropper>();
            ConfigureLoot(dropper, 2, new[]
            {
                Roll(LootKind.Gold, 1f, 2, 4),
                Roll(LootKind.Shard, 1f, 1, 1),
                Roll(LootKind.Potion, 0.8f, 1, 2),
            });

            BruteMonster brain = body.AddComponent<BruteMonster>();
            ConfigureMonster(brain, "Brute", new Color(0.32f, 0.45f, 0.62f), 13f, 12f);
        }

        private static GameObject CreateBody(Transform parent, string name, Vector3 position, Material material,
            PrimitiveType shape, Vector3 scale, float controllerHeight, float controllerRadius)
        {
            GameObject body = GameObject.CreatePrimitive(shape);
            body.name = name;
            body.transform.SetParent(parent, true);
            body.transform.position = position;
            body.transform.localScale = scale;
            body.GetComponent<MeshRenderer>().sharedMaterial = material;

            Collider primitiveCollider = body.GetComponent<Collider>();
            if (primitiveCollider != null) Object.DestroyImmediate(primitiveCollider);

            CharacterController controller = body.AddComponent<CharacterController>();
            controller.height = controllerHeight;
            controller.radius = controllerRadius;
            controller.center = Vector3.zero;
            controller.stepOffset = 0.35f;

            return body;
        }

        private static void AddMarker(Transform parent, Material material, Vector3 localPosition, Vector3 localScale)
        {
            AddMarker(parent, material, localPosition, localScale, PrimitiveType.Cube);
        }

        private static void AddMarker(Transform parent, Material material, Vector3 localPosition,
            Vector3 localScale, PrimitiveType shape)
        {
            GameObject marker = GameObject.CreatePrimitive(shape);
            marker.name = "Marker";
            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null) Object.DestroyImmediate(markerCollider);

            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = localScale;
            marker.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        // -------------------------------------------------------------- loot

        private static void BuildStarterLoot()
        {
            GameObject root = new GameObject("StarterLoot");

            CreateLootPoint(root.transform, "Potion_0", new Vector3(-3f, 0.5f, -8f), LootKind.Potion, 12f);
            CreateLootPoint(root.transform, "Potion_1", new Vector3(3.5f, 0.5f, -4f), LootKind.Potion, 16f);
            CreateLootPoint(root.transform, "Gold_0", new Vector3(0f, 0.5f, -5.5f), LootKind.Gold, 10f);
        }

        private static void CreateLootPoint(Transform parent, string name, Vector3 position, LootKind kind,
            float respawnSeconds)
        {
            GameObject point = new GameObject(name);
            point.transform.SetParent(parent, true);
            point.transform.position = position;

            LootSpawnPoint spawner = point.AddComponent<LootSpawnPoint>();
            SerializedObject serialized = new SerializedObject(spawner);
            serialized.FindProperty("kind").enumValueIndex = (int)kind;
            serialized.FindProperty("respawnSeconds").floatValue = respawnSeconds;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------- scene extras

        private static Camera SetUpCamera(Transform target)
        {
            Camera camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.10f);

            IsometricCameraRig rig = camera.GetComponent<IsometricCameraRig>();
            if (rig == null) rig = camera.gameObject.AddComponent<IsometricCameraRig>();
            rig.Target = target;

            return camera;
        }

        private static void SetUpLight()
        {
            Light light = Object.FindFirstObjectByType<Light>();
            if (light == null)
            {
                light = new GameObject("Directional Light").AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
        }

        // ----------------------------------------------------- serialization

        private static void ConfigureHealth(Health health, int maxHealth, float iframes, Team team, string label)
        {
            SerializedObject serialized = new SerializedObject(health);
            serialized.FindProperty("maxHealth").intValue = maxHealth;
            serialized.FindProperty("invulnerabilityAfterHit").floatValue = iframes;
            serialized.FindProperty("team").enumValueIndex = (int)team;
            serialized.FindProperty("displayName").stringValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMonster(MonsterBase monster, string label, Color tint, float detectionRange,
            float respawnSeconds)
        {
            SerializedObject serialized = new SerializedObject(monster);
            serialized.FindProperty("displayName").stringValue = label;
            serialized.FindProperty("baseTint").colorValue = tint;
            serialized.FindProperty("detectionRange").floatValue = detectionRange;
            serialized.FindProperty("respawnSeconds").floatValue = respawnSeconds;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector4 Roll(LootKind kind, float chance, int min, int max)
        {
            return new Vector4((int)kind, chance, min, max);
        }

        private static void ConfigureLoot(LootDropper dropper, int guaranteedFirstRow, Vector4[] rows)
        {
            SerializedObject serialized = new SerializedObject(dropper);
            serialized.FindProperty("guaranteedFirstRow").intValue = guaranteedFirstRow;

            SerializedProperty table = serialized.FindProperty("table");
            table.arraySize = rows.Length;

            for (int i = 0; i < rows.Length; i++)
            {
                SerializedProperty row = table.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("kind").enumValueIndex = (int)rows[i].x;
                row.FindPropertyRelative("chance").floatValue = rows[i].y;
                row.FindPropertyRelative("minCount").intValue = (int)rows[i].z;
                row.FindPropertyRelative("maxCount").intValue = (int)rows[i].w;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ----------------------------------------------------------- helpers

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material,
            Transform parent)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            if (parent != null) box.transform.SetParent(parent, true);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static void EnsureFolder(string folderName)
        {
            if (!AssetDatabase.IsValidFolder("Assets/" + folderName))
            {
                AssetDatabase.CreateFolder("Assets", folderName);
            }
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                return existing;
            }

            Material material = new Material(FindLitShader());
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Shader FindLitShader()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null) return urpLit;
            }

            Shader standard = Shader.Find("Standard");
            return standard != null ? standard : Shader.Find("Diffuse");
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(entry => entry.path == ScenePath)) return;

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
