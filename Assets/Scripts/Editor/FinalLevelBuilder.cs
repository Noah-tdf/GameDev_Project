using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class FinalLevelBuilder
{
    private const string ScenePath = "Assets/Scenes/FinalLevel.unity";
    private const string AssetRoot = "Assets/LEVEL 3 ASSETS";
    private const string EnemyRoot = AssetRoot + "/GandalfHardcore The Damned Enemies and NPCs";
    private const string BackgroundRoot = AssetRoot + "/GandalfHardcore Hell background";
    private const string TileAssetRoot = "Assets/Tilemaps/FinalLevel";
    private const string TileFolder = TileAssetRoot + "/PaletteTiles";
    private const string AnimationFolder = "Assets/Animations/FinalLevel/Enemies";
    private const string EnemyPrefabFolder = "Assets/Prefabs/FinalLevel/Enemies";

    private const int TileSize = 32;
    private const int Ppu = 32;
    private const float TileGridCellSize = 0.9f;
    private const float TilePpu = Ppu / TileGridCellSize;

    [MenuItem("Ubuntu City/Build Final Level Scene")]
    public static void Build()
    {
        EnsureFolders();
        ConfigureHellBackgroundImports();
        ConfigureSetpieceImports();
        AssetDatabase.Refresh();
        CreateTileAssets();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraRoot = Root("=== CAMERA ===");
        BuildSceneCamera(cameraRoot.transform);
        GameObject environmentRoot = Root("=== ENVIRONMENT ===");
        BuildHellBackground(environmentRoot.transform);
        Dictionary<string, Tilemap> tilemaps = BuildEditableTilemapGrid(environmentRoot.transform);
        BuildCenterHellSetpiece(environmentRoot.transform, tilemaps);
        Root("=== PLAYER ===");
        Root("=== ENEMIES ===");
        Root("=== UI ===");
        BuildEventSystem();
        Root("=== AUDIO ===");

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("FinalLevel.unity built with camera, layered Hell background, editable tilemap grid, and center Hell setpiece.");
    }

    private static GameObject Root(string name)
    {
        return new GameObject(name);
    }

    private static void BuildHellBackground(Transform parent)
    {
        AddBackgroundLayer(parent, "BackgroundLayer", BackgroundRoot + "/Background layer.png", -40, 10f);
        AddBackgroundLayer(parent, "BackLayer", BackgroundRoot + "/back layer.png", -30, 9f);
        AddBackgroundLayer(parent, "MiddleLayer", BackgroundRoot + "/middle layer.png", -20, 8f);
        AddBackgroundLayer(parent, "FrontLayer", BackgroundRoot + "/front layer.png", -10, 7f);
    }

    private static void BuildSceneCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(parent);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.03f, 0.01f, 0.06f);

        cameraObject.AddComponent<AudioListener>();
    }

    private static Dictionary<string, Tilemap> BuildEditableTilemapGrid(Transform parent)
    {
        GameObject gridObject = new GameObject("TilemapGrid");
        gridObject.transform.SetParent(parent);

        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSize = new Vector3(TileGridCellSize, TileGridCellSize, 1f);

        Dictionary<string, Tilemap> tilemaps = new Dictionary<string, Tilemap>();
        tilemaps["GroundTilemap"] = CreateEmptyTilemap(gridObject.transform, "GroundTilemap", 0);
        tilemaps["DecorTilemap"] = CreateEmptyTilemap(gridObject.transform, "DecorTilemap", 5);
        tilemaps["HazardTilemap"] = CreateEmptyTilemap(gridObject.transform, "HazardTilemap", 10);
        return tilemaps;
    }

    private static Tilemap CreateEmptyTilemap(Transform parent, string name, int sortingOrder)
    {
        GameObject tilemapObject = new GameObject(name);
        tilemapObject.transform.SetParent(parent);

        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        return tilemap;
    }

    private static void AddBackgroundLayer(Transform parent, string objectName, string spritePath, int sortingOrder, float zPosition)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"Missing final level background layer: {spritePath}");
            return;
        }

        GameObject layer = new GameObject(objectName);
        layer.transform.SetParent(parent);
        layer.transform.position = new Vector3(0f, 0f, zPosition);

        SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Tilemaps");
        EnsureFolder(TileAssetRoot);
        EnsureFolder(TileFolder);
        EnsureFolder("Assets/Animations");
        EnsureFolder("Assets/Animations/FinalLevel");
        EnsureFolder(AnimationFolder);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/FinalLevel");
        EnsureFolder(EnemyPrefabFolder);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    private static void ConfigureImports()
    {
        ConfigureGridTexture(AssetRoot + "/GandalfHardcore Hell Tiles 32x32.png", TileSize, TileSize);
        ConfigureGridTexture(AssetRoot + "/GandalfHardcore Hell Tiles 32x32 dark.png", TileSize, TileSize);
        ConfigureGridTexture(AssetRoot + "/GandalfHardcore Lava Tiles.png", TileSize, TileSize);
        ConfigureGridTexture(AssetRoot + "/GandalfHardcore Blood Tiles.png", TileSize, TileSize);
        ConfigureGridTexture(AssetRoot + "/Fire sheet.png", TileSize, TileSize);
        ConfigureGridTexture(AssetRoot + "/Blue fire sheet.png", TileSize, TileSize);

        ConfigureSingleSprite(AssetRoot + "/Large Statue.png", Ppu);
        ConfigureSingleSprite(AssetRoot + "/Large Statue 2.png", Ppu);

        foreach (string path in Directory.GetFiles(EnemyRoot, "*.png").Select(ToAssetPath))
        {
            Vector2Int frame = GetEnemyFrameSize(path);
            ConfigureGridTexture(path, frame.x, frame.y, true);
        }

        if (AssetDatabase.IsValidFolder(BackgroundRoot))
        {
            foreach (string path in Directory.GetFiles(BackgroundRoot, "*.png").Select(ToAssetPath))
                ConfigureSingleSprite(path, Ppu);
        }
    }

    private static void ConfigureHellBackgroundImports()
    {
        string[] layerPaths =
        {
            BackgroundRoot + "/Background layer.png",
            BackgroundRoot + "/back layer.png",
            BackgroundRoot + "/middle layer.png",
            BackgroundRoot + "/front layer.png"
        };

        foreach (string path in layerPaths)
            ConfigureSingleSprite(path, Ppu);
    }

    private static void ConfigureSetpieceImports()
    {
        ConfigureGridTexture(AssetRoot + "/GandalfHardcore Hell Tiles 32x32.png", TileSize, TileSize, false, TilePpu);
        ConfigureGridTexture(AssetRoot + "/GandalfHardcore Lava Tiles.png", TileSize, TileSize, false, TilePpu);
        ConfigureGridTexture(AssetRoot + "/Fire sheet.png", TileSize, TileSize, false, TilePpu);
        ConfigureSingleSprite(AssetRoot + "/Large Statue.png", TilePpu);
    }

    private static void BuildCenterHellSetpiece(Transform parent, Dictionary<string, Tilemap> tilemaps)
    {
        if (!tilemaps.TryGetValue("GroundTilemap", out Tilemap ground) ||
            !tilemaps.TryGetValue("DecorTilemap", out Tilemap decor) ||
            !tilemaps.TryGetValue("HazardTilemap", out Tilemap hazard))
        {
            Debug.LogWarning("Final level center setpiece skipped because the editable tilemaps were not created.");
            return;
        }

        PaintCenterGround(ground);
        PaintPreviewCave(ground);
        PaintPreviewPillars(decor);
        PaintPreviewRubble(decor);
        AnimatedTile fireTile = CreateAnimatedFireTile();
        PaintPreviewLavaAndFireBase(hazard, fireTile);

        GameObject setpiece = new GameObject("CenterHellSetpiece");
        setpiece.transform.SetParent(parent);
        AddDecoration(setpiece.transform, AssetRoot + "/Large Statue.png", "LargeStatue_Left", GridPosition(-8.1f, -3.9f), 8, false);
        AddDecoration(setpiece.transform, AssetRoot + "/Large Statue.png", "LargeStatue_Right", GridPosition(8.1f, -3.9f), 8, true);
        AddAnimatedFire(setpiece.transform, AssetRoot + "/Fire sheet.png", "CenterFire_Left", GridPosition(-1.25f, -1.05f), 12);
        AddAnimatedFire(setpiece.transform, AssetRoot + "/Fire sheet.png", "CenterFire_Middle", GridPosition(0f, -0.95f), 12);
        AddAnimatedFire(setpiece.transform, AssetRoot + "/Fire sheet.png", "CenterFire_Right", GridPosition(1.2f, -1.1f), 12);
        AddAnimatedFire(setpiece.transform, AssetRoot + "/Fire sheet.png", "LeftGroundFire", GridPosition(-5.8f, -4.05f), 12);
        AddAnimatedFire(setpiece.transform, AssetRoot + "/Fire sheet.png", "RightGroundFire", GridPosition(5.8f, -4.05f), 12);
    }

    private static Vector3 GridPosition(float x, float y)
    {
        return new Vector3(x * TileGridCellSize, y * TileGridCellSize, 0f);
    }

    private static void PaintCenterGround(Tilemap ground)
    {
        for (int x = -18; x <= 18; x++)
        {
            Tile tile = HellTile(x % 2 == 0 ? 6 : 7, 8);
            SetTileIfNotNull(ground, new Vector3Int(x, -5, 0), tile);
        }

        int[] rubblePattern = { 9, 10, 11, 10, 9, 11 };
        for (int x = -17; x <= 17; x++)
        {
            if (x > -4 && x < 4)
                continue;

            int col = rubblePattern[Mathf.Abs(x) % rubblePattern.Length];
            SetTileIfNotNull(ground, new Vector3Int(x, -4, 0), HellTile(8, col));
        }
    }

    private static void PaintPreviewCave(Tilemap ground)
    {
        for (int row = 6; row <= 8; row++)
        {
            for (int col = 0; col <= 5; col++)
            {
                SetTileIfNotNull(ground, new Vector3Int(col - 3, 5 - row, 0), HellTile(row, col));
            }
        }

        SetTileIfNotNull(ground, new Vector3Int(-4, -2, 0), HellTile(8, 9));
        SetTileIfNotNull(ground, new Vector3Int(3, -2, 0), HellTile(8, 10));
        SetTileIfNotNull(ground, new Vector3Int(4, -2, 0), HellTile(8, 11));
    }

    private static void PaintPreviewPillars(Tilemap decor)
    {
        PaintPillar(decor, -2, -1, 5);
        PaintPillar(decor, 3, -1, 5);

        SetTileIfNotNull(decor, new Vector3Int(-12, -4, 0), HellTile(8, 13));
        SetTileIfNotNull(decor, new Vector3Int(-12, -3, 0), HellTile(7, 14));
        SetTileIfNotNull(decor, new Vector3Int(-12, -2, 0), HellTile(6, 14));
        SetTileIfNotNull(decor, new Vector3Int(12, -4, 0), HellTile(8, 14));
        SetTileIfNotNull(decor, new Vector3Int(12, -3, 0), HellTile(5, 13));
    }

    private static void PaintPillar(Tilemap decor, int x, int bottomY, int height)
    {
        SetTileIfNotNull(decor, new Vector3Int(x, bottomY, 0), HellTile(5, 14));
        for (int y = bottomY + 1; y < bottomY + height - 1; y++)
            SetTileIfNotNull(decor, new Vector3Int(x, y, 0), HellTile(4, 14));

        SetTileIfNotNull(decor, new Vector3Int(x, bottomY + height - 1, 0), HellTile(3, 14));
    }

    private static void PaintPreviewRubble(Tilemap decor)
    {
        Vector3Int[] positions =
        {
            new Vector3Int(-7, -4, 0), new Vector3Int(-6, -4, 0), new Vector3Int(-5, -4, 0),
            new Vector3Int(5, -4, 0), new Vector3Int(6, -4, 0), new Vector3Int(7, -4, 0),
            new Vector3Int(-1, -1, 0), new Vector3Int(1, -1, 0)
        };

        int[] columns = { 9, 10, 11, 9, 10, 11, 13, 14 };
        for (int i = 0; i < positions.Length; i++)
            SetTileIfNotNull(decor, positions[i], HellTile(8, columns[i]));
    }

    private static void PaintPreviewLavaAndFireBase(Tilemap hazard, AnimatedTile fireTile)
    {
        for (int x = -18; x <= 18; x++)
            SetTileIfNotNull(hazard, new Vector3Int(x, -6, 0), LavaTile(0, Mathf.Abs(x) % 20));

        SetTileIfNotNull(hazard, new Vector3Int(-6, -4, 0), HellTile(6, 10));
        SetTileIfNotNull(hazard, new Vector3Int(6, -4, 0), HellTile(6, 11));

        Vector3Int[] firePositions =
        {
            new Vector3Int(-2, -1, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-6, -4, 0),
            new Vector3Int(6, -4, 0),
            new Vector3Int(-15, -4, 0),
            new Vector3Int(15, -4, 0)
        };

        foreach (Vector3Int position in firePositions)
            SetTileIfNotNull(hazard, position, fireTile);
    }

    private static AnimatedTile CreateAnimatedFireTile()
    {
        string tilePath = TileAssetRoot + "/FireAnimatedTile.asset";
        AnimatedTile fireTile = AssetDatabase.LoadAssetAtPath<AnimatedTile>(tilePath);
        if (fireTile == null)
        {
            fireTile = ScriptableObject.CreateInstance<AnimatedTile>();
            AssetDatabase.CreateAsset(fireTile, tilePath);
        }

        fireTile.m_AnimatedSprites = LoadSprites(AssetRoot + "/Fire sheet.png")
            .Where(sprite => sprite.name.Contains("_00_"))
            .OrderBy(sprite => sprite.name)
            .ToArray();
        fireTile.m_MinSpeed = 8f;
        fireTile.m_MaxSpeed = 8f;
        fireTile.m_TileColliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(fireTile);
        return fireTile;
    }

    private static Tile HellTile(int row, int column)
    {
        return PaletteTile($"GandalfHardcore_Hell_Tiles_32x32_{row:00}_{column:00}");
    }

    private static Tile LavaTile(int row, int column)
    {
        return PaletteTile($"GandalfHardcore_Lava_Tiles_{row:00}_{column:00}");
    }

    private static Tile PaletteTile(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<Tile>($"{TileFolder}/{assetName}.asset");
    }

    private static Tile TileAt(Tile[] tiles, int row, int column, int columns = 17)
    {
        if (tiles == null)
            return null;

        int index = row * columns + column;
        if (index < 0 || index >= tiles.Length)
            return null;

        return tiles[index];
    }

    private static void SetTileIfNotNull(Tilemap tilemap, Vector3Int position, TileBase tile)
    {
        if (tile != null)
            tilemap.SetTile(position, tile);
    }

    private static void ConfigureSingleSprite(string path, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static void ConfigureGridTexture(string path, int frameWidth, int frameHeight, bool firstRowAnimationFriendly = false, float pixelsPerUnit = Ppu)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (texture == null || importer == null)
            return;

        int columns = Mathf.Max(1, texture.width / frameWidth);
        int rows = Mathf.Max(1, texture.height / frameHeight);
        string cleanName = Path.GetFileNameWithoutExtension(path).Replace(" ", "_");
        List<SpriteMetaData> sprites = new List<SpriteMetaData>(columns * rows);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                sprites.Add(new SpriteMetaData
                {
                    name = $"{cleanName}_{row:00}_{col:00}",
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center,
                    rect = new Rect(col * frameWidth, texture.height - ((row + 1) * frameHeight), frameWidth, frameHeight)
                });
            }
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
#pragma warning disable 618
        importer.spritesheet = sprites.ToArray();
