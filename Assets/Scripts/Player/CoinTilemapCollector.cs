using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class CoinTilemapCollector : MonoBehaviour
{
    private const string CreditsKey = "PlayerCredits";
    private const string NumpadPath = "Assets/Art/Coins/NumpadFree/Numpad_Light.png";
    private static readonly Vector2 CounterAnchor = new Vector2(0f, 1f);
    private static readonly Vector2 CounterPosition = new Vector2(12f, -73.5f); 
    private const string CoinSpritePath = "Assets/Art/Coins/coin3_20x20.png";

    [SerializeField] private int valuePerCoin = 1;
    [SerializeField] private string coinTilemapNameContains = "coin";
    [SerializeField] private bool createCoinHud = true;
    [SerializeField] private float digitSpacing = -5f; // Tighter spacing for numpad digits
    [SerializeField] private Vector2 digitSize = new Vector2(45f, 45f);

    private readonly List<Tilemap> coinTilemaps = new List<Tilemap>();
    private Collider2D playerCollider;
    private RectTransform digitsContainer;
    private List<Image> digitImages = new List<Image>();
    private Image coinIcon;
    private Sprite[] numpadSprites;

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
        LoadNumpadSprites();
        FindCoinTilemaps();
        CreateCoinHud();
        RefreshCoinHud();
    }

    private void LoadNumpadSprites()
    {
        #if UNITY_EDITOR
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(NumpadPath);
        List<Sprite> sprites = new List<Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite s) sprites.Add(s);
        }
        // Sort by name to ensure 0-9 order
        sprites.Sort((a, b) => a.name.CompareTo(b.name));
        numpadSprites = sprites.ToArray();
        #endif
    }

    private void OnEnable()
    {
        FindCoinTilemaps();
        RefreshCoinHud();
    }

    private void Update()
    {
        if (playerCollider == null)
            return;

        if (coinTilemaps.Count == 0)
            FindCoinTilemaps();

        Bounds playerBounds = playerCollider.bounds;
        for (int i = coinTilemaps.Count - 1; i >= 0; i--)
        {
            Tilemap coinTilemap = coinTilemaps[i];
            if (coinTilemap == null)
            {
                coinTilemaps.RemoveAt(i);
                continue;
            }

            CollectTouchedTiles(coinTilemap, playerBounds);
        }
    }

    private void FindCoinTilemaps()
    {
        coinTilemaps.Clear();
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null)
                continue;

            string objectName = tilemap.gameObject.name;
            if (objectName.ToLowerInvariant().Contains(coinTilemapNameContains.ToLowerInvariant()))
                coinTilemaps.Add(tilemap);
        }
    }

    private void CollectTouchedTiles(Tilemap coinTilemap, Bounds playerBounds)
    {
        Vector3Int minCell = coinTilemap.WorldToCell(playerBounds.min);
        Vector3Int maxCell = coinTilemap.WorldToCell(playerBounds.max);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!coinTilemap.HasTile(cell))
                    continue;

                coinTilemap.SetTile(cell, null);
                AddCoins(valuePerCoin);
            }
        }
    }

    private void AddCoins(int amount)
    {
        int currentCoins = PlayerPrefs.GetInt(CreditsKey, 0);
        PlayerPrefs.SetInt(CreditsKey, currentCoins + amount);
        PlayerPrefs.Save();
        RefreshCoinHud();
    }

    private void CreateCoinHud()
    {
        if (!createCoinHud || digitsContainer != null)
            return;

        GameObject canvasObject = GameObject.Find("CoinCounterCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("CoinCounterCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 101;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Add Icon
        GameObject iconObject = GameObject.Find("CoinCounterIcon");
        if (iconObject == null)
        {
            iconObject = new GameObject("CoinCounterIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(canvasObject.transform, false);
        }
        coinIcon = iconObject.GetComponent<Image>();
        ConfigureCoinIcon(coinIcon);

        // Add Digits Container
        GameObject containerObject = GameObject.Find("CoinDigitsContainer");
        if (containerObject == null)
        {
            containerObject = new GameObject("CoinDigitsContainer", typeof(RectTransform));
            containerObject.transform.SetParent(canvasObject.transform, false);
        }
        digitsContainer = containerObject.GetComponent<RectTransform>();
        ConfigureDigitsContainer(digitsContainer);
    }

    private void ConfigureDigitsContainer(RectTransform rect)
    {
        rect.anchorMin = CounterAnchor;
        rect.anchorMax = CounterAnchor;
        rect.pivot = CounterAnchor;
        // Position it to the right of the icon
        rect.anchoredPosition = new Vector2(70f, -73.5f);
        rect.sizeDelta = new Vector2(400f, 50f);

        HorizontalLayoutGroup layout = rect.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = digitSpacing;
    }

    private void ConfigureCoinIcon(Image image)
    {
        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = CounterAnchor;
        rectTransform.anchorMax = CounterAnchor;
        rectTransform.pivot = CounterAnchor;
        rectTransform.anchoredPosition = CounterPosition;
        rectTransform.sizeDelta = new Vector2(47f, 47f); 

        // Load the specific sprite (facing front)
        #if UNITY_EDITOR
        Object[] allSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(CoinSpritePath);
        foreach (var obj in allSprites)
        {
            if (obj is Sprite s && s.name == "coin3_20x20_0")
            {
                image.sprite = s;
                break;
            }
        }
        #endif
        
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private void RefreshCoinHud()
    {
        if (digitsContainer == null || numpadSprites == null || numpadSprites.Length < 10)
            return;

        string coinStr = PlayerPrefs.GetInt(CreditsKey, 0).ToString();
        
        // Ensure enough digit images exist
        while (digitImages.Count < coinStr.Length)
        {
            GameObject digitObj = new GameObject("Digit", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            digitObj.transform.SetParent(digitsContainer, false);
            Image img = digitObj.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            RectTransform rt = digitObj.GetComponent<RectTransform>();
            rt.sizeDelta = digitSize;
            digitImages.Add(img);
        }

        // Update sprites and visibility
        for (int i = 0; i < digitImages.Count; i++)
        {
            if (i < coinStr.Length)
            {
                digitImages[i].gameObject.SetActive(true);
                int digitValue = int.Parse(coinStr[i].ToString());
                digitImages[i].sprite = numpadSprites[digitValue];
            }
            else
            {
                digitImages[i].gameObject.SetActive(false);
            }
        }
    }
}
