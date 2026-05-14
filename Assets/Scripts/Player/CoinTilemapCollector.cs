using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class CoinTilemapCollector : MonoBehaviour
{
    private const string CreditsKey = "PlayerCredits";
    private const string CounterPrefix = ":";
    private static readonly Vector2 CounterAnchor = new Vector2(0f, 1f);
    private static readonly Vector2 CounterPosition = new Vector2(68f, -70f); 
    private static readonly Vector2 CounterSize = new Vector2(420f, 60f);
    private const string CoinSpritePath = "Assets/Art/Coins/coin3_20x20.png";
    private const string FontPath = "Assets/Fonts/Astron-Bold.otf";

    [SerializeField] private int valuePerCoin = 1;
    [SerializeField] private string coinTilemapNameContains = "coin";
    [SerializeField] private bool createCoinHud = true;

    private readonly List<Tilemap> coinTilemaps = new List<Tilemap>();
    private Collider2D playerCollider;
    private TextMeshProUGUI coinText;
    private Image coinIcon;
    private static TMP_FontAsset cachedFont;

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
        FindCoinTilemaps();
        CreateCoinHud();
        RefreshCoinHud();
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
        if (!createCoinHud || coinText != null)
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

        GameObject existingText = GameObject.Find("CoinCounterText");
        if (existingText != null && existingText.TryGetComponent(out TextMeshProUGUI foundText))
        {
            coinText = foundText;
            ConfigureCoinText(coinText);
        }
        else
        {
            GameObject textObject = new GameObject("CoinCounterText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasObject.transform, false);
            coinText = textObject.GetComponent<TextMeshProUGUI>();
            ConfigureCoinText(coinText);
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
    }

    private void ConfigureCoinIcon(Image image)
    {
        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = CounterAnchor;
        rectTransform.anchorMax = CounterAnchor;
        rectTransform.pivot = CounterAnchor;
        rectTransform.anchoredPosition = new Vector2(12f, -73.5f); // Adjusted for center alignment
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

    private static void ConfigureCoinText(TextMeshProUGUI text)
    {
        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = CounterAnchor;
        rectTransform.anchorMax = CounterAnchor;
        rectTransform.pivot = CounterAnchor;
        rectTransform.anchoredPosition = CounterPosition;
        rectTransform.sizeDelta = CounterSize;

        text.alignment = TextAlignmentOptions.Left;
        text.fontSize = 40.5f; 
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.86f, 0.25f, 1f);
        text.outlineWidth = 0.16f;
        text.raycastTarget = false;

        #if UNITY_EDITOR
        if (cachedFont == null)
        {
            Font font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font != null)
            {
                cachedFont = TMP_FontAsset.CreateFontAsset(font);
                cachedFont.name = "Astron-Bold (CoinHUD)";
            }
        }
        if (cachedFont != null) text.font = cachedFont;
        #endif
    }

    private void RefreshCoinHud()
    {
        if (coinText != null)
            coinText.text = CounterPrefix + PlayerPrefs.GetInt(CreditsKey, 0);
    }
}
