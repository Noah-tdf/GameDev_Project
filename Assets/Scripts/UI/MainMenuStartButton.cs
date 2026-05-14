using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RectTransform))]
public class MainMenuStartButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "FinalLevel";
    [SerializeField] private LoreController loreController;
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

        if (loreController == null)
            loreController = FindFirstObjectByType<LoreController>();
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition, uiCamera))
        {
            if (loreController != null)
                loreController.PlayAndLoadScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }
    }
}
