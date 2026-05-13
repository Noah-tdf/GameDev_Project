using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Imports all sprite sheets from LEVEL 3 ASSETS into the FinalLevel tile palette.
/// Creates tile assets in PaletteTiles and places them on named tilemap layers.
/// Run via Tools/Bezi/Import All Level 3 Tiles to Palette.
/// </summary>
public static class LavaTileImporter
{
    private const string PalettePath  = "Assets/Tilemaps/FinalLevel/FinalLevelTilePalette.prefab";
    private const string TileSavePath = "Assets/Tilemaps/FinalLevel/PaletteTiles";
    private const string AssetsRoot   = "Assets/LEVEL 3 ASSETS";

    // ── Sheet definitions ──────────────────────────────────────────────────────
    // Rows = -1 means custom lava section slicing is used instead of a uniform grid.
    private static readonly SheetDef[] Sheets =
    {
        new SheetDef
        {
            File         = "GandalfHardcore Blood Tiles.png",
            LayerName    = "GandalfHardcore_Blood_Tiles",
            SpritePrefix = "GandalfHardcore_Blood_Tiles",
            Cols         = 20, Rows = 11, TileW = 32, TileH = 32,
        },
        new SheetDef
        {
            File         = "GandalfHardcore Hell Tiles 32x32.png",
            LayerName    = "GandalfHardcore_Hell_Tiles",
            SpritePrefix = "GandalfHardcore_Hell_Tiles_32x32",
            Cols         = 17, Rows = 9,  TileW = 32, TileH = 32,
        },
        new SheetDef
        {
            File         = "GandalfHardcore Hell Tiles 32x32 dark.png",
            LayerName    = "GandalfHardcore_Hell_Tiles_Dark",
            SpritePrefix = "GandalfHardcore_Hell_Tiles_32x32_dark",
            Cols         = 17, Rows = 9,  TileW = 32, TileH = 32,
        },
        new SheetDef
        {
            File         = "Blue fire sheet.png",
            LayerName    = "Blue_Fire_Sheet",
            SpritePrefix = "Blue_fire_sheet",
            Cols         = 6,  Rows = 1,  TileW = 32, TileH = 32,
        },
        new SheetDef
        {
            File         = "Fire sheet.png",
            LayerName    = "Fire_Sheet",
            SpritePrefix = "Fire_sheet",
            Cols         = 6,  Rows = 1,  TileW = 32, TileH = 32,
        },
        new SheetDef
        {
            File         = "GandalfHardcore Lava Tiles.png",
            LayerName    = "GandalfHardcore_Lava_Tiles",
            SpritePrefix = "Lava",
            Cols         = 20, Rows = -1, TileW = 32, TileH = 32,
        },
    };

    // Section layout for the lava sheet (Unity tex coords: y=0 = bottom of image).
    private static readonly (int yBottom, int sectionH, int tileH, string prefix)[] LavaSections =
    {
        (287, 64,  32, "Cap"),
        (255, 27,  27, "Strip"),
        (121, 128, 32, "Pillar"),
        (57,  64,  32, "Floor"),
        (0,   32,  32, "Fringe"),
    };

    // ── Entry point ────────────────────────────────────────────────────────────

    [MenuItem("Tools/Bezi/Import All Level 3 Tiles to Palette")]
    public static void Run()
    {
        ResliceLavaSheet();
        AssetDatabase.Refresh();

        GameObject paletteRoot = PrefabUtility.LoadPrefabContents(PalettePath);

        // Reset the root transform so the palette origin is at (0,0).
        paletteRoot.transform.localPosition = Vector3.zero;

        int totalCreated = 0;
        int totalPlaced  = 0;

        // Each layer is stacked vertically below the previous one,
        // separated by a 2-row gap so tiles never overlap.
        int yOffset = 0;

        foreach (SheetDef sheet in Sheets)
        {
            string   spritePath = $"{AssetsRoot}/{sheet.File}";
            Sprite[] sprites    = AssetDatabase.LoadAllAssetsAtPath(spritePath)
                                               .OfType<Sprite>()
                                               .ToArray();

            if (sprites.Length == 0)
            {
                Debug.LogWarning($"[TileImporter] No sprites in '{sheet.File}' — skipping.");
                continue;
            }

            Tilemap layer = GetOrCreateLayer(paletteRoot, sheet.LayerName);
            layer.ClearAllTiles();

            // Always keep layer Transform at origin — offsets are baked into cell positions.
            layer.transform.localPosition = Vector3.zero;

            int rowsUsed;
            int created, placed;

            if (sheet.Rows < 0)
            {
                (created, placed, rowsUsed) = PlaceLavaTiles(sprites, layer, sheet, yOffset);
            }
            else
            {
                (created, placed) = PlaceGridTiles(sprites, layer, sheet, yOffset);
                rowsUsed = sheet.Rows;
            }

            totalCreated += created;
            totalPlaced  += placed;
            Debug.Log($"[TileImporter] '{sheet.LayerName}' — {placed} tiles placed, {created} assets created.");

            // Advance the cursor down by this layer's row count plus a 2-row gap.
            yOffset -= rowsUsed + 2;
        }

        AssetDatabase.SaveAssets();
        PrefabUtility.SaveAsPrefabAsset(paletteRoot, PalettePath);
        PrefabUtility.UnloadPrefabContents(paletteRoot);
        AssetDatabase.Refresh();

        Debug.Log($"[TileImporter] Complete — {totalCreated} tile assets created, {totalPlaced} tiles placed.");
    }

