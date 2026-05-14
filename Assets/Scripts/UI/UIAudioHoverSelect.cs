using UnityEngine;
using UnityEngine.EventSystems;

public class UIAudioHoverSelect : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectSound;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound != null && audioSource != null)
            audioSource.PlayOneShot(hoverSound);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selectSound != null && audioSource != null)
            audioSource.PlayOneShot(selectSound);
    }

    public void SetSounds(AudioClip hover, AudioClip select)
    {
        hoverSound = hover;
        selectSound = select;
    }
}