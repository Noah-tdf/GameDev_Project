using UnityEngine;

public class PortalTeleport : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "FinalLevel";
    [SerializeField] private bool requiresActivation = true;
    
    private PortalController portalController;

    private void Awake()
    {
        portalController = GetComponentInParent<PortalController>();
    }

    [SerializeField] private Sprite portalTransitionImage;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player touched portal.");
            if (requiresActivation && (portalController == null || !portalController.IsActivated))
            {
                Debug.Log("Portal not activated yet.");
                return;
            }

            Debug.Log($"Teleporting to {targetSceneName} via Portal.");
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionWithPortal(targetSceneName, portalTransitionImage);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
            }
        }
    }
}
