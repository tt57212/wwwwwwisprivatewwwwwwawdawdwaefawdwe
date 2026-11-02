using UnityEngine;

public class ToggleUI : MonoBehaviour
{
    // Das UI Panel, das ein- oder ausgeblendet werden soll
    public GameObject uiPanel;

    // Diese Methode wird beim Klick auf den Button ausgelöst
    public void ToggleUIVisibility()
    {
        if (uiPanel != null)
        {
            // Ein-/Ausblenden umschalten
            uiPanel.SetActive(!uiPanel.activeSelf);
        }
    }
}