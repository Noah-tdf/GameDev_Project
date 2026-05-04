using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayModeStartScene
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    static PlayModeStartScene()
    {
        SceneAsset mainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (mainMenuScene != null)
        {
            EditorSceneManager.playModeStartScene = mainMenuScene;
        }
    }
}
