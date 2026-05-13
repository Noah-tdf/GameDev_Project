using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RectTransform))]
public class MainMenuStoreButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "Store";
    [SerializeField] private Camera uiCamera;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (uiCamera == null)
            uiCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition, uiCamera))
            SceneManager.LoadScene(sceneName);
    }
}
