using UnityEngine;

public class Portal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.SpringeZurNacht();
        }
        else
        {
            Debug.LogWarning("Portal: Kein DayNightCycle in der Szene gefunden!");
        }
    }

    private bool IsPlayer(Collider other)
    {
        return
            other.CompareTag("Player") ||
            other.GetComponentInParent<movement_player>() != null;
    }
}