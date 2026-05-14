using UnityEngine;

public class PortalController : MonoBehaviour
{
    [SerializeField] private GameObject portalVisuals;
    [SerializeField] private Animator animator;
    [SerializeField] private bool startsActive = false;
    [SerializeField] private AudioClip activationSound;

    public bool IsActivated { get; private set; }

    private void Awake()
    {
        IsActivated = startsActive;
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
        IsActivated = true;
        
        if (activationSound != null) AudioSource.PlayClipAtPoint(activationSound, transform.position);

        if (animator != null)
        {
            animator.SetBool("Activated", true);
            Debug.Log("Portal Activated!");
        }
    }
}

