using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirulentVentures.Editor
{
    [InitializeOnLoad]
    public static class PlayFromTitle
    {
        private const string ToggleKey = "VirulentVentures_PlayFromTitleEnabled";
        private const string MenuPath = "Virulent Ventures/Play From Title Scene";
        private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";

        static PlayFromTitle()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(MenuPath)]
        private static void TogglePlayFromTitle()
        {
            bool currentState = EditorPrefs.GetBool(ToggleKey, true);
            EditorPrefs.SetBool(ToggleKey, !currentState);
            Debug.Log($"Play From Title Scene {(EditorPrefs.GetBool(ToggleKey) ? "enabled" : "disabled")}.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateTogglePlayFromTitle()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(ToggleKey, true));
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && EditorPrefs.GetBool(ToggleKey, true))
            {
                var currentScene = SceneManager.GetActiveScene();
                if (currentScene.path != TitleScenePath)
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(TitleScenePath);
                        Debug.Log($"PlayFromTitle: Loading {TitleScenePath} for play mode.");
                    }
                    else
                    {
                        Debug.LogWarning("PlayFromTitle: Scene switch canceled by user.");
                    }
                }
            }
        }
    }
}