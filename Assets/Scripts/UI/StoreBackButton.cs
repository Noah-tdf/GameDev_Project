using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RectTransform))]
public class StoreBackButton : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private Camera uiCamera;

    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Camera eventCamera = GetEventCamera();
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition, eventCamera))
            SceneManager.LoadScene(menuSceneName);
    }

    private Camera GetEventCamera()
    {
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (uiCamera != null)
            return uiCamera;

        if (parentCanvas != null && parentCanvas.worldCamera != null)
            return parentCanvas.worldCamera;

        return Camera.main;
    }
}
