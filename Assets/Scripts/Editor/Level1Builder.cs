using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Editor utility: Ubuntu City → Build Level 1 Scene
/// Creates a fully playable Level 1 scene with all GameObjects, scripts, and prefabs.
/// </summary>
public static class Level1Builder
{
    private const string ScenePath  = "Assets/Scenes/Level1.unity";
    private const string PrefabPath = "Assets/Prefabs/Level1/";

    // ── Entry point ──────────────────────────────────────────────────────────
    [MenuItem("Ubuntu City/Build Level 1 Scene")]
    public static void Build()
    {
        EnsureFolders();

        // Create a fresh empty scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Hierarchy roots ──────────────────────────────────────────────────
        GameObject camRoot      = new GameObject("=== CAMERA ===");
        GameObject envRoot      = new GameObject("=== ENVIRONMENT ===");
        GameObject hazardRoot   = new GameObject("=== HAZARDS ===");
        GameObject playerRoot   = new GameObject("=== PLAYER ===");
        GameObject enemyRoot    = new GameObject("=== ENEMIES ===");
        GameObject uiRoot       = new GameObject("=== UI ===");

        // ── 1. CAMERA ────────────────────────────────────────────────────────
        GameObject camGO = BuildCamera();
        camGO.transform.SetParent(camRoot.transform);

        // ── 2. ENVIRONMENT ───────────────────────────────────────────────────
        GameObject bgParent       = BuildBackground(envRoot.transform);
        GameObject ground         = BuildGround(envRoot.transform);
        GameObject platformParent = BuildPlatforms(envRoot.transform);
        BuildLevelBounds(envRoot.transform);

        // ── 3. HAZARDS ───────────────────────────────────────────────────────
        GameObject carPrefab = BuildCarPrefab();
        BuildCarSpawner(hazardRoot.transform, carPrefab);

        // ── 4. PLAYER ────────────────────────────────────────────────────────
        GameObject bulletPrefab = BuildBulletPrefab();
        GameObject player = BuildPlayer(playerRoot.transform, bulletPrefab);

        // ── 5. ENEMIES ───────────────────────────────────────────────────────
        BuildEnemies(enemyRoot.transform);

        // ── 6. UI ────────────────────────────────────────────────────────────
        TextMeshProUGUI hpLabel = BuildUI(uiRoot.transform);

        // ── Wire up camera → player ─────────────────────────────────────────
        CameraFollow cf = camGO.GetComponent<CameraFollow>();
        if (cf != null) cf.SetTarget(player.transform);

        // Wire HP label → PlayerHealth
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null && hpLabel != null)
        {
            SerializedObject so = new SerializedObject(ph);
            so.FindProperty("hpText").objectReferenceValue = hpLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Save ─────────────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Ubuntu City — Level 1",
            "Level1.unity created!\n\nOpen the scene and press Play.\nA/D = move  |  Space = jump  |  J = shoot",
            "OK");
    }

    // ────────────────────────────────────────────────────────────────────────
    // CAMERA
    // ────────────────────────────────────────────────────────────────────────
    private static GameObject BuildCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(5f, 0f, -10f);

        Camera cam = go.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.529f, 0.808f, 0.922f);  // #87CEEB sky blue

        go.AddComponent<AudioListener>();

        CameraFollow cf = go.AddComponent<CameraFollow>();
        // Expose bounds in Inspector — defaults cover the 60-unit level
        SerializedObject so = new SerializedObject(cf);
        so.FindProperty("lockedY").floatValue   = 0f;
        so.FindProperty("minX").floatValue      = -5f;
        so.FindProperty("maxX").floatValue      = 65f;
        so.FindProperty("smoothTime").floatValue = 0.15f;
        so.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    // ────────────────────────────────────────────────────────────────────────
    // BACKGROUND (3-layer parallax)
    // ────────────────────────────────────────────────────────────────────────
    private static GameObject BuildBackground(Transform parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent);

        // Layer 1 — Sky (barely moves)
        GameObject sky = MakeRect("SkyLayer", new Vector3(30f, 0f, 10f), new Vector3(80f, 12f, 1f),
                                  new Color(0.529f, 0.808f, 0.922f));
        sky.GetComponent<SpriteRenderer>().sortingOrder = -20;
        sky.transform.SetParent(bg.transform);
        ParallaxBackground skyPx = sky.AddComponent<ParallaxBackground>();
        SetField(skyPx, "parallaxFactor", 0.02f);

        // Layer 2 — Buildings (moves at 0.2x)
        GameObject buildings = new GameObject("BuildingsLayer");
        buildings.transform.SetParent(bg.transform);
        buildings.transform.position = new Vector3(0f, -1f, 8f);
        ParallaxBackground buildPx = buildings.AddComponent<ParallaxBackground>();
        SetField(buildPx, "parallaxFactor", 0.2f);

        // Coloured building blocks of varying heights
        Color[] houseColors = { new Color(0.98f, 0.80f, 0.80f), new Color(0.80f, 0.90f, 0.98f),
                                  new Color(0.98f, 0.95f, 0.70f), new Color(0.95f, 0.95f, 0.95f),
                                  new Color(0.82f, 0.92f, 0.78f) };
        float[] heights = { 4f, 5.5f, 3.5f, 6f, 4.5f, 5f, 3.8f, 6.5f };
        float xPos = -5f;
        for (int i = 0; i < 12; i++)
        {
            float h = heights[i % heights.Length] + Random.Range(-0.3f, 0.3f);
            float w = Random.Range(3.5f, 5.5f);
            Color col = houseColors[i % houseColors.Length];
            GameObject house = MakeRect("House_" + i,
                new Vector3(xPos + w * 0.5f, -2f + h * 0.5f, 8f),
                new Vector3(w, h, 1f), col);
            house.GetComponent<SpriteRenderer>().sortingOrder = -15;
            house.transform.SetParent(buildings.transform);
            xPos += w + Random.Range(0.2f, 1f);
        }

        // Layer 3 — Props (moves at 0.5x)
        GameObject props = new GameObject("PropsLayer");
        props.transform.SetParent(bg.transform);
        props.transform.position = new Vector3(0f, 0f, 5f);
        ParallaxBackground propsPx = props.AddComponent<ParallaxBackground>();
        SetField(propsPx, "parallaxFactor", 0.5f);

        // Lamp posts every 8 units
        for (int i = 0; i < 8; i++)
        {
            float x = i * 8f;
            // Pole
            GameObject pole = MakeRect("LampPole_" + i, new Vector3(x, -1.5f, 5f),
                                       new Vector3(0.15f, 3f, 1f), new Color(0.25f, 0.22f, 0.20f));
            pole.GetComponent<SpriteRenderer>().sortingOrder = -8;
            pole.transform.SetParent(props.transform);
            // Lamp head
            GameObject lamp = MakeRect("LampHead_" + i, new Vector3(x + 0.4f, 0.1f, 5f),
                                       new Vector3(0.8f, 0.25f, 1f), new Color(0.95f, 0.85f, 0.50f));
            lamp.GetComponent<SpriteRenderer>().sortingOrder = -7;
            lamp.transform.SetParent(props.transform);
        }

        return bg;
    }

    // ────────────────────────────────────────────────────────────────────────
    // GROUND
    // ────────────────────────────────────────────────────────────────────────
    private static GameObject BuildGround(Transform parent)
    {
        // The level is 60 units wide, centred at x=25
        GameObject ground = MakeRect("Ground", new Vector3(25f, -4f, 0f),
                                     new Vector3(70f, 2f, 1f), new Color(0.55f, 0.50f, 0.45f));
        ground.tag = "Ground";
        ground.layer = LayerMask.NameToLayer("Ground");
        BoxCollider2D bc = ground.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one;
        ground.GetComponent<SpriteRenderer>().sortingOrder = -2;
        ground.transform.SetParent(parent);

        // Cobblestone stripe (visual only)
        GameObject stripe = MakeRect("RoadStripe", new Vector3(25f, -3.1f, 0f),
                                     new Vector3(70f, 0.12f, 1f), new Color(0.65f, 0.60f, 0.55f));
        stripe.GetComponent<SpriteRenderer>().sortingOrder = -1;
        stripe.transform.SetParent(ground.transform);

        return ground;
    }

    // ────────────────────────────────────────────────────────────────────────
    // PLATFORMS (7 platforms along the level)
    // ────────────────────────────────────────────────────────────────────────
    private static GameObject BuildPlatforms(Transform parent)
    {
        GameObject root = new GameObject("Platforms");
        root.transform.SetParent(parent);

        // (name, x, y, width) — y is height above ground (ground surface = -3)
        (string n, float x, float y, float w)[] defs =
        {
            ("Platform_01",  5f,  -1.5f, 3f),
            ("Platform_02", 10f,  -0.5f, 2.5f),
            ("Platform_03", 17f,  -2f,   4f),
            ("Platform_04", 22f,  -0.8f, 2f),
            ("Platform_05", 30f,  -1.5f, 5f),
            ("Platform_06", 37f,  -2.2f, 3f),
            ("Platform_07", 44f,  -0.5f, 2.5f),
        };

        foreach (var d in defs)
        {
            GameObject p = MakeRect(d.n, new Vector3(d.x, d.y, 0f),
                                    new Vector3(d.w, 0.5f, 1f), new Color(0.60f, 0.42f, 0.28f));
            p.tag   = "Ground";
            p.layer = LayerMask.NameToLayer("Ground");
            BoxCollider2D bc = p.AddComponent<BoxCollider2D>();
            bc.size = Vector2.one;
            p.GetComponent<SpriteRenderer>().sortingOrder = 0;
            p.transform.SetParent(root.transform);
        }

        return root;
    }

    // ────────────────────────────────────────────────────────────────────────
    // LEVEL BOUNDS
    // ────────────────────────────────────────────────────────────────────────
    private static void BuildLevelBounds(Transform parent)
    {
        // Left wall — prevents player backtracking
        GameObject leftWall = new GameObject("LeftBound");
        leftWall.transform.position = new Vector3(-2f, 0f, 0f);
        leftWall.layer = LayerMask.NameToLayer("Ground");
        BoxCollider2D lbc = leftWall.AddComponent<BoxCollider2D>();
        lbc.size = new Vector2(1f, 20f);
        leftWall.transform.SetParent(parent);

        // Right trigger — level complete
        GameObject endTrigger = new GameObject("LevelEndTrigger");
        endTrigger.transform.position = new Vector3(62f, 0f, 0f);
        endTrigger.tag = "LevelEnd";
        BoxCollider2D ebc = endTrigger.AddComponent<BoxCollider2D>();
        ebc.isTrigger = true;
        ebc.size = new Vector2(2f, 12f);
        endTrigger.AddComponent<LevelEndTrigger>();
        endTrigger.transform.SetParent(parent);
    }

    // ────────────────────────────────────────────────────────────────────────
    // CAR PREFAB
    // ────────────────────────────────────────────────────────────────────────
    private static GameObject BuildCarPrefab()
    {
        GameObject car = new GameObject("Car");
        car.tag = "Car";

        // Body sprite (trigger — damages player on side hit)
        SpriteRenderer sr = car.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color  = new Color(0.20f, 0.44f, 0.85f);  // Will be overridden by spawner
        sr.sortingOrder = 1;
        car.transform.localScale = new Vector3(3f, 1f, 1f);

        // Body trigger collider
        BoxCollider2D bodyCol = car.AddComponent<BoxCollider2D>();
        bodyCol.isTrigger = true;
        bodyCol.size      = new Vector2(0.9f, 0.85f);  // Slightly smaller than sprite

        // Top solid collider — player can stand on roof
        GameObject roof = new GameObject("Roof");
        roof.transform.SetParent(car.transform);
        roof.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        BoxCollider2D roofCol = roof.AddComponent<BoxCollider2D>();
        roofCol.size   = new Vector2(0.95f, 0.1f);
        roofCol.offset = Vector2.zero;

        car.AddComponent<CarMovement>();

        // Save prefab
        string path = PrefabPath + "Car.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(car, path);
        Object.DestroyImmediate(car);
        return prefab;
    }

    // ────────────────────────────────────────────────────────────────────────
    // CAR SPAWNER
    // ────────────────────────────────────────────────────────────────────────
    private static void BuildCarSpawner(Transform parent, GameObject carPrefab)
    {
        GameObject spawner = new GameObject("CarSpawner");
        spawner.transform.position = new Vector3(35f, -4f, 0f);
        CarSpawner cs = spawner.AddComponent<CarSpawner>();

        SerializedObject so = new SerializedObject(cs);
        so.FindProperty("carPrefab").objectReferenceValue = carPrefab;
        so.FindProperty("lane1Y").floatValue = -3f;
        so.FindProperty("lane2Y").floatValue = -2f;
        so.FindProperty("spawnXRight").floatValue = 35f;
        so.FindProperty("spawnXLeft").floatValue  = -5f;
        so.ApplyModifiedPropertiesWithoutUndo();

        spawner.transform.SetParent(parent);
    }

    // ────────────────────────────────────────────────────────────────────────
    // BULLET PREFAB
    // ────────────────────────────────────────────────────────────────────────
    private static GameObject BuildBulletPrefab()
    {
        string existingPath = "Assets/Prefabs/Combat/Bullet.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(existingPath);
        if (existing != null) return existing;

        // Build fresh if not found
        GameObject bullet = new GameObject("Bullet");
        bullet.tag = "Bullet";
        bullet.layer = LayerMask.NameToLayer("Projectile");

        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color  = new Color(1f, 0.9f, 0.2f);
        bullet.transform.localScale = new Vector3(0.35f, 0.2f, 1f);

        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D cc = bullet.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.45f;

        bullet.AddComponent<Bullet>();

        System.IO.Directory.CreateDirectory("Assets/Prefabs/Combat");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bullet, existingPath);
        Object.DestroyImmediate(bullet);
        return prefab;
    }

    // ────────────────────────────────────────────────────────────────────────
    // PLAYER (Luca)
    // ────────────────────────────────────────────────────────────────────────
    private static GameObject BuildPlayer(Transform parent, GameObject bulletPrefab)
    {
        GameObject go = new GameObject("Luca");
        go.tag   = "Player";
        go.layer = LayerMask.NameToLayer("Player");
        go.transform.position = new Vector3(0f, -2f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color  = new Color(0.87f, 0.72f, 0.53f);  // Warm cream/tan
        sr.sortingOrder = 5;
        go.transform.localScale = new Vector3(1f, 1.8f, 1f);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size   = new Vector2(0.85f, 0.95f);

        // Ground check child
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(go.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.5f, 0f);

        // Fire point child
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(go.transform);
        firePoint.transform.localPosition = new Vector3(0.6f, 0f, 0f);

        // Scripts
        PlayerMovement pm = go.AddComponent<PlayerMovement>();
        SerializedObject pmSo = new SerializedObject(pm);
        pmSo.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        pmSo.FindProperty("groundLayer").intValue = LayerMask.GetMask("Ground");
        pmSo.ApplyModifiedPropertiesWithoutUndo();

        PlayerHealth health = go.AddComponent<PlayerHealth>();

        PlayerShooting ps = go.AddComponent<PlayerShooting>();
        SerializedObject psSo = new SerializedObject(ps);
        psSo.FindProperty("playerMovement").objectReferenceValue = pm;
        psSo.FindProperty("firePoint").objectReferenceValue      = firePoint.transform;
        psSo.FindProperty("bulletPrefab").objectReferenceValue   = bulletPrefab;
        psSo.ApplyModifiedPropertiesWithoutUndo();

        go.AddComponent<AudioSource>();

        go.transform.SetParent(parent);

        // Save as prefab
        string path = PrefabPath + "Luca.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);

        return go;
    }

    // ────────────────────────────────────────────────────────────────────────
    // ENEMIES (5 UbuntyWalkers)
    // ────────────────────────────────────────────────────────────────────────
    private static void BuildEnemies(Transform parent)
    {
        // (x position, patrol half-width)
        (float x, float hw)[] placements = {
            (8f,  2.5f), (15f, 2f), (24f, 3f), (33f, 2f), (46f, 2.5f)
        };

        for (int i = 0; i < placements.Length; i++)
        {
            float x = placements[i].x;
            float hw = placements[i].hw;

            GameObject go = new GameObject("UbuntyWalker_0" + (i + 1));
            go.tag   = "Enemy";
            go.layer = LayerMask.NameToLayer("Enemy");
            go.transform.position = new Vector3(x, -2.4f, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color  = new Color(0.85f, 0.18f, 0.18f);  // Villain red
            sr.sortingOrder = 4;
            go.transform.localScale = new Vector3(0.8f, 1.2f, 1f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(0.9f, 0.9f);

            // Patrol points
            GameObject left  = new GameObject("LeftPoint");
            left.transform.SetParent(go.transform);
            left.transform.localPosition = new Vector3(-hw / go.transform.localScale.x, 0f, 0f);

            GameObject right = new GameObject("RightPoint");
            right.transform.SetParent(go.transform);
            right.transform.localPosition = new Vector3(hw / go.transform.localScale.x, 0f, 0f);

            Enemy enemy = go.AddComponent<Enemy>();
            SerializedObject so = new SerializedObject(enemy);
            so.FindProperty("leftPoint").objectReferenceValue  = left.transform;
            so.FindProperty("rightPoint").objectReferenceValue = right.transform;
            so.FindProperty("maxHealth").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            go.transform.SetParent(parent);

            // Save first enemy as prefab template
            if (i == 0)
                PrefabUtility.SaveAsPrefabAsset(go, PrefabPath + "UbuntyWalker.prefab");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // UI (Canvas with HP label)
    // ────────────────────────────────────────────────────────────────────────
    private static TextMeshProUGUI BuildUI(Transform parent)
    {
        // Canvas
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(parent);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // HP text
        GameObject textGO = new GameObject("HPText");
        textGO.transform.SetParent(canvasGO.transform);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "HP: 5";
        tmp.fontSize  = 36;
        tmp.color     = Color.white;

        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(200f, 60f);

        return tmp;
    }

    // ────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ────────────────────────────────────────────────────────────────────────
    private static GameObject MakeRect(string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color  = color;
        return go;
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private static void SetField(Object target, string field, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty sp = so.FindProperty(field);
        if (sp != null) { sp.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    private static void EnsureFolders()
    {
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        System.IO.Directory.CreateDirectory(PrefabPath);
        System.IO.Directory.CreateDirectory("Assets/Prefabs/Combat");
    }
}

/// <summary>
/// Tiny trigger script placed at the right edge of the level.
/// Logs "Level Complete!" when the player reaches the end.
/// Replace with a proper scene-transition later.
/// </summary>
public class LevelEndTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Debug.Log("Level Complete!");
    }
}
