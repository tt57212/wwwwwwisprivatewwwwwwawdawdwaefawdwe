using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    public Material daySkybox;
    public Material nightSkybox;

    public Light sun;

    [Range(0, 24)]
    public float time = 12f;

    public float dayLength = 120f;

    [Header("Portal-Einstellung")]
    [Tooltip("Uhrzeit, auf die gesprungen wird, wenn der Spieler durchs Portal geht.")]
    [Range(0, 24)]
    public float nachtZeit = 20f;

    [Tooltip("Falls true, bleibt die Zeit nach dem Portal stehen (keine automatische Weiterbewegung).")]
    public bool zeitAnhaltenNachPortal = false;

    private bool zeitAngehalten = false;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!zeitAngehalten)
        {
            // Zeit weiterlaufen lassen
            time += (24f / dayLength) * Time.deltaTime;

            if (time >= 24f)
                time = 0f;
        }

        // Sonne drehen
        float sunAngle = (time / 24f) * 360f - 90f;
        sun.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);

        // Skybox wechseln
        if (time >= 6f && time < 18f)
        {
            RenderSettings.skybox = daySkybox;
        }
        else
        {
            RenderSettings.skybox = nightSkybox;
        }

        // Beleuchtung aktualisieren
        DynamicGI.UpdateEnvironment();
    }

    // Wird vom Portal aufgerufen
    public void SpringeZurNacht()
    {
        time = nachtZeit;
        zeitAngehalten = zeitAnhaltenNachPortal;

        Debug.Log("Es ist jetzt Nacht! Zeit = " + time);
    }

    // Falls man die Zeit manuell wieder freigeben will
    public void ZeitFortsetzen()
    {
        zeitAngehalten = false;
    }
}