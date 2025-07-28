using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ExamItem : MonoBehaviour
{
    public string objectID;

    void OnEnable()
    {
        var interactable = GetComponent<XRGrabInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnGrabbed);
        }
    }

    void OnDisable()
    {
        var interactable = GetComponent<XRGrabInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnGrabbed);
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log($"Objeto médico '{objectID}' foi usado.");

        PhysicalExamManager examManager = FindObjectOfType<PhysicalExamManager>();
        if (examManager != null)
        {
            examManager.RegisterExamItem(objectID);
        }
    }
}
