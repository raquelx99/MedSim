using UnityEngine;

public class InstrumentInteraction : MonoBehaviour
{
    public string instrumentTag;
    public TVDisplay tvDisplay;
    public Sprite displayImage;
    public AudioManager audioManager;
    public AudioClip interactionSound;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(instrumentTag)) return;

        ExamItem examItem = other.GetComponent<ExamItem>();
        if (examItem == null) return;

        InstrumentEvents.RaiseInstrumentUsed(examItem.objectID);

        if (tvDisplay != null && displayImage != null)
            tvDisplay.ShowImage(displayImage);

        if (audioManager != null && interactionSound != null)
            audioManager.Play(interactionSound);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(instrumentTag)) return;

        if (tvDisplay != null)
            tvDisplay.Hide();

        if (audioManager != null && interactionSound != null)
            audioManager.Stop(interactionSound);
    }
}