    // ── Lava sheet reslice ─────────────────────────────────────────────────────

    private static void ResliceLavaSheet()
    {
        string path = $"{AssetsRoot}/GandalfHardcore Lava Tiles.png";
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) { Debug.LogError("[TileImporter] Lava TextureImporter not found."); return; }

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;

        const int cols = 20;
#pragma warning disable CS0618
        var meta = new List<SpriteMetaData>();
        foreach (var (yBottom, sectionH, tileH, prefix) in LavaSections)
        {
            int rows = sectionH / tileH;
            for (int r = 0; r < rows; r++)
            {
                float y = yBottom + (rows - 1 - r) * tileH;
                for (int c = 0; c < cols; c++)
                {
                    meta.Add(new SpriteMetaData
                    {
                        name      = $"Lava_{prefix}_r{r:D2}_c{c:D2}",
                        rect      = new Rect(c * 32, y, 32, tileH),
                        alignment = 0,
                        pivot     = new Vector2(0.5f, 0.5f),
                    });
                }
            }
        }
        importer.spritesheet = meta.ToArray();
#pragma warning restore CS0618

        importer.SaveAndReimport();
        Debug.Log($"[TileImporter] Lava sheet resliced into {meta.Count} sprites.");
    }

    // ── Tile placement ─────────────────────────────────────────────────────────

    private static (int created, int placed) PlaceGridTiles(Sprite[] sprites, Tilemap layer, SheetDef sheet, int yOffset)
    {
        int created = 0, placed = 0;
        for (int r = 0; r < sheet.Rows; r++)
        {
            for (int c = 0; c < sheet.Cols; c++)
            {
                string spriteName = $"{sheet.SpritePrefix}_{r:D2}_{c:D2}";
                Sprite sprite     = sprites.FirstOrDefault(s => s.name == spriteName);
                if (sprite == null) continue;

                Tile tile = GetOrCreateTile(sprite, spriteName, ref created);
                layer.SetTile(new Vector3Int(c, yOffset - r, 0), tile);
                placed++;
            }
        }
        return (created, placed);
    }

    private static (int created, int placed, int rowsUsed) PlaceLavaTiles(Sprite[] sprites, Tilemap layer, SheetDef sheet, int yOffset)
    {
        int created = 0, placed = 0, paletteRow = 0;
        foreach (var (_, sectionH, tileH, prefix) in LavaSections)
        {
            int rows = sectionH / tileH;
            for (int r = 0; r < rows; r++, paletteRow++)
            {
                for (int c = 0; c < sheet.Cols; c++)
                {
                    string spriteName = $"Lava_{prefix}_r{r:D2}_c{c:D2}";
                    Sprite sprite     = sprites.FirstOrDefault(s => s.name == spriteName);
                    if (sprite == null) continue;

                    Tile tile = GetOrCreateTile(sprite, spriteName, ref created);
                    layer.SetTile(new Vector3Int(c, yOffset - paletteRow, 0), tile);
                    placed++;
                }
            }
        }
        return (created, placed, paletteRow);
    }

    // ── Utilities ──────────────────────────────────────────────────────────────

    /// <summary>Returns an existing Tilemap layer child by name, or creates one.</summary>
    private static Tilemap GetOrCreateLayer(GameObject paletteRoot, string layerName)
    {
        Transform existing = paletteRoot.transform.Find(layerName);
        if (existing != null)
        {
            Tilemap tm = existing.GetComponent<Tilemap>();
            if (tm != null) return tm;
        }

        GameObject go   = new GameObject(layerName);
        go.transform.SetParent(paletteRoot.transform, false);
        Tilemap tilemap = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        return tilemap;
    }

    /// <summary>Loads an existing Tile asset or creates a new one.</summary>
    private static Tile GetOrCreateTile(Sprite sprite, string spriteName, ref int createdCount)
    {
        string tilePath = $"{TileSavePath}/{spriteName}.asset";
        Tile   tile     = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile == null)
        {
            tile        = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color  = Color.white;
            AssetDatabase.CreateAsset(tile, tilePath);
            createdCount++;
        }
        return tile;
    }

    // ── Data ───────────────────────────────────────────────────────────────────

    private class SheetDef
    {
        public string File;
        public string LayerName;
        public string SpritePrefix;
        public int    Cols;
        public int    Rows;
        public int    TileW;
        public int    TileH;
    }
}
