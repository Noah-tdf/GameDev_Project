using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuQuitButton : MonoBehaviour
{
    [SerializeField] private Camera uiCamera;

    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectSound;

    private RectTransform rectTransform;
    private AudioSource audioSource;
    private bool isHovering = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

    #if UNITY_EDITOR
        if (hoverSound == null) hoverSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Button/MECHSwtch_BLEEOOP_Lazer_Click.ogg");
        if (selectSound == null) selectSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Button/UIClick_BLEEOOP_Digi_Select.ogg");
    #endif

        if (uiCamera == null)
        {
            uiCamera = Camera.main;
        }
    }

    private void Update()
    {
        Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        bool currentlyInside = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition, uiCamera);

        if (currentlyInside && !isHovering)
        {
            isHovering = true;
            if (hoverSound != null) audioSource.PlayOneShot(hoverSound);
        }
        else if (!currentlyInside && isHovering)
        {
            isHovering = false;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (currentlyInside)
        {
            if (selectSound != null) audioSource.PlayOneShot(selectSound);
            QuitGame();
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
