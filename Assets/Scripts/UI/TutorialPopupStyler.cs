using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Normalises every tutorial / dialogue popup in the game so they all look like
/// the pause menu: Astron-Bold font, a wide rect so text flows horizontally,
/// and a font size that scales DOWN for longer messages so a 60-character tip
/// reads at roughly the same visual width as a 20-character one.
///
/// Auto-spawns in gameplay scenes; re-checks each frame so runtime-built popups
/// (e.g. the HealthBarPopUp built in PlayerMovement.Start) get caught too.
/// </summary>
public class TutorialPopupStyler : MonoBehaviour
{
    private static TMP_FontAsset cachedFontAsset;

    private readonly HashSet<TMP_Text> styled = new HashSet<TMP_Text>();
    private const float TargetWidth = 900f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "MainMenu" || scene == "Store") return;
        if (FindAnyObjectByType<TutorialPopupStyler>() != null) return;
        new GameObject("TutorialPopupStyler").AddComponent<TutorialPopupStyler>();
    }

    private void Update()
    {
        TMP_Text[] all = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text label in all)
        {
            if (label == null || styled.Contains(label)) continue;
            if (!IsTutorialOrDialogue(label)) continue;
            StyleLabel(label);
            styled.Add(label);
        }
    }

    private static bool IsTutorialOrDialogue(TMP_Text label)
    {
        // Walk up the parent chain. We match popups by container/object name so
        // we never touch HUD text like Credits or the health bar.
        for (Transform t = label.transform; t != null; t = t.parent)
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains("popup")) return true;
            if (n.Contains("dialogue") || n.Contains("dialog")) return true;
            if (n.Contains("discovery")) return true;
        }
        return false;
    }

    private void StyleLabel(TMP_Text label)
    {
        TMP_FontAsset font = ResolveFont();
        if (font != null) label.font = font;

        // Pick font size from text length so long tips visually match short ones.
        int len = label.text != null ? label.text.Length : 0;
        float fontSize;
        if      (len > 70) fontSize = 24f;
        else if (len > 45) fontSize = 28f;
        else if (len > 25) fontSize = 34f;
        else               fontSize = 40f;
        label.fontSize = fontSize;

        label.enableWordWrapping = true;
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Overflow;

        // Make the rect wide enough that text doesn't stack vertically.
        RectTransform rt = label.rectTransform;
        Vector2 size = rt.sizeDelta;
        if (size.x < TargetWidth) size.x = TargetWidth;
        size.y = Mathf.Max(size.y, fontSize * 2.4f); // room for ~2 wrapped lines
        rt.sizeDelta = size;

        // Widen any direct ancestor RectTransform that's narrower than the text width
        // (the popup's canvas/panel often clips otherwise).
        Transform cur = rt.parent;
        int hops = 0;
        while (cur != null && hops < 3)
        {
            RectTransform parentRT = cur as RectTransform;
            if (parentRT != null)
            {
                Vector2 ps = parentRT.sizeDelta;
                if (ps.x > 0f && ps.x < TargetWidth) { ps.x = TargetWidth; parentRT.sizeDelta = ps; }
            }
            cur = cur.parent;
            hops++;
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
