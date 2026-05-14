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

    [Header("Detection")]
    public float interactionRange = 2f;
    public LayerMask playerLayer;

    private bool isPlayerInRange = false;
    private bool isInteracting = false;
    private bool isComplete = false;
    private float currentProgress = 0f;
    private Transform playerTransform;

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
            if (!isPlayerInRange) Debug.Log($"Player detected at {name}");
            isPlayerInRange = true;
            playerTransform = hit.transform;
        }
        else
        {
            if (isPlayerInRange) Debug.Log($"Player left range of {name}");
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
            isInteracting = true;
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
        isInteracting = false;
        currentProgress = 0f;
        progressBar.fillAmount = 0f;
    }

    private void OnInteractionComplete()
    {
        Debug.Log("Interaction Complete!");
        isComplete = true;
        
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
