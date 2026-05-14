using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private GameObject loadingAnimationRoot;
    [SerializeField] private Image transitionImage;
    [SerializeField] private TextMeshProUGUI skipPromptText;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private Sprite defaultTransitionSprite;
    [SerializeField] private Color defaultFadeColor = Color.black;

    private bool isTransitioning = false;
    private bool skipRequested = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
            transitionCanvasGroup.blocksRaycasts = false;
        }

        if (loadingAnimationRoot != null)
        {
            loadingAnimationRoot.SetActive(false);
        }

        if (skipPromptText != null)
        {
            skipPromptText.gameObject.SetActive(false);
        }
    }

    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionStandard(sceneName, true));
    }

    public void TransitionToSceneImmediate(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionStandard(sceneName, false));
    }

    public void TransitionWithPortal(string sceneName, Sprite portalImage)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionPortal(sceneName, portalImage));
    }

    private IEnumerator TransitionStandard(string sceneName, bool fadeOut)
    {
        isTransitioning = true;

        // Reset UI to standard loading state
        SetTransitionVisuals(null, defaultFadeColor);
        if (skipPromptText != null) skipPromptText.gameObject.SetActive(false);

        if (fadeOut)
        {
            // Fade Out
            yield return StartCoroutine(Fade(1f));
        }
        else
        {
            // Already black, just ensure alpha is 1
            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = 1f;
                transitionCanvasGroup.blocksRaycasts = true;
            }
        }

        // Removed Loading Animation

        yield return new WaitForSeconds(0.5f);

        // Load Scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // Fade In
        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    private IEnumerator TransitionPortal(string sceneName, Sprite portalImage)
    {
        isTransitioning = true;
        skipRequested = false;

        // Set the special portal background
        SetTransitionVisuals(portalImage, Color.white);
        
        // Fade Out (to show the portal image)
        yield return StartCoroutine(Fade(1f));

        // Show Skip Prompt
        if (skipPromptText != null)
        {
            skipPromptText.gameObject.SetActive(true);
            skipPromptText.text = "Press any key to skip";
        }

        // Wait for input
        while (!skipRequested)
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame ||
                Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                skipRequested = true;
            }
            yield return null;
        }

        if (skipPromptText != null) skipPromptText.gameObject.SetActive(false);

        // Removed Loading Animation

        // Load Scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // Fade In
        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    private void SetTransitionVisuals(Sprite sprite, Color color)
    {
        if (transitionImage != null)
        {
            transitionImage.sprite = sprite != null ? sprite : defaultTransitionSprite;
            transitionImage.color = color;
            // If sprite is null, ensure it's just a solid color (e.g. black)
            if (sprite == null && defaultTransitionSprite == null)
            {
                transitionImage.sprite = null;
            }
        }
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (transitionCanvasGroup == null) yield break;

        float startAlpha = transitionCanvasGroup.alpha;
        float elapsed = 0f;

        transitionCanvasGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        transitionCanvasGroup.alpha = targetAlpha;
        if (targetAlpha <= 0f)
        {
            transitionCanvasGroup.blocksRaycasts = false;
        }
    }
}
