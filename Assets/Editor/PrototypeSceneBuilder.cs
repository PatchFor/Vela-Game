using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Vela.CameraRig;
using Vela.Gameplay;
using Vela.Player;

namespace Vela.EditorTools
{
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

            var groundMaterial = GetOrCreateMaterial("Ground", new Color(0.20f, 0.23f, 0.29f));
            var wallMaterial = GetOrCreateMaterial("Wall", new Color(0.13f, 0.15f, 0.20f));
            var obstacleMaterial = GetOrCreateMaterial("Obstacle", new Color(0.31f, 0.35f, 0.44f));
            var playerMaterial = GetOrCreateMaterial("Player", new Color(0.95f, 0.72f, 0.28f));
            var shardMaterial = GetOrCreateMaterial("Shard", new Color(0.35f, 0.85f, 0.95f));

            BuildArena(groundMaterial, wallMaterial, obstacleMaterial);
            var player = BuildPlayer(playerMaterial);
            var camera = SetUpCamera(player.transform);
            player.GetComponent<IsometricPlayerController>().CameraPivot = camera.transform;

            SetUpLight();
            BuildShards(shardMaterial);

            new GameObject("GameManager").AddComponent<PrototypeGameManager>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            Debug.Log($"Vela: prototype scene built at {ScenePath}. Press Play to try it.");
        }

        private static void BuildArena(Material ground, Material wall, Material obstacle)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground";
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = ground;

            const float half = 15f;
            CreateBox("Wall_North", new Vector3(0f, 1f, half), new Vector3(30f, 2f, 1f), wall);
            CreateBox("Wall_South", new Vector3(0f, 1f, -half), new Vector3(30f, 2f, 1f), wall);
            CreateBox("Wall_East", new Vector3(half, 1f, 0f), new Vector3(1f, 2f, 30f), wall);
            CreateBox("Wall_West", new Vector3(-half, 1f, 0f), new Vector3(1f, 2f, 30f), wall);

            var blocks = new[]
            {
                new Vector3(-5f, 0.75f, 4f),
                new Vector3(3.5f, 0.75f, 6.5f),
                new Vector3(7f, 0.75f, -2f),
                new Vector3(-8f, 0.75f, -6f),
                new Vector3(0f, 0.75f, -8f),
                new Vector3(-2.5f, 0.75f, 0.5f),
            };

            for (var i = 0; i < blocks.Length; i++)
            {
                CreateBox($"Obstacle_{i}", blocks[i], new Vector3(2f, 1.5f, 2f), obstacle);
            }
        }

        private static GameObject BuildPlayer(Material material)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.1f, -10f);
            player.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());

            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = Vector3.zero;
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.35f;

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "FacingMarker";
            Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.2f, 0.55f);
            nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.4f);
            nose.GetComponent<MeshRenderer>().sharedMaterial = material;

            player.AddComponent<IsometricPlayerController>();
            return player;
        }

        private static Camera SetUpCamera(Transform target)
        {
            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = 9f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.10f);

            var rig = camera.GetComponent<IsometricCameraRig>();
            if (rig == null) rig = camera.gameObject.AddComponent<IsometricCameraRig>();
            rig.Target = target;

            return camera;
        }

        private static void SetUpLight()
        {
            var light = Object.FindFirstObjectByType<Light>();
            if (light == null)
            {
                light = new GameObject("Directional Light").AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
        }

        private static void BuildShards(Material material)
        {
            var positions = new[]
            {
                new Vector3(-10f, 1f, 8f),
                new Vector3(0f, 1f, 10f),
                new Vector3(9f, 1f, 7f),
                new Vector3(11f, 1f, -6f),
                new Vector3(-11f, 1f, -9f),
                new Vector3(4f, 1f, 1f),
                new Vector3(-6f, 1f, -2f),
            };

            var root = new GameObject("Shards");

            for (var i = 0; i < positions.Length; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shard.name = $"Shard_{i}";
                shard.transform.SetParent(root.transform, true);
                shard.transform.position = positions[i];
                shard.transform.localScale = Vector3.one * 0.7f;
                shard.GetComponent<MeshRenderer>().sharedMaterial = material;
                shard.GetComponent<SphereCollider>().isTrigger = true;
                shard.AddComponent<Collectible>();
            }
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static void EnsureFolder(string folderName)
        {
            if (!AssetDatabase.IsValidFolder($"Assets/{folderName}"))
            {
                AssetDatabase.CreateFolder("Assets", folderName);
            }
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                return existing;
            }

            var material = new Material(FindLitShader()) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Shader FindLitShader()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                var urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null) return urpLit;
            }

            return Shader.Find("Standard") ?? Shader.Find("Diffuse");
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(entry => entry.path == ScenePath)) return;

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
