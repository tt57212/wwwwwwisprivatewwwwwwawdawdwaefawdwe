using UnityEngine;

public class Teleport : MonoBehaviour
{
    public GameObject gameobjekt;

    private GameObject player;

    void Start()
    {
        FindePlayer();
    }

    void FindePlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("Kein GameObject mit Tag 'Player' gefunden!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        TeleportierePlayer(other.gameObject);
    }

    private bool IsPlayer(Collider other)
    {
        return
            other.CompareTag("Player") ||
            other.GetComponentInParent<movement_player>() != null;
    }

    // Diese Methode aufrufen, um zu teleportieren
    // (z.B. per Button, Trigger oder Taste)
    public void TeleportierePlayer()
    {
        if (player == null)
        {
            FindePlayer();

            if (player == null)
                return;
        }

        TeleportierePlayer(player);
    }

    private void TeleportierePlayer(GameObject target)
    {
        if (gameobjekt == null)
        {
            Debug.LogWarning("Kein Ziel-GameObjekt zugewiesen!");
            return;
        }

        target.transform.position = gameobjekt.transform.position;

        Debug.Log("Player teleportiert zu: " + gameobjekt.transform.position);
    }
}