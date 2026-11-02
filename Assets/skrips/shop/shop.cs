using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.ParticleSystem;

public class shop : MonoBehaviour
{
    public UnityEngine.UI.RawImage image;
    public TMPro.TMP_Text text;
    public UnityEngine.UI.Button button;

    private bool isinrange = false;
    private bool uiOpen = false;

    private void Start()
    {
        if (text != null)
            text.gameObject.SetActive(false);

        if (button != null)
            button.gameObject.SetActive(false);

        if (image != null)
            image.gameObject.SetActive(false);
    }

    private bool IsPlayer(Collider other)
    {
        return
            other.CompareTag("Player") ||
            other.GetComponentInParent<movement_player>() != null;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            "[Shop] OnTriggerEnter ausgelöst von: " +
            other.name +
            " | Tag=" + other.tag,
            other
        );

        if (!IsPlayer(other))
        {
            Debug.Log("[Shop] Ignoriert: kein Player (" + other.name + ")");
            return;
        }

        isinrange = true;

        if (text != null)
            text.gameObject.SetActive(true);

        if (button != null)
            button.gameObject.SetActive(true);

        Debug.Log("SPIELER IST IM SHOP!");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        isinrange = false;

        CloseUI();

        if (text != null)
            text.gameObject.SetActive(false);

        if (button != null)
            button.gameObject.SetActive(false);

        Debug.Log("SPIELER HAT SHOP VERLASSEN!");
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // E öffnet den Shop
        if (isinrange &&
            !uiOpen &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenUI();
        }

        // ESC schließt den Shop
        if (uiOpen &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseUI();
        }
    }

    public void OpenUI()
    {
        if (!isinrange)
            return;

        uiOpen = true;

        if (image != null)
            image.gameObject.SetActive(true);

        if (text != null)
            text.gameObject.SetActive(false);

        if (button != null)
            button.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("SHOP GEÖFFNET!");
    }

    public void CloseUI()
    {
        uiOpen = false;

        if (image != null)
            image.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (isinrange)
        {
            if (text != null)
                text.gameObject.SetActive(true);

            if (button != null)
                button.gameObject.SetActive(true);
        }

        Debug.Log("SHOP GESCHLOSSEN!");
    }
}