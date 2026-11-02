using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class conter_ui_nuggit : MonoBehaviour
{
    [Header("Nugget")]
    public float nuggetAmount = 1f;

    [Header("Score")]
    public float scoreAmount = 1f;

    [Header("Collect Sound")]
    public AudioClip collectSound;

    [Range(0f, 1f)]
    public float collectVolume = 1f;

    private bool collected = false;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            "[Nugget] OnTriggerEnter ausgelöst von: " +
            other.name +
            " | Tag=" + other.tag,
            other
        );

        if (collected)
            return;

        // Erkennung robust machen: funktioniert auch, wenn der Tag
        // "Player" nur am Root-Objekt gesetzt ist, der Collider aber
        // an einem Kind-Objekt liegt (Tags werden NICHT vererbt).
        bool isPlayer =
            other.CompareTag("Player") ||
            other.GetComponentInParent<movement_player>() != null;

        if (!isPlayer)
        {
            Debug.Log(
                "[Nugget] Ignoriert: kein Player (" + other.name + ")"
            );
            return;
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning(
                "ScoreManager fehlt!"
            );
            return;
        }

        collected = true;

        // =========================================
        // BONUS HOLEN
        // =========================================

        float bonus =
            ScoreManager.Instance.GetNuggetBonus();

        // =========================================
        // ENDGÜLTIGER NUGGET-WERT
        // =========================================

        float finalAmount =
            nuggetAmount + bonus;

        // Nuggets geben
        ScoreManager.Instance.AddNuggets(
            finalAmount
        );

        // Score geben
        ScoreManager.Instance.AddPoints(
            scoreAmount
        );

        // =========================================
        // SOUND
        // =========================================

        if (collectSound != null && audioSource != null)
        {
            // WICHTIG: PlayOneShot statt PlayClipAtPoint, damit der
            // Sound über die AudioSource-Komponente (und damit über
            // den im Output-Feld zugewiesenen Audio Mixer) läuft.
            // PlayClipAtPoint würde eine eigene, nicht gemixte
            // AudioSource erzeugen.
            audioSource.PlayOneShot(collectSound, collectVolume);
        }

        Debug.Log(
            "NUGGET GESAMMELT! " +
            "Basis: +" + nuggetAmount +
            " | Bonus: +" + bonus +
            " | Gesamt: +" + finalAmount
        );

        // Objekt erst zerstören, nachdem der Sound fertig abgespielt
        // wurde, da "transform" sonst mitten im Sound verschwindet.
        float delay = collectSound != null ? collectSound.length : 0f;
        Destroy(gameObject, delay);
    }
}