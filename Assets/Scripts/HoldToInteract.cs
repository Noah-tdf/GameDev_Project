using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Events;

public class HoldToInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionTime = 2f;
    public string promptText = "E to Interact";
    public GameObject completionEffectPrefab;
    public Transform effectSpawnPoint;
    public bool oneTimeOnly = true;
    public bool isLocked = false;
    public UnityEvent OnComplete;

    [Header("UI References")]
    public GameObject uiPanel;
    public Image progressBar;
    public TextMeshProUGUI promptLabel;

    [Header("Audio")]
    public AudioClip appearanceSound;
    public AudioClip completionSound;

    [Header("Detection")]
    public float interactionRange = 2f;
    public LayerMask playerLayer;

    private bool isPlayerInRange = false;
    private bool isInteracting = false;
    private bool isComplete = false;
    private float currentProgress = 0f;
    private Transform playerTransform;
    private Animator playerAnimator;
    private PlayerShooting playerShooting;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        uiPanel.SetActive(false);
        promptLabel.text = promptText;
        progressBar.fillAmount = 0f;
    }

    private void Update()
    {
        if (isLocked)
        {
            uiPanel.SetActive(false);
            return;
        }

        if (isComplete && oneTimeOnly)
        {
            uiPanel.SetActive(false);
            return;
        }

        CheckRange();

        if (isPlayerInRange)
        {
            if (!uiPanel.activeSelf && appearanceSound != null)
            {
                AudioSource.PlayClipAtPoint(appearanceSound, transform.position);
            }
            uiPanel.SetActive(true);
            HandleInteraction();
        }
        else
        {
            uiPanel.SetActive(false);
            ResetInteraction();
        }
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    private void CheckRange()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactionRange, playerLayer);
        if (hit != null)
        {
            if (!isPlayerInRange)
            {
                Debug.Log($"Player detected at {name}");
                GameObject player = hit.gameObject;
                playerAnimator = player.GetComponent<Animator>();
                if (playerAnimator == null) playerAnimator = player.GetComponentInParent<Animator>();
                
                playerShooting = player.GetComponent<PlayerShooting>();
                if (playerShooting == null) playerShooting = player.GetComponentInParent<PlayerShooting>();
                
                playerMovement = player.GetComponent<PlayerMovement>();
                if (playerMovement == null) playerMovement = player.GetComponentInParent<PlayerMovement>();
            }
            isPlayerInRange = true;
            playerTransform = hit.transform;
        }
        else
        {
            if (isPlayerInRange)
            {
                Debug.Log($"Player left range of {name}");
                SetPlayerInteractingAnimation(false);
                playerAnimator = null;
                playerShooting = null;
                playerMovement = null;
            }
            isPlayerInRange = false;
        }
    }

            private void HandleInteraction()
            {
            bool isKeyPressed = false;
            if (Keyboard.current != null)
            {
            isKeyPressed = Keyboard.current.eKey.isPressed;
            }

            if (isKeyPressed)
            {
            if (!isInteracting)
            {
                isInteracting = true;
                SetPlayerInteractingAnimation(true);
            }
            currentProgress += Time.deltaTime;
            progressBar.fillAmount = currentProgress / interactionTime;

            if (currentProgress >= interactionTime)
            {
                OnInteractionComplete();
            }
            }
            else
            {
            ResetInteraction();
            }
            }

            private void ResetInteraction()
            {
            if (isInteracting)
            {
            isInteracting = false;
            SetPlayerInteractingAnimation(false);
            }
            currentProgress = 0f;
            progressBar.fillAmount = 0f;
            }

            private void SetPlayerInteractingAnimation(bool state)
            {
            if (playerAnimator != null)
            {
            try
            {
                playerAnimator.SetBool("IsInteracting", state);
            }
            catch { /* Parameter might not exist on all animators */ }
            }
        
            if (playerShooting != null)
            {
            playerShooting.SetWeaponsHiddenForInteraction(state);
            }

            if (playerMovement != null)
            {
            playerMovement.SetMovementLocked(state);
            }
            }

    private void OnInteractionComplete()
    {
        Debug.Log("Interaction Complete!");
        isComplete = true;

        if (completionSound != null) AudioSource.PlayClipAtPoint(completionSound, transform.position);
        
        OnComplete?.Invoke();

        if (completionEffectPrefab != null)
        {
            Vector3 spawnPos = effectSpawnPoint != null ? effectSpawnPoint.position : transform.position;
            Instantiate(completionEffectPrefab, spawnPos, Quaternion.identity);
        }
        
        ResetInteraction();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
