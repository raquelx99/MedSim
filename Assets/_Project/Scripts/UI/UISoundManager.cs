using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class UISoundManager : MonoBehaviour
{
    public UIInputModule inputModule;

    [Header("Sons")]
    public AudioClip hoverClip;
    public AudioClip clickClip;
    [Range(0f, 1f)] public float hoverVolume = 0.4f;
    [Range(0f, 1f)] public float clickVolume = 0.8f;
    public float minHoverInterval = 0.05f;

    private AudioSource source;
    private float lastHoverTime = float.NegativeInfinity;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        if (inputModule == null)
            inputModule = FindObjectOfType<UIInputModule>();
    }

    void OnEnable()
    {
        if (inputModule == null)
        {
            Debug.LogWarning("UISoundManager: nenhum XR UI Input Module encontrado, os botões ficarão sem som.", this);
            return;
        }

        inputModule.pointerEnter += OnPointerEnter;
        inputModule.pointerClick += OnPointerClick;
    }

    void OnDisable()
    {
        if (inputModule == null) return;

        inputModule.pointerEnter -= OnPointerEnter;
        inputModule.pointerClick -= OnPointerClick;
    }

    private void OnPointerEnter(GameObject target, PointerEventData eventData)
    {
        if (!target.TryGetComponent(out Selectable selectable) || !CanInteract(selectable))
            return;

        if (Time.unscaledTime - lastHoverTime < minHoverInterval)
            return;

        lastHoverTime = Time.unscaledTime;
        Play(hoverClip, hoverVolume);
    }

    private void OnPointerClick(GameObject target, PointerEventData eventData)
    {
        Selectable selectable = target.GetComponentInParent<Selectable>();
        if (selectable != null && CanInteract(selectable))
            Play(clickClip, clickVolume);
    }

    private static bool CanInteract(Selectable selectable)
    {
        return selectable.isActiveAndEnabled && selectable.IsInteractable();
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip != null)
            source.PlayOneShot(clip, volume);
    }
}
