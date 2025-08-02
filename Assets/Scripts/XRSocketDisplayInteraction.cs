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
        display.ShowImage(displayImage);
    }
}
