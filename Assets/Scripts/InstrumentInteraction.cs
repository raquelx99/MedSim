using UnityEngine;

public class InstrumentInteraction : MonoBehaviour
{
    public string instrumentTag;

    public TVDisplay tvDisplay;

    public Sprite displayImage;
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
                tvDisplay.ShowImage(displayImage);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(instrumentTag))
        {
            tvDisplay.Hide();
        }
    }

}
