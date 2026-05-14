using UnityEngine;

public class PortalController : MonoBehaviour
{
    [SerializeField] private GameObject portalVisuals;
    [SerializeField] private Animator animator;
    [SerializeField] private bool startsActive = false;

    private void Awake()
    {
        if (portalVisuals != null)
        {
            portalVisuals.SetActive(true); // Always active now, but animation handles state
        }
        
        if (animator != null)
        {
            animator.SetBool("Activated", startsActive);
        }
    }

    public void ActivatePortal()
    {
        if (animator != null)
        {
            animator.SetBool("Activated", true);
            Debug.Log("Portal Activated!");
        }
    }
}

