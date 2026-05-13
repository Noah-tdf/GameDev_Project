using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LoreController : MonoBehaviour
{
    [System.Serializable]
    public struct LorePanel
    {
        public string title;
        [TextArea(2, 4)] public string subtitle;
        public Vector2 focusPoint;   // normalized [0..1] point on master image to center on
        public float zoom;            // 1 = whole image fits, >1 = zoomed in
        public float holdDuration;    // seconds to dwell after pan completes
    }

    [Header("References")]
    [SerializeField] private Sprite loreMasterSprite;
    [SerializeField] private TMP_FontAsset subtitleFont;
    [SerializeField] private Font stencilSourceFont;

    [Header("Sequence")]
    [SerializeField] private LorePanel[] panels = DefaultPanels();
    [SerializeField] private float panDuration = 1.6f;
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    [SerializeField] private float finalRevealDuration = 3.2f;
    [SerializeField] private float finalRevealHoldDuration = 1.2f;
    [SerializeField] private float startZoom = 1f;
    [SerializeField] private Vector2 startFocus = new Vector2(0.5f, 0.5f);

    [Header("Subtitles")]
    [SerializeField] private Color subtitleColor = new Color(0.92f, 0.88f, 0.78f, 1f);
    [SerializeField] private float subtitleFontSize = 38f;

    [Header("After Sequence")]
    [SerializeField] private string nextSceneName = "Level1";

    private static TMP_FontAsset cachedStencilFontAsset;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform viewport;
    private Image backgroundImage;
    private Image loreImage;
    private RectTransform loreImageRect;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI skipPromptText;
    private bool skipRequested;
    private bool isPlaying;
    private bool isLoadingScene;
    private bool finalRevealStarted;
    private bool finalRevealComplete;
    private float skipEnabledTime;
    private float imageNativeAspect = 1f;

    private void Awake()
    {
        ResolveReferences();
        BuildUI();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (skipRequested)
            return;

        if (Time.unscaledTime < skipEnabledTime)
            return;

        bool anyKey = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (anyKey)
        {
            if (finalRevealStarted && !finalRevealComplete)
                return;

            skipRequested = true;
        }
    }

    private void ResolveReferences()
    {
#if UNITY_EDITOR
        if (loreMasterSprite == null)
            loreMasterSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Lore/Gemini_Generated_Image_q4pd15q4pd15q4pd.png");

        if (stencilSourceFont == null)
            stencilSourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BlackOpsOne-Regular.ttf");

        if (subtitleFont == null)
            subtitleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Dunkerque-Regular-FREE-FOR-PERSONAL-USE-ONLY SDF.asset");
#endif

        TMP_FontAsset stencil = ResolveStencilFontAsset();
        if (stencil != null)
            subtitleFont = stencil;

        if (loreMasterSprite != null && loreMasterSprite.rect.height > 0f)
            imageNativeAspect = loreMasterSprite.rect.width / loreMasterSprite.rect.height;
    }

    private TMP_FontAsset ResolveStencilFontAsset()
    {
        if (cachedStencilFontAsset != null)
            return cachedStencilFontAsset;
        if (stencilSourceFont == null)
            return null;
        cachedStencilFontAsset = TMP_FontAsset.CreateFontAsset(stencilSourceFont);
        if (cachedStencilFontAsset != null)
            cachedStencilFontAsset.name = stencilSourceFont.name + " SDF (Runtime)";
        return cachedStencilFontAsset;
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("LoreCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        backgroundImage = CreateImage("Background", canvasObject.transform, null, Color.black);
        Stretch(backgroundImage.rectTransform);
        backgroundImage.raycastTarget = true;

        viewport = CreateRect("LoreViewport", canvasObject.transform);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        loreImage = CreateImage("LoreMasterImage", viewport, loreMasterSprite, Color.white);
        loreImageRect = loreImage.rectTransform;
        loreImage.preserveAspect = true;
        loreImage.raycastTarget = false;
        loreImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        loreImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        loreImageRect.pivot = new Vector2(0.5f, 0.5f);
        loreImageRect.sizeDelta = ImageSizeForZoom(1f);

        subtitleText = CreateText("SubtitleText", canvasObject.transform, string.Empty, subtitleFontSize, TextAlignmentOptions.Center);
        subtitleText.rectTransform.anchorMin = new Vector2(0.1f, 0f);
        subtitleText.rectTransform.anchorMax = new Vector2(0.9f, 0f);
        subtitleText.rectTransform.pivot = new Vector2(0.5f, 0f);
        subtitleText.rectTransform.anchoredPosition = new Vector2(0f, 70f);
        subtitleText.rectTransform.sizeDelta = new Vector2(0f, 160f);
        subtitleText.color = subtitleColor;
        subtitleText.gameObject.SetActive(false);

        skipPromptText = CreateText("SkipPrompt", canvasObject.transform, "Press any key to skip", 22f, TextAlignmentOptions.BottomRight);
        skipPromptText.rectTransform.anchorMin = new Vector2(1f, 0f);
        skipPromptText.rectTransform.anchorMax = new Vector2(1f, 0f);
        skipPromptText.rectTransform.pivot = new Vector2(1f, 0f);
        skipPromptText.rectTransform.anchoredPosition = new Vector2(-30f, 22f);
        skipPromptText.rectTransform.sizeDelta = new Vector2(420f, 40f);
        skipPromptText.color = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, 0.55f);
    }

    public void PlayAndLoadScene(string sceneName)
    {
        if (isPlaying)
            return;

        if (!string.IsNullOrEmpty(sceneName))
            nextSceneName = sceneName;

        isLoadingScene = false;
        gameObject.SetActive(true);
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        isPlaying = true;
        skipRequested = false;
        finalRevealStarted = false;
        finalRevealComplete = false;
        skipEnabledTime = Time.unscaledTime + 0.35f;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.alpha = 0f;
        }

        ResetVisualAlpha();
        ApplyFocus(startFocus, startZoom);
        yield return Fade(0f, 1f, fadeInDuration);

        for (int i = 0; i < panels.Length; i++)
        {
            if (skipRequested)
                break;
            yield return ShowPanel(panels[i]);
        }

        if (!skipRequested)
        {
            finalRevealStarted = true;
            yield return ShowFinalReveal();

            finalRevealComplete = true;
            while (!skipRequested && !isLoadingScene)
                yield return null;
        }

        if (isLoadingScene)
            yield break;

        yield return FadeLoreToBlack(fadeOutDuration);
        FinishSequence();
    }

    private IEnumerator ShowPanel(LorePanel panel)
    {
        Vector2 startPos = loreImageRect.anchoredPosition;
        Vector2 startSize = loreImageRect.sizeDelta;
        Vector2 endSize = ImageSizeForZoom(Mathf.Max(0.1f, panel.zoom));
        Vector2 endPos = AnchoredPositionForFocus(panel.focusPoint, endSize);

        float t = 0f;
        while (t < panDuration)
        {
            if (skipRequested)
                yield break;
            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / panDuration));
            Vector2 currentSize = Vector2.Lerp(startSize, endSize, u);
            loreImageRect.sizeDelta = currentSize;
            loreImageRect.anchoredPosition = ClampAnchoredPosition(Vector2.Lerp(startPos, endPos, u), currentSize);
            yield return null;
        }

        loreImageRect.sizeDelta = endSize;
        loreImageRect.anchoredPosition = endPos;

        float hold = panel.holdDuration > 0f ? panel.holdDuration : 2.4f;
        float h = 0f;
        while (h < hold && !skipRequested)
        {
            h += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator ShowFinalReveal()
    {
        Vector2 startPos = loreImageRect.anchoredPosition;
        Vector2 startSize = loreImageRect.sizeDelta;
        Vector2 endSize = ImageSizeForZoom(1f);
        Vector2 endPos = AnchoredPositionForFocus(new Vector2(0.5f, 0.5f), endSize);

        float t = 0f;
        while (t < finalRevealDuration)
        {
            if (skipRequested)
                yield break;

            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / finalRevealDuration));
            Vector2 currentSize = Vector2.Lerp(startSize, endSize, u);
            loreImageRect.sizeDelta = currentSize;
            loreImageRect.anchoredPosition = ClampAnchoredPosition(Vector2.Lerp(startPos, endPos, u), currentSize);
            yield return null;
        }

        loreImageRect.sizeDelta = endSize;
        loreImageRect.anchoredPosition = endPos;
    }

    private void ApplyFocus(Vector2 focus, float zoom)
    {
        Vector2 size = ImageSizeForZoom(Mathf.Max(0.1f, zoom));
        loreImageRect.sizeDelta = size;
        loreImageRect.anchoredPosition = AnchoredPositionForFocus(focus, size);
    }

    private Vector2 ImageSizeForZoom(float zoom)
    {
        // size so that at zoom=1 the image fills the viewport without black side gaps.
        Vector2 viewportSize = GetViewportSize();
        float viewportAspect = viewportSize.x / Mathf.Max(1f, viewportSize.y);

        float baseWidth, baseHeight;
        if (imageNativeAspect >= viewportAspect)
        {
            baseHeight = viewportSize.y;
            baseWidth = viewportSize.y * imageNativeAspect;
        }
        else
        {
            baseWidth = viewportSize.x;
            baseHeight = viewportSize.x / imageNativeAspect;
        }

        return new Vector2(baseWidth, baseHeight) * zoom;
    }

    private Vector2 GetViewportSize()
    {
        if (viewport == null)
            return new Vector2(1920f, 1080f);
        Rect rect = viewport.rect;
        if (rect.width <= 1f || rect.height <= 1f)
            return new Vector2(1920f, 1080f);
        return new Vector2(rect.width, rect.height);
    }

    private Vector2 AnchoredPositionForFocus(Vector2 focus, Vector2 imageSize)
    {
        // focus is normalized [0..1] on the image. Translate so that focused point lands at viewport center.
        float dx = (0.5f - focus.x) * imageSize.x;
        float dy = (0.5f - focus.y) * imageSize.y;
        return ClampAnchoredPosition(new Vector2(dx, dy), imageSize);
    }

    private Vector2 ClampAnchoredPosition(Vector2 position, Vector2 imageSize)
    {
        Vector2 viewportSize = GetViewportSize();
        float maxX = Mathf.Max(0f, (imageSize.x - viewportSize.x) * 0.5f);
        float maxY = Mathf.Max(0f, (imageSize.y - viewportSize.y) * 0.5f);

        return new Vector2(
            Mathf.Clamp(position.x, -maxX, maxX),
            Mathf.Clamp(position.y, -maxY, maxY)
        );
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private IEnumerator FadeLoreToBlack(float duration)
    {
        if (backgroundImage != null)
            backgroundImage.color = Color.black;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        Color loreStart = loreImage != null ? loreImage.color : Color.white;
        Color subtitleStart = subtitleText != null ? subtitleText.color : subtitleColor;
        Color skipStart = skipPromptText != null ? skipPromptText.color : subtitleColor;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(t / duration);
            SetAlpha(loreImage, loreStart.a * alpha);
            SetAlpha(subtitleText, subtitleStart.a * alpha);
            SetAlpha(skipPromptText, skipStart.a * alpha);
            yield return null;
        }

        SetAlpha(loreImage, 0f);
        SetAlpha(subtitleText, 0f);
        SetAlpha(skipPromptText, 0f);
    }

    private void ResetVisualAlpha()
    {
        if (backgroundImage != null)
            backgroundImage.color = Color.black;
        SetAlpha(loreImage, 1f);
        SetAlpha(subtitleText, subtitleColor.a);
        SetAlpha(skipPromptText, 0.55f);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private void FinishSequence()
    {
        isPlaying = false;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (isLoadingScene)
            return;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            isLoadingScene = true;
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        isPlaying = false;
        gameObject.SetActive(false);
    }

    private static LorePanel[] DefaultPanels()
    {
        return new LorePanel[]
        {
            new LorePanel
            {
                title = "The Ruined Skyline",
                subtitle = "The city fell quiet under smoke and broken towers.",
                focusPoint = new Vector2(0.232f, 0.747f),
                zoom = 2.05f,
                holdDuration = 2.6f,
            },
            new LorePanel
            {
                title = "CityJan's Decree",
                subtitle = "One signature turned fear into law.",
                focusPoint = new Vector2(0.568f, 0.788f),
                zoom = 2.45f,
                holdDuration = 2.4f,
            },
            new LorePanel
            {
                title = "Deployment of the Horde",
                subtitle = "Machines marched where citizens used to stand.",
                focusPoint = new Vector2(0.842f, 0.770f),
                zoom = 2.35f,
                holdDuration = 2.6f,
            },
            new LorePanel
            {
                title = "The Burnt Wilds",
                subtitle = "Outside the walls, the world burned down to ash.",
                focusPoint = new Vector2(0.161f, 0.316f),
                zoom = 2.6f,
                holdDuration = 2.6f,
            },
            new LorePanel
            {
                title = "Resource Scarcity",
                subtitle = "Food, fuel, and trust became harder to find.",
                focusPoint = new Vector2(0.414f, 0.335f),
                zoom = 3.0f,
                holdDuration = 2.4f,
            },
            new LorePanel
            {
                title = "Propaganda Broadcast",
                subtitle = "Every broadcast watched, warned, and rewrote the truth.",
                focusPoint = new Vector2(0.610f, 0.335f),
                zoom = 3.0f,
                holdDuration = 2.4f,
            },
            new LorePanel
            {
                title = "A Spark of Resistance",
                subtitle = "Luca saw a way back through the dark.",
                focusPoint = new Vector2(0.858f, 0.316f),
                zoom = 2.55f,
                holdDuration = 3.0f,
            },
        };
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        if (subtitleFont != null)
            textComponent.font = subtitleFont;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = subtitleColor;
        textComponent.raycastTarget = false;
        textComponent.enableWordWrapping = true;
        return textComponent;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
