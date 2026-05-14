using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Animated pause menu styled after the GameMaker obj_pause asset:
/// text-only buttons that tween in scale / colour / horizontal offset,
/// with the selected entry growing and shifting right while others fade.
/// Toggled with Esc. Also displayed (as Game Over) when the player dies.
/// Spawns itself in gameplay scenes via <see cref="EnsureExists"/>; no scene setup required.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float buttonSpacing = 170f;      // vertical pixels between buttons (selected scales 2.5x, so leave breathing room)
    [SerializeField] private float lerpAmount    = 0.2f;      // GameMaker uses 0.2 per frame @ 60fps
    [SerializeField] private float maxAlpha      = 0.95f;     // dim background target
    [SerializeField] private Font  sciFiFont;                 // pixel/blocky font (auto-loaded from Assets/Fonts in editor)

    [Header("Audio")]
    [SerializeField] private AudioClip switchSound;
    [SerializeField] private AudioClip selectSound;

    private AudioSource audioSource;
    private static TMP_FontAsset cachedFontAsset;

    enum Action { Resume, Restart, MainMenu, Exit }
    struct Btn
    {
        public string label;
        public Action action;
        public RectTransform rect;
        public TextMeshProUGUI text;
        public float curScale, curAlpha, curXOffset;
    }

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image dimImage;
    private RectTransform panel;
    private TextMeshProUGUI titleText;
    private Btn[] buttons;
    private int selected;
    private float screenAlpha;
    private float screenAlphaTarget;
    private bool isPaused;
    private bool isGameOver;
    private bool isResuming;     // brief tween-out before unpausing
    private float storedTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null) return;
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "MainMenu" || scene == "Store") return;
        Debug.Log($"[PauseMenu] Spawning controller for scene '{scene}'. Press Esc to test.");
        new GameObject("PauseMenuController").AddComponent<PauseMenuController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.ignoreListenerPause = true;

    #if UNITY_EDITOR
        if (switchSound == null) switchSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Button/MECHSwtch_BLEEOOP_Lazer_Click.ogg");
        if (selectSound == null) selectSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Button/UIClick_BLEEOOP_Digi_Select.ogg");
    #endif

        BuildUI();
        ApplyVisibility(0f);
        Debug.Log("[PauseMenu] Awake: controller built. Press Esc to toggle. Keyboard.current=" + (Keyboard.current != null ? "OK" : "NULL"));
    }

    public static void SpawnIfMissing()
    {
        if (Instance != null) return;
        new GameObject("PauseMenuController").AddComponent<PauseMenuController>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse    ms = Mouse.current;

        // Toggle pause (disabled when Game Over is showing — only Restart/Main Menu allowed there)
        if (!isGameOver && kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("[PauseMenu] Esc detected — toggling pause.");
            TogglePause();
        }

        // Tween screen alpha toward target (uses unscaled time since timeScale = 0 when paused)
        screenAlpha = Approach(screenAlpha, screenAlphaTarget, lerpAmount * Time.unscaledDeltaTime * 60f);
        ApplyVisibility(screenAlpha);

        if (isResuming && screenAlpha <= 0.01f)
        {
            isResuming = false;
            isPaused = false;
            Time.timeScale = storedTimeScale;
        }

        if (!isPaused && !isGameOver) return;

        // Navigation
        bool down = (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame))
                 || (ms != null && ms.scroll.ReadValue().y < -0.01f);
        bool up   = (kb != null && (kb.upArrowKey.wasPressedThisFrame   || kb.wKey.wasPressedThisFrame))
                 || (ms != null && ms.scroll.ReadValue().y >  0.01f);
        bool confirm = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                    || (ms != null && ms.leftButton.wasPressedThisFrame);

        if (down)
        {
            int prev = selected;
            selected = Mathf.Min(selected + 1, buttons.Length - 1);
            if (selected != prev && switchSound != null) audioSource.PlayOneShot(switchSound);
        }
        if (up)
        {
            int prev = selected;
            selected = Mathf.Max(selected - 1, 0);
            if (selected != prev && switchSound != null) audioSource.PlayOneShot(switchSound);
        }

        // Mouse Hover Detection
        if (ms != null && canvas != null)
        {
            Vector2 mousePos = ms.position.ReadValue();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (isGameOver && buttons[i].action == Action.Resume) continue;
                
                if (RectTransformUtility.RectangleContainsScreenPoint(buttons[i].rect, mousePos, canvas.worldCamera))
                {
                    if (selected != i)
                    {
                        selected = i;
                        if (switchSound != null) audioSource.PlayOneShot(switchSound);
                    }
                    break;
                }
            }
        }

        // Tween each button toward its target state
        for (int i = 0; i < buttons.Length; i++)
        {
            // Skip the hidden Resume button when in Game Over
            if (isGameOver && buttons[i].action == Action.Resume)
            {
                buttons[i].text.gameObject.SetActive(false);
                continue;
            }
            buttons[i].text.gameObject.SetActive(true);

            int diff = Mathf.Abs(selected - i);
            float targetScale  = (i == selected) ? 2.5f : Mathf.Max(0.2f, 2.0f - 0.2f * diff);
            float targetAlpha  = (i == selected) ? 1.0f : Mathf.Max(0.2f, 1.0f - 0.2f * diff);
            float targetXOff   = (i == selected) ? 15f  : 0f;
            Color targetColor  = (i == selected) ? Color.white : new Color(0.55f, 0.55f, 0.55f);

            buttons[i].curScale   = Mathf.Lerp(buttons[i].curScale,   targetScale, lerpAmount);
            buttons[i].curAlpha   = Mathf.Lerp(buttons[i].curAlpha,   targetAlpha, lerpAmount);
            buttons[i].curXOffset = Mathf.Lerp(buttons[i].curXOffset, targetXOff,  lerpAmount);

            buttons[i].rect.localScale = Vector3.one * buttons[i].curScale;
            float targetY = -buttonSpacing * (i - (buttons.Length - 1) / 2f) - 80f; // shift stack down so the title sits clearly above
            Vector2 anchored = buttons[i].rect.anchoredPosition;
            anchored.x = buttons[i].curXOffset;
            anchored.y = Mathf.Lerp(anchored.y, targetY, lerpAmount);
            buttons[i].rect.anchoredPosition = anchored;

            Color c = targetColor;
            c.a = buttons[i].curAlpha * screenAlpha;
            buttons[i].text.color = c;
        }

        if (confirm) Confirm();
    }

    private void Confirm()
    {
        if (isResuming) return;
        if (selectSound != null) audioSource.PlayOneShot(selectSound);
        
        Action action = buttons[selected].action;
        if (isGameOver && action == Action.Resume) return;

        switch (action)
        {
            case Action.Resume:   BeginResume();         break;
            case Action.Restart:  Restart();             break;
            case Action.MainMenu: QuitToMainMenu();      break;
            case Action.Exit:     QuitApplication();     break;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void TogglePause() { if (isPaused) BeginResume(); else Pause(); }

    public void Pause()
    {
        if (isPaused || isGameOver) return;
        isPaused = true;
        storedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        titleText.text = "PAUSED";
        screenAlphaTarget = maxAlpha;
        selected = 0;
        ResetButtonTransforms();
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        isPaused = true;
        Time.timeScale = 0f;
        titleText.text = "GAME OVER";
        screenAlphaTarget = maxAlpha;
        selected = 1; // Restart is the natural default after death
        ResetButtonTransforms();
    }

    private void BeginResume()
    {
        if (!isPaused || isGameOver) return;
        isResuming = true;
        screenAlphaTarget = 0f;
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Application.Quit();
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResetButtonTransforms()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].curScale   = 1f;
            buttons[i].curAlpha   = 1f;
            buttons[i].curXOffset = 0f;
            buttons[i].rect.anchoredPosition = Vector2.zero;
            buttons[i].rect.localScale = Vector3.one;
        }
    }

    private void ApplyVisibility(float a)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = a > 0.01f ? 1f : 0f;
            canvasGroup.blocksRaycasts = a > 0.01f;
            canvasGroup.interactable   = a > 0.01f;
        }
        if (dimImage != null)
        {
            Color c = dimImage.color;
            c.a = a;
            dimImage.color = c;
        }
        if (titleText != null)
        {
            Color c = titleText.color;
            c.a = a;
            titleText.color = c;
        }
    }

    private static float Approach(float current, float target, float step)
    {
        if (current < target) return Mathf.Min(current + Mathf.Abs(step), target);
        if (current > target) return Mathf.Max(current - Mathf.Abs(step), target);
        return target;
    }

    private TMP_FontAsset ResolveFont()
    {
        if (cachedFontAsset != null) return cachedFontAsset;

#if UNITY_EDITOR
        if (sciFiFont == null)
        {
            // Prefer Astron-Bold (the sci-fi font the user dropped in), then fall back.
            string[] candidates = {
                "Assets/Fonts/Astron-Bold.otf",
                "Assets/Fonts/Astron.otf",
                "Assets/Fonts/PressStart2P-Regular.ttf",
                "Assets/Fonts/Pixeled.ttf",
                "Assets/Fonts/BlackOpsOne-Regular.ttf",
            };
            foreach (string path in candidates)
            {
                sciFiFont = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (sciFiFont != null) break;
            }
        }
#endif
        if (sciFiFont == null) return null;
        cachedFontAsset = TMP_FontAsset.CreateFontAsset(sciFiFont);
        if (cachedFontAsset != null) cachedFontAsset.name = sciFiFont.name + " SDF (Runtime)";
        return cachedFontAsset;
    }

    // ── UI construction ───────────────────────────────────────────────────────
    private void BuildUI()
    {
        TMP_FontAsset font = ResolveFont();
        GameObject canvasGO = new GameObject("PauseMenuCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasGO.GetComponent<CanvasGroup>();

        dimImage = CreateImage("Dim", canvasGO.transform, new Color(0f, 0f, 0f, 0f));
        Stretch(dimImage.rectTransform);

        RectTransform centerRoot = CreateRect("Center", canvasGO.transform);
        centerRoot.anchorMin = centerRoot.anchorMax = new Vector2(0.5f, 0.5f);
        centerRoot.pivot = new Vector2(0.5f, 0.5f);
        centerRoot.anchoredPosition = Vector2.zero;
        centerRoot.sizeDelta = new Vector2(900f, 1100f);

        titleText = CreateText("Title", centerRoot, "PAUSED", 96f, TextAlignmentOptions.Center, Color.white);
        titleText.fontStyle = FontStyles.Bold;
        if (font != null) titleText.font = font;
        titleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        titleText.rectTransform.sizeDelta = new Vector2(800f, 140f);

        Action[] actions = { Action.Resume, Action.Restart, Action.MainMenu, Action.Exit };
        string[] labels  = { "RESUME",     "RESTART",     "MAINMENU",     "EXIT" };
        buttons = new Btn[actions.Length];

        for (int i = 0; i < actions.Length; i++)
        {
            Btn b = new Btn { label = labels[i], action = actions[i], curScale = 1f, curAlpha = 1f };
            GameObject go = new GameObject(labels[i] + "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(centerRoot, false);
            b.rect = go.GetComponent<RectTransform>();
            b.rect.anchorMin = b.rect.anchorMax = new Vector2(0.5f, 0.5f);
            b.rect.pivot = new Vector2(0.5f, 0.5f);
            b.rect.anchoredPosition = Vector2.zero;
            b.rect.sizeDelta = new Vector2(420f, 60f);

            b.text = go.GetComponent<TextMeshProUGUI>();
            b.text.text = labels[i];
            b.text.fontSize = 36f;
            b.text.alignment = TextAlignmentOptions.Center;
            b.text.color = Color.white;
            b.text.fontStyle = FontStyles.Bold;
            b.text.raycastTarget = false;
            b.text.enableWordWrapping = false;
            if (font != null) b.text.font = font;

            buttons[i] = b;
        }
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return img;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
