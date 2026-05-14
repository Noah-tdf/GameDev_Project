using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Applies the Astron-Bold sci-fi font to every TextMeshProUGUI on the Main Menu
/// (and Store) screens at runtime — no need to manually wire fonts in the scene.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MainMenuStyler : MonoBehaviour
{
    private static TMP_FontAsset cachedFontAsset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene != "MainMenu" && scene != "Store") return;
        if (FindAnyObjectByType<MainMenuStyler>() != null) return;
        new GameObject("MainMenuStyler").AddComponent<MainMenuStyler>();
    }

    private void Awake()
    {
        TMP_FontAsset font = ResolveFont();
        if (font == null)
        {
            Debug.LogWarning("[MainMenuStyler] No sci-fi font found in Assets/Fonts/ (looked for Astron-Bold.otf, Astron.otf, PressStart2P, BlackOpsOne).");
            return;
        }

        TextMeshProUGUI[] labels = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label == null) continue;
            label.font = font;
        }
    }

    private static TMP_FontAsset ResolveFont()
    {
        if (cachedFontAsset != null) return cachedFontAsset;
#if UNITY_EDITOR
        string[] candidates = {
            "Assets/Fonts/Astron-Bold.otf",
            "Assets/Fonts/Astron.otf",
            "Assets/Fonts/PressStart2P-Regular.ttf",
            "Assets/Fonts/Pixeled.ttf",
            "Assets/Fonts/BlackOpsOne-Regular.ttf",
        };
        Font source = null;
        foreach (string path in candidates)
        {
            source = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (source != null) break;
        }
        if (source != null)
        {
            cachedFontAsset = TMP_FontAsset.CreateFontAsset(source);
            if (cachedFontAsset != null) cachedFontAsset.name = source.name + " SDF (Runtime)";
        }
#endif
        return cachedFontAsset;
    }
}
