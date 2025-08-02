using UnityEngine;
using UnityEngine.UI;

public class TVDisplay : MonoBehaviour
{
    public Image imageSlot;
    public GameObject panel; 

    public void ShowImage(Sprite image)
    {
        panel.SetActive(true);
        imageSlot.sprite = image;
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
