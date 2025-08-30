using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRSocketDisplayInteraction : MonoBehaviour
{
    public TVDisplay display;
    public Sprite displayImage;

    private XRSocketInteractor socket;

    private void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        socket.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        GameObject instrumentObject = args.interactableObject.transform.gameObject;
        ExamItem examItem = instrumentObject.GetComponent<ExamItem>();
        if (examItem != null)
        {
            string objectID = examItem.objectID;
            Debug.Log($"Instrumento '{objectID}' foi usado.");
            PhysicalExamManager examManager = FindObjectOfType<PhysicalExamManager>();
            if (examManager != null)
            {
                examManager.RegisterExamItem(objectID);
            }
        }
        display.ShowImage(displayImage);
    }
    
}

