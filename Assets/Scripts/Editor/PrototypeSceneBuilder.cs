using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/PrototypeScene.unity";
    private const string PlaceholderFolder = "Assets/Art/Placeholders";
    private const string MaterialFolder = "Assets/Materials";

    [MenuItem("Ubuntu City/Build Prototype Demo Scene")]
    public static void BuildPrototypeScene()
    {
        EnsureFolders();
        EnsurePlaceholderAssets();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "PrototypeScene";

        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(PlaceholderFolder, "Background.png"));
        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(PlaceholderFolder, "GroundBlock.png"));
        Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(PlaceholderFolder, "Player.png"));
        Sprite bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(PlaceholderFolder, "Bullet.png"));
        Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(PlaceholderFolder, "Enemy.png"));

        Material grayscaleMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetPath(MaterialFolder, "PlaceholderSprite.mat"));

        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();

        CreateCamera();
        CreateLight();
        CreateLevel(backgroundSprite, groundSprite, grayscaleMaterial);
        GameObject bulletPrefab = CreateBulletPrefab(bulletSprite, grayscaleMaterial);
        GameObject player = CreatePlayer(playerSprite, bulletPrefab, grayscaleMaterial);
        CreateEnemy(enemySprite, grayscaleMaterial);

        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(player.transform);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Ubuntu City",
            "PrototypeScene has been created.\n\nOpen the scene and press Play.\nControls: A/D or Left/Right to move, Space to jump, J to shoot.",
            "OK");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.83f, 0.83f, 0.83f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        cameraObject.AddComponent<AudioListener>();

        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        cameraObject.transform.position = new Vector3(0f, 1.5f, -10f);
        follow.SetTarget(null);
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Prototype Light Placeholder");
        lightObject.transform.position = new Vector3(0f, 2f, 0f);
    }

    private static void CreateLevel(Sprite backgroundSprite, Sprite groundSprite, Material material)
    {
        GameObject level = new GameObject("Level");

        GameObject background = CreateSpriteObject("Background", backgroundSprite, new Vector3(0f, 1.5f, 5f), new Vector3(18f, 10f, 1f), material, new Color(0.7f, 0.7f, 0.7f));
        background.transform.SetParent(level.transform);

        GameObject ground = CreateSpriteObject("Ground", groundSprite, new Vector3(0f, -3.75f, 0f), new Vector3(20f, 1.5f, 1f), material, new Color(0.15f, 0.15f, 0.15f));
        ground.tag = "Ground";
        ground.layer = LayerMask.NameToLayer("Ground");
        ground.AddComponent<BoxCollider2D>().size = Vector2.one;
        ground.transform.SetParent(level.transform);

        CreatePlatform(level.transform, "Platform_01", new Vector3(-2.5f, -1.6f, 0f), new Vector3(3f, 0.5f, 1f), groundSprite, material);
        CreatePlatform(level.transform, "Platform_02", new Vector3(2.25f, -0.4f, 0f), new Vector3(3f, 0.5f, 1f), groundSprite, material);
        CreatePlatform(level.transform, "Platform_03", new Vector3(6f, 0.9f, 0f), new Vector3(2.5f, 0.5f, 1f), groundSprite, material);
    }

    private static void CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale, Sprite sprite, Material material)
    {
        GameObject platform = CreateSpriteObject(name, sprite, position, scale, material, new Color(0.25f, 0.25f, 0.25f));
        platform.tag = "Platform";
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.AddComponent<BoxCollider2D>().size = Vector2.one;
        platform.transform.SetParent(parent);
    }

    private static GameObject CreatePlayer(Sprite playerSprite, GameObject bulletPrefab, Material material)
    {
        GameObject player = CreateSpriteObject("Player", playerSprite, new Vector3(-6f, -2.3f, 0f), new Vector3(0.9f, 1.4f, 1f), material, Color.white);
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Player");

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Vertical;
        collider.size = new Vector2(0.8f, 1.4f);

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.72f, 0f);

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(player.transform);
        firePoint.transform.localPosition = new Vector3(0.65f, 0.05f, 0f);

        PlayerMovement movement = player.AddComponent<PlayerMovement>();
        SetPrivateField(movement, "groundCheck", groundCheck.transform);
        SetPrivateField(movement, "groundLayer", LayerMask.GetMask("Ground"));

        PlayerShooting shooting = player.AddComponent<PlayerShooting>();
        SetPrivateField(shooting, "playerMovement", movement);
        SetPrivateField(shooting, "firePoint", firePoint.transform);
        SetPrivateField(shooting, "bulletPrefab", bulletPrefab);

        PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Characters/Player.prefab");
        Object.DestroyImmediate(player);

        GameObject playerInstance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab")) as GameObject;
        playerInstance.transform.position = new Vector3(-6f, -2.3f, 0f);
        return playerInstance;
    }

    private static GameObject CreateBulletPrefab(Sprite bulletSprite, Material material)
    {
        GameObject bullet = CreateSpriteObject("Bullet", bulletSprite, Vector3.zero, new Vector3(0.35f, 0.2f, 1f), material, Color.white);
        bullet.tag = "Bullet";
        bullet.layer = LayerMask.NameToLayer("Projectile");

        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D collider = bullet.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.45f;

        bullet.AddComponent<Bullet>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bullet, "Assets/Prefabs/Combat/Bullet.prefab");
        Object.DestroyImmediate(bullet);
        return prefab;
    }

    private static void CreateEnemy(Sprite enemySprite, Material material)
    {
        GameObject enemy = CreateSpriteObject("Enemy_Test", enemySprite, new Vector3(4.5f, -2.3f, 0f), new Vector3(0.9f, 1.2f, 1f), material, new Color(0.9f, 0.9f, 0.9f));
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.8f, 1.2f);

        GameObject leftPoint = new GameObject("LeftPoint");
        leftPoint.transform.SetParent(enemy.transform);
        leftPoint.transform.localPosition = new Vector3(-2f, 0f, 0f);

        GameObject rightPoint = new GameObject("RightPoint");
        rightPoint.transform.SetParent(enemy.transform);
        rightPoint.transform.localPosition = new Vector3(2f, 0f, 0f);

        Enemy enemyScript = enemy.AddComponent<Enemy>();
        SetPrivateField(enemyScript, "leftPoint", leftPoint.transform);
        SetPrivateField(enemyScript, "rightPoint", rightPoint.transform);

        PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/Prefabs/Characters/Enemy_Test.prefab");
        Object.DestroyImmediate(enemy);

        GameObject enemyInstance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Enemy_Test.prefab")) as GameObject;
        enemyInstance.transform.position = new Vector3(4.5f, -2.3f, 0f);
    }

    private static GameObject CreateSpriteObject(string objectName, Sprite sprite, Vector3 position, Vector3 scale, Material material, Color color)
    {
        GameObject go = new GameObject(objectName);
        go.transform.position = position;
        go.transform.localScale = scale;

        SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.material = material;
        return go;
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory("Assets/Prefabs/Characters");
        Directory.CreateDirectory("Assets/Prefabs/Combat");
        Directory.CreateDirectory("Assets/Art/Placeholders");
        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Scripts/Editor");
    }

    private static void EnsurePlaceholderAssets()
    {
        CreateTextureIfMissing(Path.Combine(PlaceholderFolder, "Background.png"), 64, 64, new Color(0.7f, 0.7f, 0.7f));
        CreateTextureIfMissing(Path.Combine(PlaceholderFolder, "GroundBlock.png"), 64, 64, new Color(0.2f, 0.2f, 0.2f));
        CreateTextureIfMissing(Path.Combine(PlaceholderFolder, "Player.png"), 48, 72, new Color(1f, 1f, 1f));
        CreateTextureIfMissing(Path.Combine(PlaceholderFolder, "Enemy.png"), 48, 64, new Color(0.9f, 0.9f, 0.9f));
        CreateTextureIfMissing(Path.Combine(PlaceholderFolder, "Bullet.png"), 24, 12, new Color(0.05f, 0.05f, 0.05f));

        ConfigureTextureImporter(Path.Combine(PlaceholderFolder, "Background.png"), 64);
        ConfigureTextureImporter(Path.Combine(PlaceholderFolder, "GroundBlock.png"), 64);
        ConfigureTextureImporter(Path.Combine(PlaceholderFolder, "Player.png"), 48);
        ConfigureTextureImporter(Path.Combine(PlaceholderFolder, "Enemy.png"), 48);
        ConfigureTextureImporter(Path.Combine(PlaceholderFolder, "Bullet.png"), 24);

        string materialPath = AssetPath(MaterialFolder, "PlaceholderSprite.mat");
        if (!File.Exists(materialPath))
        {
            Shader shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
    }

    private static string AssetPath(string folder, string fileName)
    {
        return $"{folder}/{fileName}";
    }

    private static void CreateTextureIfMissing(string path, int width, int height, Color color)
    {
        if (File.Exists(path))
        {
            return;
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = color;
        }

        texture.SetPixels(colors);
        texture.Apply();

        byte[] png = texture.EncodeToPNG();
        File.WriteAllBytes(path, png);
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
    }

    private static void ConfigureTextureImporter(string path, int pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.SaveAndReimport();
    }

    private static void SetPrivateField(Object target, string fieldName, object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);

        if (property == null)
        {
            return;
        }

        switch (value)
        {
            case Transform transformValue:
                property.objectReferenceValue = transformValue;
                break;
            case GameObject gameObjectValue:
                property.objectReferenceValue = gameObjectValue;
                break;
            case PlayerMovement movementValue:
                property.objectReferenceValue = movementValue;
                break;
            case int intValue:
                property.intValue = intValue;
                break;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
