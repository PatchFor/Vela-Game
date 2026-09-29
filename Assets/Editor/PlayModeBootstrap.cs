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
                if (!IsOutdated() || !AskToRebuild())
                {
                    ApplyStartScene();
                    return;
                }
            }

            // No scene yet (or it's outdated): stop, build it, then start Play again.
            EditorApplication.isPlaying = false;
            Debug.Log("Vela: building the combat scene. Play will start automatically.");
            EditorApplication.delayCall += () =>
            {
                try
                {
                    PrototypeSceneBuilder.BuildScene();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    EditorUtility.DisplayDialog("Vela", "Building the combat scene failed:\n\n" + e.Message +
                        "\n\nSee the Console for details.", "OK");
                    return;
                }

                ApplyStartScene();
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) EditorApplication.EnterPlaymode();
            };
        }

        private static int SavedVersion()
        {
            try
            {
                return System.IO.File.Exists(PrototypeSceneBuilder.VersionFile)
                    ? int.Parse(System.IO.File.ReadAllText(PrototypeSceneBuilder.VersionFile).Trim())
                    : 0;
            }
            catch (System.Exception)
            {
                return 0;
            }
        }

        private static bool IsOutdated() => SavedVersion() < PrototypeSceneBuilder.SceneVersion;

        /// Asks once per scene version. "Keep" is remembered so it doesn't nag every Play.
        private static bool AskToRebuild()
        {
            var key = "Vela.SkipRebuild." + PrototypeSceneBuilder.SceneVersion;
            if (EditorPrefs.GetBool(key, false)) return false;

            var rebuild = EditorUtility.DisplayDialog("Vela: combat scene is out of date",
                "The code has new content (sounds, perfect dodge, input commands, and more).\n\n" +
                "Rebuild CombatPrototype.unity now? Changes you made to that scene by hand will be lost.\n" +
                "(Config assets in Assets/Config are kept.)",
                "Rebuild", "Keep old scene");
            if (!rebuild) EditorPrefs.SetBool(key, true);
            return rebuild;
        }
    }
}