#pragma warning restore 618
        importer.SaveAndReimport();
    }

    private static Vector2Int GetEnemyFrameSize(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            return new Vector2Int(64, 64);

        string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        if (name.Contains("female hell giant"))
            return new Vector2Int(160, 128);

        if (texture.height <= 64)
            return new Vector2Int(64, 64);

        if (texture.width >= 1280 && texture.height >= 512)
            return new Vector2Int(128, 128);

        return new Vector2Int(64, 64);
    }

    private static Dictionary<string, Tile[]> CreateTileAssets()
    {
        string[] sheetPaths =
        {
            AssetRoot + "/GandalfHardcore Hell Tiles 32x32.png",
            AssetRoot + "/GandalfHardcore Hell Tiles 32x32 dark.png",
            AssetRoot + "/GandalfHardcore Lava Tiles.png",
            AssetRoot + "/GandalfHardcore Blood Tiles.png"
        };

        Dictionary<string, Tile[]> tileSets = new Dictionary<string, Tile[]>();
        foreach (string sheet in sheetPaths)
        {
            string setName = Path.GetFileNameWithoutExtension(sheet).Replace(" ", "_");
            Sprite[] sprites = LoadSprites(sheet);
            List<Tile> tiles = new List<Tile>();

            foreach (Sprite sprite in sprites)
            {
                string tilePath = $"{TileFolder}/{sprite.name}.asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }

                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Sprite;
                EditorUtility.SetDirty(tile);
                tiles.Add(tile);
            }

            tileSets[setName] = tiles.ToArray();
        }

        AssetDatabase.SaveAssets();
        return tileSets;
    }

    private static GameObject CreateTilePalettePrefab(Dictionary<string, Tile[]> tileSets)
    {
        string palettePath = TileAssetRoot + "/FinalLevelTilePalette.prefab";
        GameObject palette = new GameObject("FinalLevelTilePalette");
        Grid grid = palette.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        int rowOffset = 0;
        foreach (KeyValuePair<string, Tile[]> set in tileSets)
        {
            GameObject mapObject = new GameObject(set.Key);
            mapObject.transform.SetParent(palette.transform);
            Tilemap map = mapObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = mapObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 0;

            for (int i = 0; i < set.Value.Length; i++)
            {
                int x = i % 20;
                int y = rowOffset - (i / 20);
                map.SetTile(new Vector3Int(x, y, 0), set.Value[i]);
            }

            rowOffset -= Mathf.CeilToInt(set.Value.Length / 20f) + 2;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(palette, palettePath);
        Object.DestroyImmediate(palette);
        return prefab;
    }

    private static Dictionary<string, GameObject> CreateEnemyAnimationPrefabs()
    {
        Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        foreach (string path in Directory.GetFiles(EnemyRoot, "*.png").Select(ToAssetPath))
        {
            Sprite[] sprites = LoadSprites(path);
            if (sprites.Length == 0)
                continue;

            string cleanName = Path.GetFileNameWithoutExtension(path).Replace(" ", "");
            Sprite[] firstRow = sprites
                .Where(sprite => sprite.name.Contains("_00_"))
                .OrderBy(sprite => sprite.name)
                .Take(12)
                .ToArray();

            if (firstRow.Length == 0)
                firstRow = sprites.Take(8).ToArray();

            string clipPath = $"{AnimationFolder}/{cleanName}_Idle.anim";
            string controllerPath = $"{AnimationFolder}/{cleanName}.controller";
            AnimationClip clip = CreateSpriteClip(clipPath, firstRow);
            AnimatorController controller = CreateController(controllerPath, clip);
            GameObject prefab = CreateEnemyPrefab(cleanName, firstRow[0], controller);
            prefabs[cleanName] = prefab;
        }

        AssetDatabase.SaveAssets();
        return prefabs;
    }

    private static AnimationClip CreateSpriteClip(string clipPath, Sprite[] frames)
    {
        AssetDatabase.DeleteAsset(clipPath);
        AnimationClip clip = new AnimationClip
        {
            frameRate = 8f
        };

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / clip.frameRate,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static AnimatorController CreateController(string controllerPath, AnimationClip clip)
    {
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorState state = controller.layers[0].stateMachine.AddState("Idle");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        return controller;
    }

    private static GameObject CreateEnemyPrefab(string name, Sprite sprite, RuntimeAnimatorController controller)
    {
        string prefabPath = $"{EnemyPrefabFolder}/{name}.prefab";
        GameObject enemy = new GameObject(name);
        enemy.tag = "Enemy";

        SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 8;

        Animator animator = enemy.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 1f);

        Enemy enemyLogic = enemy.AddComponent<Enemy>();
        SerializedObject serialized = new SerializedObject(enemyLogic);
        SetSerialized(serialized, "maxHealth", name.Contains("Devil") ? 8 : 4);
        SetSerialized(serialized, "patrolSpeed", name.Contains("Skull") || name.Contains("Demon") ? 1.6f : 2.1f);
        SetSerialized(serialized, "contactDamage", name.Contains("Devil") ? 2 : 1);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
        Object.DestroyImmediate(enemy);
        return prefab;
    }

    private static void BuildEnvironment(Transform parent)
    {
        GameObject background = new GameObject("HellCityBackground");
        background.transform.SetParent(parent);

        List<Sprite> sprites = new List<Sprite>();
        if (AssetDatabase.IsValidFolder(BackgroundRoot))
        {
            foreach (string path in Directory.GetFiles(BackgroundRoot, "*.png").Select(ToAssetPath))
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    sprites.Add(sprite);
            }
        }

        if (sprites.Count == 0)
        {
            BuildFallbackBackdrop(background.transform);
        }
        else
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                GameObject layer = new GameObject("BackgroundLayer_" + i);
                layer.transform.SetParent(background.transform);
                layer.transform.position = new Vector3(i * 7f, -0.8f + (i % 2) * 0.6f, 10f + i);
                SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[i];
                renderer.sortingOrder = -30 + i;
                float targetHeight = i == 0 ? 12f : 9f;
                float scale = targetHeight / Mathf.Max(0.01f, renderer.bounds.size.y);
                layer.transform.localScale = Vector3.one * scale;
            }
        }

        AddDecoration(parent, AssetRoot + "/Large Statue.png", "LargeStatue_A", new Vector3(18f, -2f, 0f), 6);
        AddDecoration(parent, AssetRoot + "/Large Statue 2.png", "LargeStatue_B", new Vector3(48f, -2f, 0f), 6);
        AddAnimatedFire(parent, AssetRoot + "/Fire sheet.png", "FirePillar_A", new Vector3(28f, -3.25f, 0f));
        AddAnimatedFire(parent, AssetRoot + "/Blue fire sheet.png", "BlueFirePillar_B", new Vector3(62f, -1.25f, 0f));
    }

    private static void BuildFallbackBackdrop(Transform parent)
    {
        GameObject sky = new GameObject("DarkHellSky");
        sky.transform.SetParent(parent);
        sky.transform.position = new Vector3(35f, 0f, 10f);
        SpriteRenderer renderer = sky.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = new Color(0.05f, 0.02f, 0.08f);
        renderer.sortingOrder = -40;
        sky.transform.localScale = new Vector3(90f, 16f, 1f);
    }

    private static void AddDecoration(Transform parent, string spritePath, string name, Vector3 position, int order, bool flipX = false)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
            return;

        GameObject decoration = new GameObject(name);
        decoration.transform.SetParent(parent);
        decoration.transform.position = position;
        SpriteRenderer renderer = decoration.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.flipX = flipX;
    }

    private static void AddAnimatedFire(Transform parent, string sheetPath, string name, Vector3 position, int sortingOrder = 7)
    {
        Sprite[] sprites = LoadSprites(sheetPath).Where(sprite => sprite.name.Contains("_00_")).OrderBy(sprite => sprite.name).ToArray();
        if (sprites.Length == 0)
            return;

        string clipPath = $"{AnimationFolder}/{name}.anim";
        string controllerPath = $"{AnimationFolder}/{name}.controller";
        AnimationClip clip = CreateSpriteClip(clipPath, sprites);
        AnimatorController controller = CreateController(controllerPath, clip);

        GameObject fire = new GameObject(name);
        fire.transform.SetParent(parent);
        fire.transform.position = position;
        SpriteRenderer renderer = fire.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites[0];
        renderer.sortingOrder = sortingOrder;
        Animator animator = fire.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
    }

    private static void BuildGroundTilemaps(Transform parent, Dictionary<string, Tile[]> tileSets)
    {
        GameObject gridObject = new GameObject("FinalLevelGrid");
        gridObject.transform.SetParent(parent);
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        Tile[] groundTiles = PickTileSet(tileSets, "GandalfHardcore_Hell_Tiles_32x32");
        Tile[] lavaTiles = PickTileSet(tileSets, "GandalfHardcore_Lava_Tiles");
        Tile[] bloodTiles = PickTileSet(tileSets, "GandalfHardcore_Blood_Tiles");

        Tilemap ground = MakeTilemap(gridObject.transform, "GroundTilemap", 0, true);
        Tilemap lava = MakeTilemap(gridObject.transform, "LavaTilemap", 1, false);
        Tilemap blood = MakeTilemap(gridObject.transform, "BloodTrimTilemap", 2, false);

        PaintPlatform(ground, groundTiles, -3, -5, 74, 3);
        PaintPlatform(ground, groundTiles, 6, -1, 10, 2);
        PaintPlatform(ground, groundTiles, 22, 1, 8, 2);
        PaintPlatform(ground, groundTiles, 36, -1, 11, 2);
        PaintPlatform(ground, groundTiles, 54, 2, 9, 2);
        PaintPlatform(ground, groundTiles, 68, -1, 8, 2);

        PaintPlatform(lava, lavaTiles, 17, -5, 4, 1);
        PaintPlatform(lava, lavaTiles, 48, -5, 5, 1);
        PaintPlatform(lava, lavaTiles, 63, -5, 3, 1);
        PaintPlatform(blood, bloodTiles, 22, 3, 8, 1);
        PaintPlatform(blood, bloodTiles, 54, 4, 9, 1);
    }

    private static void BuildAirPlatforms(Transform parent, Dictionary<string, Tile[]> tileSets)
    {
        GameObject gridObject = new GameObject("AirPlatformGrid");
        gridObject.transform.SetParent(parent);
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        Tile[] darkTiles = PickTileSet(tileSets, "GandalfHardcore_Hell_Tiles_32x32_dark");
        Tilemap air = MakeTilemap(gridObject.transform, "AirPlatforms", 4, true);

        PaintPlatform(air, darkTiles, 13, 2, 5, 1);
        PaintPlatform(air, darkTiles, 31, 4, 5, 1);
        PaintPlatform(air, darkTiles, 43, 2, 6, 1);
        PaintPlatform(air, darkTiles, 61, 5, 5, 1);
    }

    private static Tilemap MakeTilemap(Transform parent, string name, int sortingOrder, bool collidable)
    {
        GameObject objectMap = new GameObject(name);
        objectMap.transform.SetParent(parent);
        objectMap.layer = GroundLayer();
        Tilemap tilemap = objectMap.AddComponent<Tilemap>();
        TilemapRenderer renderer = objectMap.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        if (collidable)
        {
            TilemapCollider2D collider = objectMap.AddComponent<TilemapCollider2D>();
            collider.usedByComposite = true;
            Rigidbody2D rb = objectMap.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            objectMap.AddComponent<CompositeCollider2D>();
            objectMap.tag = "Ground";
        }

        return tilemap;
    }

    private static void PaintPlatform(Tilemap tilemap, Tile[] tiles, int startX, int startY, int width, int height)
    {
        if (tiles == null || tiles.Length == 0)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = tiles[(x + y * 3) % tiles.Length];
                tilemap.SetTile(new Vector3Int(startX + x, startY + y, 0), tile);
            }
        }
    }

    private static void BuildLadders(Transform parent)
    {
        MakeLadder(parent, "Ladder_To_Mid", new Vector3(20f, -2f, 0f), 4f);
        MakeLadder(parent, "Ladder_To_Final", new Vector3(53f, -1f, 0f), 6f);
    }

    private static void MakeLadder(Transform parent, string name, Vector3 position, float height)
    {
        GameObject ladder = new GameObject(name);
        ladder.transform.SetParent(parent);
        ladder.transform.position = position;
        BoxCollider2D trigger = ladder.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1f, height);

        SpriteRenderer renderer = ladder.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = new Color(0.45f, 0.18f, 0.06f);
        renderer.sortingOrder = 3;
        ladder.transform.localScale = new Vector3(0.25f, height, 1f);
    }

    private static void BuildEnemies(Transform parent, Dictionary<string, GameObject> prefabs)
    {
        PlaceEnemy(parent, prefabs, "Imp", new Vector3(10f, 0f, 0f), 7f, 13f);
        PlaceEnemy(parent, prefabs, "BurningSkull", new Vector3(25f, 3f, 0f), 22f, 29f);
        PlaceEnemy(parent, prefabs, "DemonEye", new Vector3(39f, 1f, 0f), 36f, 46f);
        PlaceEnemy(parent, prefabs, "OldDemon", new Vector3(57f, 4f, 0f), 54f, 62f);
        PlaceEnemy(parent, prefabs, "TheDevil", new Vector3(70f, 0f, 0f), 66f, 74f);
    }

    private static void PlaceEnemy(Transform parent, Dictionary<string, GameObject> prefabs, string key, Vector3 position, float leftX, float rightX)
    {
        GameObject prefab = FindEnemyPrefab(prefabs, key);
        if (prefab == null)
            return;

        GameObject enemy = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (enemy == null)
            return;

        enemy.transform.SetParent(parent);
        enemy.transform.position = position;

        GameObject left = new GameObject("LeftPatrolPoint");
        left.transform.SetParent(enemy.transform);
        left.transform.position = new Vector3(leftX, position.y, 0f);

        GameObject right = new GameObject("RightPatrolPoint");
        right.transform.SetParent(enemy.transform);
        right.transform.position = new Vector3(rightX, position.y, 0f);

        Enemy enemyLogic = enemy.GetComponent<Enemy>();
        if (enemyLogic != null)
        {
            SerializedObject serialized = new SerializedObject(enemyLogic);
            serialized.FindProperty("leftPoint").objectReferenceValue = left.transform;
            serialized.FindProperty("rightPoint").objectReferenceValue = right.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static GameObject FindEnemyPrefab(Dictionary<string, GameObject> prefabs, string key)
    {
        if (prefabs.TryGetValue(key, out GameObject exact))
            return exact;

        KeyValuePair<string, GameObject> caseInsensitiveExact = prefabs.FirstOrDefault(pair =>
            string.Equals(pair.Key, key, System.StringComparison.OrdinalIgnoreCase));
        if (caseInsensitiveExact.Value != null)
            return caseInsensitiveExact.Value;

        KeyValuePair<string, GameObject> contains = prefabs.FirstOrDefault(pair =>
            pair.Key.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0);
        return contains.Value;
    }

    private static GameObject BuildPlayer(Transform parent)
    {
        string[] candidates =
        {
            "Assets/Prefabs/Characters/Luca.prefab",
            "Assets/Prefabs/Level1/Luca.prefab"
        };

        foreach (string path in candidates)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (player == null)
                continue;

            player.name = "Luca";
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Player");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(0f, -2f, 0f);
            return player;
        }

        GameObject fallback = new GameObject("Luca");
        fallback.transform.SetParent(parent);
        fallback.transform.position = new Vector3(0f, -2f, 0f);
        fallback.tag = "Player";
        fallback.AddComponent<SpriteRenderer>().sprite = CreateSquareSprite();
        fallback.AddComponent<Rigidbody2D>();
        fallback.AddComponent<BoxCollider2D>();
        return fallback;
    }

    private static GameObject BuildCamera(Transform parent, Transform target)
    {
        GameObject camera = new GameObject("Main Camera");
        camera.transform.SetParent(parent);
        camera.transform.position = new Vector3(4f, 0f, -10f);
        camera.tag = "MainCamera";

        Camera cam = camera.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.01f, 0.06f);
        camera.AddComponent<AudioListener>();

        CameraFollow follow = camera.AddComponent<CameraFollow>();
        SerializedObject serialized = new SerializedObject(follow);
        serialized.FindProperty("target").objectReferenceValue = target;
        serialized.FindProperty("lockedY").floatValue = 0f;
        serialized.FindProperty("minX").floatValue = -3f;
        serialized.FindProperty("maxX").floatValue = 78f;
        serialized.FindProperty("smoothTime").floatValue = 0.12f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return camera;
    }

    private static void BuildTips(Transform parent)
    {
        GameObject tip = new GameObject("FinalLevelIntroTip");
        tip.transform.SetParent(parent);
        tip.transform.position = new Vector3(4f, 2.2f, 0f);
        TextMesh text = tip.AddComponent<TextMesh>();
        text.text = "FINAL LEVEL";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.35f;
        text.fontSize = 32;
        MeshRenderer renderer = tip.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 20;
    }

    private static void BuildUI(Transform parent)
    {
        GameObject uiNote = new GameObject("RuntimeHUD");
        uiNote.transform.SetParent(parent);
    }

    private static GameObject BuildEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        return eventSystem;
    }

    private static void BuildAudio(Transform parent)
    {
        GameObject ambience = new GameObject("HellAmbience");
        ambience.transform.SetParent(parent);
        AudioSource source = ambience.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0.35f;
    }

    private static void BuildLevelBounds(Transform parent)
    {
        GameObject leftWall = new GameObject("LeftBound");
        leftWall.transform.SetParent(parent);
        leftWall.transform.position = new Vector3(-2f, 0f, 0f);
        leftWall.layer = GroundLayer();
        BoxCollider2D leftCollider = leftWall.AddComponent<BoxCollider2D>();
        leftCollider.size = new Vector2(1f, 20f);

        GameObject deathZone = new GameObject("FallDeathZone");
        deathZone.transform.SetParent(parent);
        deathZone.transform.position = new Vector3(36f, -9f, 0f);
        BoxCollider2D zoneCollider = deathZone.AddComponent<BoxCollider2D>();
        zoneCollider.isTrigger = true;
        zoneCollider.size = new Vector2(90f, 2f);

        GameObject exit = new GameObject("FinalExitTrigger");
        exit.transform.SetParent(parent);
        exit.transform.position = new Vector3(77f, 0f, 0f);
        BoxCollider2D exitCollider = exit.AddComponent<BoxCollider2D>();
        exitCollider.isTrigger = true;
        exitCollider.size = new Vector2(2f, 12f);
        LevelExitTrigger trigger = exit.AddComponent<LevelExitTrigger>();
        SerializedObject serialized = new SerializedObject(trigger);
        serialized.FindProperty("nextSceneName").stringValue = "MainMenu";
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Tile[] PickTileSet(Dictionary<string, Tile[]> tileSets, string keyPart)
    {
        KeyValuePair<string, Tile[]> found = tileSets.FirstOrDefault(pair => pair.Key.Contains(keyPart));
        return found.Value ?? tileSets.Values.FirstOrDefault() ?? new Tile[0];
    }

    private static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();
    }

    private static string ToAssetPath(string absolutePath)
    {
        return absolutePath.Replace("\\", "/").Replace(Application.dataPath, "Assets");
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(scene => scene.path == ScenePath))
            return;

        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void SetSerialized(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.intValue = value;
    }

    private static void SetSerialized(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static int GroundLayer()
    {
        int layer = LayerMask.NameToLayer("Ground");
        return layer >= 0 ? layer : 0;
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
