using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vela.EditorTools
{
    /// Pressing Play always runs the combat prototype, whatever scene is open.
    /// If the scene hasn't been generated yet, it is built first and Play starts afterwards.
    [InitializeOnLoad]
    public static class PlayModeBootstrap
    {
        private const string ScenePath = "Assets/Scenes/CombatPrototype.unity";
        private const string DisabledKey = "Vela.PlayModeBootstrap.Disabled";
        private const string MenuPath = "Vela/Always Play Combat Scene";

        static PlayModeBootstrap()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += ApplyStartScene;
        }

        private static bool Enabled => !EditorPrefs.GetBool(DisabledKey, false);

        [MenuItem(MenuPath, priority = 40)]
        private static void Toggle()
        {
            EditorPrefs.SetBool(DisabledKey, Enabled);
            ApplyStartScene();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void ApplyStartScene()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorSceneManager.playModeStartScene = Enabled ? scene : null;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode || !Enabled) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                ApplyStartScene();
                return;
            }

            // No scene yet: stop, build it, then start Play again.
            EditorApplication.isPlaying = false;
            Debug.Log("Vela: combat scene not found, building it now. Play will start automatically.");
            EditorApplication.delayCall += () =>
            {
                PrototypeSceneBuilder.BuildScene();
                ApplyStartScene();
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) EditorApplication.EnterPlaymode();
            };
        }
    }
}
