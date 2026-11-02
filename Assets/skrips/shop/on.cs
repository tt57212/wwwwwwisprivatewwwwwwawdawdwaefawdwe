using UnityEngine;

public class on : MonoBehaviour
{
    public GameObject rawImage;

    public void ShowRawImage()
    {
        rawImage.SetActive(true);
    }

    public void HideRawImage()
    {
        rawImage.SetActive(false);
    }
}