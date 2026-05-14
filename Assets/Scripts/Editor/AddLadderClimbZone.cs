using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Ubuntu City → Add Ladder Climb Zone
/// Creates a single continuous BoxCollider2D trigger zone covering the painted
/// cells on LadderTilemap. PlayerMovement detects any trigger whose name (or
/// any ancestor's name) contains "ladder" — so this zone makes the entire
/// ladder climbable, with no gaps between painted cells.
/// </summary>
public static class AddLadderClimbZone
{
    const string LadderTilemapName = "LadderTilemap";
    const string ZoneName          = "LadderClimbZone";
    const float  HorizontalPadding = 0.25f;
    const float  VerticalPadding   = 0.5f;

    [MenuItem("Ubuntu City/Add Ladder Climb Zone")]
    public static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Ladder Climb Zone",
                "Open the scene with the ladder first (e.g. Level1).", "OK");
            return;
        }

        Tilemap ladderTilemap = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Tilemap tm in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (tm.name == LadderTilemapName)
                {
                    ladderTilemap = tm;
                    break;
                }
            }
            if (ladderTilemap != null) break;
        }

        if (ladderTilemap == null)
        {
            EditorUtility.DisplayDialog("Ladder Climb Zone",
                $"Couldn't find a Tilemap named '{LadderTilemapName}' in the active scene.", "OK");
            return;
        }

        ladderTilemap.CompressBounds();
        BoundsInt cellBounds = ladderTilemap.cellBounds;

        bool hasAny = false;
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
        {
            for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
            {
                if (!ladderTilemap.HasTile(new Vector3Int(x, y, 0))) continue;
                hasAny = true;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (!hasAny)
        {
            EditorUtility.DisplayDialog("Ladder Climb Zone",
                $"'{LadderTilemapName}' has no painted tiles. Paint your ladder tiles first, then run this again.", "OK");
            return;
        }

        Vector3 bottomLeftWorld = ladderTilemap.CellToWorld(new Vector3Int(minX,     minY,     0));
        Vector3 topRightWorld   = ladderTilemap.CellToWorld(new Vector3Int(maxX + 1, maxY + 1, 0));
        Vector3 worldCenter     = (bottomLeftWorld + topRightWorld) * 0.5f;
        Vector3 worldSize       = topRightWorld - bottomLeftWorld;
        worldSize.x = Mathf.Abs(worldSize.x) + HorizontalPadding * 2f;
        worldSize.y = Mathf.Abs(worldSize.y) + VerticalPadding   * 2f;

        Transform parent = ladderTilemap.transform.parent != null
            ? ladderTilemap.transform.parent
            : ladderTilemap.transform;

        Transform existing = parent.Find(ZoneName);
        GameObject zoneGO;
        if (existing != null)
        {
            zoneGO = existing.gameObject;
            Undo.RecordObject(zoneGO, "Update Ladder Climb Zone");
        }
        else
        {
            zoneGO = new GameObject(ZoneName);
            Undo.RegisterCreatedObjectUndo(zoneGO, "Create Ladder Climb Zone");
            zoneGO.transform.SetParent(parent, worldPositionStays: false);
        }

        zoneGO.transform.position   = new Vector3(worldCenter.x, worldCenter.y, 0f);
        zoneGO.transform.rotation   = Quaternion.identity;
        zoneGO.transform.localScale = Vector3.one;

        BoxCollider2D box = zoneGO.GetComponent<BoxCollider2D>();
        if (box == null) box = Undo.AddComponent<BoxCollider2D>(zoneGO);
        Undo.RecordObject(box, "Configure Ladder Climb Zone");
        box.isTrigger = true;
        box.offset    = Vector2.zero;
        box.size      = new Vector2(worldSize.x, worldSize.y);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = zoneGO;

        EditorUtility.DisplayDialog("Ladder Climb Zone",
            $"Created/updated '{ZoneName}' under '{parent.name}'.\n" +
            $"Center: ({worldCenter.x:F2}, {worldCenter.y:F2})\n" +
            $"Size:   ({worldSize.x:F2} × {worldSize.y:F2})\n\n" +
            "Press Play and try climbing. If the zone is too short/tall, adjust the BoxCollider2D size in the Inspector.",
            "OK");
    }
}
