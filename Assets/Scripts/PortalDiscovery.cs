using UnityEngine;
using TMPro;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PortalDiscovery : MonoBehaviour
{
    [Header("Settings")]
    public string discoveryDialogue = "I need to figure out how to turn on the portal.";
    public float displayDuration = 10f;
    public float detectionRange = 4f;
    public LayerMask playerLayer;
    public HoldToInteract antennaToUnlock;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    private bool hasDiscovered = false;
    private bool isPlayerInRange = false;
    private Coroutine hideTimerCoroutine;
    private static TMP_FontAsset cachedFontAsset;

    private void Awake()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null)
        {
            dialogueText.text = discoveryDialogue;
            ApplyStyle(dialogueText);
        }
    }

    private void Update()
    {
        CheckRange();

        if (isPlayerInRange && !hasDiscovered)
        {
            Discover();
        }
        // Note: intentionally NOT hiding when the player leaves range.
        // The dialogue stays for `displayDuration` seconds no matter where the player goes.
    }

    private void CheckRange()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        isPlayerInRange = (hit != null);
    }

    private void Discover()
    {
        hasDiscovered = true;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueText != null) ApplyStyle(dialogueText);

        if (antennaToUnlock != null)
        {
            antennaToUnlock.SetLocked(false);
            Debug.Log("Antenna unlocked by portal discovery!");
        }

        if (hideTimerCoroutine != null) StopCoroutine(hideTimerCoroutine);
        hideTimerCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        hideTimerCoroutine = null;
    }

    private static void ApplyStyle(TextMeshProUGUI label)
    {
        TMP_FontAsset font = ResolveFont();
        if (font != null) label.font = font;

        int len = label.text != null ? label.text.Length : 0;
        if      (len > 70) label.fontSize = 24f;
        else if (len > 45) label.fontSize = 28f;
        else if (len > 25) label.fontSize = 34f;
        else               label.fontSize = 40f;

        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;

        RectTransform rt = label.rectTransform;
        Vector2 size = rt.sizeDelta;
        if (size.x < 900f) size.x = 900f;
        size.y = Mathf.Max(size.y, label.fontSize * 2.4f);
        rt.sizeDelta = size;
    }

    private static TMP_FontAsset ResolveFont()
    {
        if (cachedFontAsset != null) return cachedFontAsset;
#if UNITY_EDITOR
        string[] candidates = {
            "Assets/Fonts/Astron-Bold.otf",
            "Assets/Fonts/Astron.otf",
            "Assets/Fonts/PressStart2P-Regular.ttf",
            "Assets/Fonts/Pixeled.ttf",
            "Assets/Fonts/BlackOpsOne-Regular.ttf",
        };
        Font source = null;
        foreach (string path in candidates)
        {
            source = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (source != null) break;
        }
        if (source != null)
        {
            cachedFontAsset = TMP_FontAsset.CreateFontAsset(source);
            if (cachedFontAsset != null) cachedFontAsset.name = source.name + " SDF (Runtime)";
        }
#endif
        return cachedFontAsset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
