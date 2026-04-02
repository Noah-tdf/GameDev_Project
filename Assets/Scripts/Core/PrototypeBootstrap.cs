using UnityEngine;
using UnityEngine.SceneManagement;

public class PrototypeBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BuildPrototypeOnPlay()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return;
        }

        // Don't run in Level1 or any scene that already has Luca / a camera / a GameManager
        if (activeScene.name == "Level1") return;
        if (GameObject.Find("Player") != null) return;
        if (GameObject.Find("Luca")   != null) return;
        if (GameObject.Find("Main Camera") != null) return;
        if (Object.FindFirstObjectByType<GameManager>() != null) return;

        CreateWorld();
    }

    private static void CreateWorld()
    {
        CreateGameManager();
        CameraFollow cameraFollow = CreateCamera();
        CreateBackground();
        CreateGround();
        CreatePlatform("Platform_01", new Vector3(-2.5f, -1.6f, 0f), new Vector3(3f, 0.5f, 1f));
        CreatePlatform("Platform_02", new Vector3(2.25f, -0.4f, 0f), new Vector3(3f, 0.5f, 1f));
        CreatePlatform("Platform_03", new Vector3(6f, 0.9f, 0f), new Vector3(2.5f, 0.5f, 1f));

        GameObject bulletPrefab = CreateBulletPrefab();
        GameObject player = CreatePlayer(bulletPrefab);
        CreateEnemy();

        cameraFollow.SetTarget(player.transform);
    }

    private static void CreateGameManager()
    {
        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();
    }

    private static CameraFollow CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 1.5f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.83f, 0.83f, 0.83f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        cameraObject.AddComponent<AudioListener>();

        return cameraObject.AddComponent<CameraFollow>();
    }

    private static void CreateBackground()
    {
        GameObject background = CreateRectangle("Background", new Vector3(0f, 1.5f, 5f), new Vector3(18f, 10f, 1f), new Color(0.75f, 0.75f, 0.75f));
        background.GetComponent<SpriteRenderer>().sortingOrder = -10;
    }

    private static void CreateGround()
    {
        GameObject ground = CreateRectangle("Ground", new Vector3(0f, -3.75f, 0f), new Vector3(20f, 1.5f, 1f), new Color(0.15f, 0.15f, 0.15f));
        ground.tag = "Ground";
        SetLayerIfAvailable(ground, "Ground");
        ground.AddComponent<BoxCollider2D>().size = Vector2.one;
    }

    private static void CreatePlatform(string objectName, Vector3 position, Vector3 scale)
    {
        GameObject platform = CreateRectangle(objectName, position, scale, new Color(0.25f, 0.25f, 0.25f));
        platform.tag = "Platform";
        SetLayerIfAvailable(platform, "Ground");
        platform.AddComponent<BoxCollider2D>().size = Vector2.one;
    }

    private static GameObject CreatePlayer(GameObject bulletPrefab)
    {
        GameObject player = CreateRectangle("Player", new Vector3(-6f, -2.3f, 0f), new Vector3(0.9f, 1.4f, 1f), Color.white);
        player.tag = "Player";
        SetLayerIfAvailable(player, "Player");

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        CapsuleCollider2D capsuleCollider = player.AddComponent<CapsuleCollider2D>();
        capsuleCollider.direction = CapsuleDirection2D.Vertical;
        capsuleCollider.size = new Vector2(0.8f, 1.4f);

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.72f, 0f);

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(player.transform);
        firePoint.transform.localPosition = new Vector3(0.65f, 0.05f, 0f);

        PlayerMovement movement = player.AddComponent<PlayerMovement>();
        AssignPlayerMovementReferences(movement, groundCheck.transform);

        PlayerShooting shooting = player.AddComponent<PlayerShooting>();
        AssignPlayerShootingReferences(shooting, movement, firePoint.transform, bulletPrefab);

        return player;
    }

    private static GameObject CreateBulletPrefab()
    {
        GameObject bulletPrefab = CreateRectangle("BulletPrefab_Runtime", new Vector3(1000f, 1000f, 0f), new Vector3(0.35f, 0.2f, 1f), new Color(0.05f, 0.05f, 0.05f));
        bulletPrefab.tag = "Bullet";
        SetLayerIfAvailable(bulletPrefab, "Projectile");

        Rigidbody2D rb = bulletPrefab.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D circleCollider = bulletPrefab.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.45f;

        bulletPrefab.AddComponent<Bullet>();
        bulletPrefab.SetActive(false);

        return bulletPrefab;
    }

    private static void CreateEnemy()
    {
        GameObject enemy = CreateRectangle("Enemy_Test", new Vector3(4.5f, -2.3f, 0f), new Vector3(0.9f, 1.2f, 1f), new Color(0.9f, 0.9f, 0.9f));
        enemy.tag = "Enemy";
        SetLayerIfAvailable(enemy, "Enemy");

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D boxCollider = enemy.AddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(0.8f, 1.2f);

        GameObject leftPoint = new GameObject("LeftPoint");
        leftPoint.transform.SetParent(enemy.transform);
        leftPoint.transform.localPosition = new Vector3(-2f, 0f, 0f);

        GameObject rightPoint = new GameObject("RightPoint");
        rightPoint.transform.SetParent(enemy.transform);
        rightPoint.transform.localPosition = new Vector3(2f, 0f, 0f);

        Enemy enemyScript = enemy.AddComponent<Enemy>();
        AssignEnemyReferences(enemyScript, leftPoint.transform, rightPoint.transform);
    }

    private static GameObject CreateRectangle(string objectName, Vector3 position, Vector3 scale, Color color)
    {
        GameObject go = new GameObject(objectName);
        go.transform.position = position;
        go.transform.localScale = scale;

        SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateSquareSprite();
        spriteRenderer.color = color;

        return go;
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private static void SetLayerIfAvailable(GameObject target, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            target.layer = layer;
        }
    }

    private static void AssignPlayerMovementReferences(PlayerMovement movement, Transform groundCheck)
    {
        movement.GetType().GetField("groundCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(movement, groundCheck);
        movement.GetType().GetField("groundLayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(movement, LayerMask.GetMask("Ground"));
    }

    private static void AssignPlayerShootingReferences(PlayerShooting shooting, PlayerMovement movement, Transform firePoint, GameObject bulletPrefab)
    {
        shooting.GetType().GetField("playerMovement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(shooting, movement);
        shooting.GetType().GetField("firePoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(shooting, firePoint);
        shooting.GetType().GetField("bulletPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(shooting, bulletPrefab);
    }

    private static void AssignEnemyReferences(Enemy enemy, Transform leftPoint, Transform rightPoint)
    {
        enemy.GetType().GetField("leftPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemy, leftPoint);
        enemy.GetType().GetField("rightPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemy, rightPoint);
    }
}
