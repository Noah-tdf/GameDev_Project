using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ubuntu City → Fix Scene
/// Ensures all placeholder sprites use a saved asset (not runtime textures),
/// restores building colours, camera, ground, and platforms.
/// </summary>
public static class SceneFixer
{
    private const string WhiteSquarePath = "Assets/Art/Placeholders/WhiteSquare.png";

    [MenuItem("Ubuntu City/Fix Scene")]
    public static void Fix()
    {
        // 1. Guarantee the white square sprite asset exists on disk
        Sprite square = EnsureWhiteSquare();

        // 2. Open Level1
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);

        // 3. Camera — sky blue background
        FixCamera(scene);

        // 4. Sky layer — big blue rect behind everything
        ApplySprite(scene, "SkyLayer", square, new Color(0.529f, 0.808f, 0.922f),
                    new Vector3(0f, 2f, 10f), new Vector3(80f, 14f, 1f), -20);

        // 5. Building blocks inside BuildingsLayer
        RestoreBuildings(scene, square);

        // 6. Lamp posts inside PropsLayer
        RestoreProps(scene, square);

        // 7. Ground
        ApplySprite(scene, "Ground", square, new Color(0.55f, 0.50f, 0.45f),
                    new Vector3(25f, -4f, 0f), new Vector3(70f, 2f, 1f), -2);

        // 8. Platforms
        Color brickColor = new Color(0.60f, 0.42f, 0.28f);
        for (int i = 1; i <= 7; i++)
            ApplySprite(scene, "Platform_0" + i, square, brickColor, null, null, 0);

        // 9. Road stripe child of Ground
        ApplySprite(scene, "RoadStripe", square, new Color(0.65f, 0.60f, 0.55f), null, null, -1);

        // 10. Player start position
        GameObject luca = Find(scene, "Luca");
        if (luca != null) luca.transform.position = new Vector3(2f, -2f, 0f);

        // 11. Enemy tint reset
        for (int i = 1; i <= 5; i++)
        {
            GameObject e = Find(scene, "UbuntyWalker_0" + i);
            if (e == null) continue;
            SpriteRenderer sr = e.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Ubuntu City: Scene fixed and saved!");
    }

    // ── Camera ────────────────────────────────────────────────────────────────
    private static void FixCamera(Scene scene)
    {
        GameObject go = Find(scene, "Main Camera");
        if (go == null) return;
        Camera cam = go.GetComponent<Camera>();
        if (cam == null) return;
        cam.backgroundColor  = new Color(0.529f, 0.808f, 0.922f);
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.orthographicSize = 5f;
    }

    // ── Buildings ─────────────────────────────────────────────────────────────
    private static void RestoreBuildings(Scene scene, Sprite square)
    {
        Color[] palette = {
            new Color(0.98f, 0.80f, 0.80f),
            new Color(0.80f, 0.90f, 0.98f),
            new Color(0.98f, 0.95f, 0.70f),
            new Color(0.95f, 0.95f, 0.95f),
            new Color(0.82f, 0.92f, 0.78f),
        };

        GameObject buildings = Find(scene, "BuildingsLayer");
        if (buildings == null) return;

        // Reset parent position so children are visible from the start
        buildings.transform.position   = new Vector3(0f, -1f, 8f);
        buildings.transform.localScale = Vector3.one;
        SpriteRenderer parentSr = buildings.GetComponent<SpriteRenderer>();
        if (parentSr != null) { parentSr.sprite = null; parentSr.color = Color.clear; }

        int idx = 0;
        foreach (Transform child in buildings.GetComponentsInChildren<Transform>(true))
        {
            if (child == buildings.transform) continue;
            if (!child.name.StartsWith("House_")) continue;
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            sr.sprite       = square;
            sr.color        = palette[idx % palette.Length];
            sr.sortingOrder = -15;
            idx++;
        }
    }

    // ── Lamp posts ────────────────────────────────────────────────────────────
    private static void RestoreProps(Scene scene, Sprite square)
    {
        GameObject props = Find(scene, "PropsLayer");
        if (props == null) return;

        props.transform.position   = new Vector3(0f, -1f, 5f);
        props.transform.localScale = Vector3.one;
        SpriteRenderer parentSr = props.GetComponent<SpriteRenderer>();
        if (parentSr != null) { parentSr.sprite = null; parentSr.color = Color.clear; }

        foreach (Transform child in props.GetComponentsInChildren<Transform>(true))
        {
            if (child == props.transform) continue;
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            sr.sprite = square;

            if (child.name.StartsWith("LampPole_"))
                sr.color = new Color(0.25f, 0.22f, 0.20f);
            else if (child.name.StartsWith("LampHead_"))
                sr.color = new Color(0.95f, 0.85f, 0.50f);
        }
    }

    // ── Generic sprite apply ──────────────────────────────────────────────────
    private static void ApplySprite(Scene scene, string name, Sprite sprite, Color color,
                                     Vector3? pos, Vector3? scale, int sortOrder)
    {
        GameObject go = Find(scene, name);
        if (go == null) return;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite       = sprite;
            sr.color        = color;
            sr.sortingOrder = sortOrder;
        }
        if (pos.HasValue)   go.transform.position   = pos.Value;
        if (scale.HasValue) go.transform.localScale  = scale.Value;
    }

    // ── White square sprite asset ─────────────────────────────────────────────
    private static Sprite EnsureWhiteSquare()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquarePath);
        if (existing != null) return existing;

        Directory.CreateDirectory("Assets/Art/Placeholders");

        // 4×4 white PNG
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(WhiteSquarePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(WhiteSquarePath);

        TextureImporter ti = AssetImporter.GetAtPath(WhiteSquarePath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType         = TextureImporterType.Sprite;
            ti.spriteImportMode    = SpriteImportMode.Single;
            ti.filterMode          = FilterMode.Point;
            ti.mipmapEnabled       = false;
            ti.alphaIsTransparency = true;
            ti.spritePixelsPerUnit = 4;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquarePath);
    }

    // ── Scene search ─────────────────────────────────────────────────────────
    private static GameObject Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            var t = root.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(x => x.name == name);
            if (t != null) return t.gameObject;
        }
        return null;
    }
}
