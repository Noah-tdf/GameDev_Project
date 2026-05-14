using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Ubuntu City → Make Selected Tilemap One-Way Platform
/// Configures the selected tilemap GameObject so the player can jump up
/// through it from below and land on top. Works with the existing
/// PlayerMovement.ConfigureUpperPlatformTilemaps() which auto-detects any
/// tilemap whose name (or an ancestor's name) contains "upperplatform".
/// </summary>
public static class MakeUpperPlatform
{
    const string UpperPlatformTag = "UpperPlatform";

    [MenuItem("Ubuntu City/Make Selected Tilemap One-Way Platform")]
    public static void Run()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("One-Way Platform",
                "Select the platform tilemap GameObject in the Hierarchy first " +
                "(the tilemap whose tiles the player should jump up through and land on).",
                "OK");
            return;
        }

        var tilemaps = new System.Collections.Generic.HashSet<Tilemap>();
        foreach (GameObject root in Selection.gameObjects)
        {
            foreach (Tilemap tm in root.GetComponentsInChildren<Tilemap>(true))
                if (tm != null) tilemaps.Add(tm);
        }

        if (tilemaps.Count == 0)
        {
            EditorUtility.DisplayDialog("One-Way Platform",
                "Selected object(s) contained no Tilemap components. " +
                "Select the platform tilemap GameObject (or a parent group containing tilemaps).",
                "OK");
            return;
        }

        int converted = 0;
        foreach (Tilemap tilemap in tilemaps)
        {
            GameObject go = tilemap.gameObject;

            if (!NameChainContains(go.transform, "upperplatform"))
            {
                Undo.RecordObject(go, "Rename to UpperPlatform");
                go.name = $"{UpperPlatformTag}_{go.name}";
            }

            TilemapCollider2D col = go.GetComponent<TilemapCollider2D>();
            if (col == null) col = Undo.AddComponent<TilemapCollider2D>(go);
            Undo.RecordObject(col, "Configure One-Way Collider");
            col.isTrigger       = false;
            col.usedByEffector  = true;

            PlatformEffector2D eff = go.GetComponent<PlatformEffector2D>();
            if (eff == null) eff = Undo.AddComponent<PlatformEffector2D>(go);
            Undo.RecordObject(eff, "Configure One-Way Effector");
            eff.useOneWay         = true;
            eff.useOneWayGrouping = true;
            eff.surfaceArc        = 170f;
            eff.sideArc           = 0f;

            converted++;
        }

        if (converted > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        EditorUtility.DisplayDialog("One-Way Platform",
            converted == 0
                ? "Nothing converted — make sure you selected a GameObject with a Tilemap component."
                : $"Converted {converted} tilemap(s) to one-way platforms.\n\n" +
                  "Press Play and test: jump up through them from below, land on top. " +
                  "If it still feels solid from below, check that no other collider (without the effector) covers the same cells.",
            "OK");
    }

    static bool NameChainContains(Transform t, string needle)
    {
        string n = needle.ToLowerInvariant();
        for (Transform cur = t; cur != null; cur = cur.parent)
        {
            string normalized = cur.name.Replace(" ", "").Replace("_", "").ToLowerInvariant();
            if (normalized.Contains(n)) return true;
        }
        return false;
    }
}
