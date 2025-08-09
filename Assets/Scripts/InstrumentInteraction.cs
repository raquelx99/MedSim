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
        if (other.CompareTag(instrumentTag))
        {
            ExamItem examItem = other.GetComponent<ExamItem>();
            if (examItem != null)
            {
                string objectID = examItem.objectID;
                Debug.Log($"Instrumento '{objectID}' foi usado.");
                PhysicalExamManager examManager = FindObjectOfType<PhysicalExamManager>();
                if (examManager != null)
                {
                    examManager.RegisterExamItem(objectID);
                }
                if (tvDisplay != null && displayImage != null)
                {
                    tvDisplay.ShowImage(displayImage);
                }
                if (interactionSound != null && audioManager != null)
                {
                    Debug.Log("Áudio de interação tocado.");
                    audioManager.Play(interactionSound);
                }
                
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(instrumentTag))
        {
            if (tvDisplay != null)
            {
                tvDisplay.Hide();
            }
            if (audioManager != null && interactionSound != null)
            {
                audioManager.Stop(interactionSound);
            }
            
        }
    }

}
