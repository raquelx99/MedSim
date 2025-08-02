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
            tvDisplay.ShowImage(displayImage);
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
