using UnityEngine;

public class DinoNugget : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Optional: wird nur noch für die Bewegung (Verfolgen) genutzt. " +
             "Falls leer, sucht der Dino automatisch den lokalen Spieler.")]
    public GameObject player;

    [Header("Facing")]
    public GameObject facingPoint;

    [Header("Save Zones")]
    public GameObject[] saveZones;

    [Header("Bewegung")]
    public float speed = 3f;
    public float rotationSpeed = 8f;
    public float stopDistance = 2f;

    [Header("TP wenn Dino den Player berührt")]
    public Vector3 respawnPosition =
        new Vector3(2.03f, 2.87f, -14.55f);

    private Collider[] saveColliders;

    private void Start()
    {
        if (saveZones != null && saveZones.Length > 0)
        {
            saveColliders = new Collider[saveZones.Length];

            for (int i = 0; i < saveZones.Length; i++)
            {
                if (saveZones[i] == null)
                    continue;

                Collider col = saveZones[i].GetComponent<Collider>();

                if (col == null)
                {
                    Debug.LogWarning(
                        "DinoNugget: Save Zone '" + saveZones[i].name +
                        "' braucht einen Collider!"
                    );
                }

                saveColliders[i] = col;
            }
        }
    }

    private void Update()
    {
        // Im Multiplayer gibt es mehrere Spieler -> immer den
        // lokal gesteuerten (Owner) Spieler verfolgen, falls
        // keine feste Referenz im Inspector gesetzt wurde.
        if (player == null && movement_player.Local != null)
            player = movement_player.Local.gameObject;

        if (player == null)
            return;

        if (facingPoint == null)
            return;

        // Player in einer Save Zone?
        if (IsPlayerInSaveZone())
            return;

        Vector3 direction =
            player.transform.position -
            transform.position;

        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= stopDistance)
            return;

        direction.Normalize();

        // Facing Point bestimmt die Vorderseite
        Vector3 facingForward =
            facingPoint.transform.forward;

        facingForward.y = 0f;

        if (facingForward.sqrMagnitude > 0.001f)
        {
            facingForward.Normalize();

            Quaternion targetRotation =
                Quaternion.FromToRotation(
                    facingForward,
                    direction
                ) *
                transform.rotation;

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }

        // Dino bewegt sich zum Player
        transform.position +=
            direction *
            speed *
            Time.deltaTime;
    }

    private bool IsPlayerInSaveZone()
    {
        if (saveColliders == null || saveColliders.Length == 0)
            return false;

        for (int i = 0; i < saveColliders.Length; i++)
        {
            if (saveColliders[i] == null)
                continue;

            if (saveColliders[i].bounds.Contains(player.transform.position))
                return true;
        }

        return false;
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
            "[Dino] OnTriggerEnter ausgelöst von: " +
            other.name +
            " | Tag=" + other.tag,
            other
        );

        // Dino trifft Player -> per Tag ODER Komponente erkennen,
        // damit es im Multiplayer mit jedem Spieler funktioniert,
        // nicht nur mit der einen manuell im Inspector gesetzten
        // Referenz, und auch wenn der Tag nur am Root-Objekt sitzt.
        if (!IsPlayer(other))
        {
            Debug.Log("[Dino] Ignoriert: kein Player (" + other.name + ")");
            return;
        }

        GameObject touchedPlayer = other.gameObject;

        touchedPlayer.transform.position =
            respawnPosition;

        Debug.Log(
            "Dino hat den Player erwischt! TP zu " +
            respawnPosition,
            touchedPlayer
        );
    }
}