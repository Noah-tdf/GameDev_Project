using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuQuitButton : MonoBehaviour
{
    [SerializeField] private Camera uiCamera;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (uiCamera == null)
        {
            uiCamera = Camera.main;
        }
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
