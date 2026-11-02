using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Werte")]
    public float nuggets = 0f;
    public float score = 0f;

    [Header("Nugget Upgrade")]
    public float nuggetBonus = 0f;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text nuggit_text;

    [Header("UI Auto-Bind (Fallback, falls in der Szene nicht verknüpft)")]
    [Tooltip("Name des GameObjects mit der Score-Anzeige in der jeweiligen Szene.")]
    public string scoreTextObjectName = "ScoreText";
    [Tooltip("Name des GameObjects mit der Nugget-Anzeige in der jeweiligen Szene.")]
    public string nuggetTextObjectName = "NuggetText";

    private const string NuggetsKey = "Save_Nuggets";
    private const string ScoreKey = "Save_Score";
    private const string BonusKey = "Save_NuggetBonus";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Diese Szene hat ihre EIGENE ScoreManager-Instanz mit
            // (im Inspector) korrekt verknüpften UI-Referenzen für
            // genau diese Szene. Bevor das Duplikat zerstört wird,
            // übernehmen wir diese Referenzen in die persistente
            // Instanz - sonst gehen sie beim Destroy() verloren und
            // die Anzeige bleibt leer/kaputt.
            if (scoreText != null)
                Instance.scoreText = scoreText;

            if (nuggit_text != null)
                Instance.nuggit_text = nuggit_text;

            Instance.UpdateUIPublic();

            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Nur wenn das Objekt Root ist
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Fallback: Falls in der neuen Szene keine eigene
        // ScoreManager-Instanz mit verknüpften UI-Referenzen existiert,
        // versuchen wir per Namenssuche die passenden Objekte zu finden.
        RebindUIFallback();
        UpdateUI();
    }

    private void RebindUIFallback()
    {
        if (scoreText == null && !string.IsNullOrEmpty(scoreTextObjectName))
        {
            GameObject go = GameObject.Find(scoreTextObjectName);

            if (go != null)
                scoreText = go.GetComponent<TMP_Text>();
        }

        if (nuggit_text == null && !string.IsNullOrEmpty(nuggetTextObjectName))
        {
            GameObject go = GameObject.Find(nuggetTextObjectName);

            if (go != null)
                nuggit_text = go.GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        LoadData();
        UpdateUI();
    }

    // =====================================================
    // NUGGETS
    // =====================================================

    public void AddNuggets(float amount)
    {
        nuggets += amount;

        UpdateUI();
        SaveData();

        Debug.Log(
            "Nuggets: " + nuggets
        );
    }

    public void RemoveNuggets(float amount)
    {
        if (amount <= 0)
            return;

        if (nuggets < amount)
        {
            Debug.Log(
                "Nicht genug Nuggets!"
            );
            return;
        }

        nuggets -= amount;

        UpdateUI();
        SaveData();

        Debug.Log(
            "Nuggets übrig: " + nuggets
        );
    }

    public float GetNuggets()
    {
        return nuggets;
    }

    // =====================================================
    // SCORE
    // =====================================================

    public void AddPoints(float amount)
    {
        score += amount;

        UpdateUI();
        SaveData();
    }

    // =====================================================
    // BONUS
    // =====================================================

    public void AddNuggetBonus(float amount)
    {
        if (amount <= 0)
            return;

        nuggetBonus += amount;

        SaveData();

        Debug.Log(
            "BONUS ERHÖHT! Aktueller Bonus: +" +
            nuggetBonus
        );
    }

    public float GetNuggetBonus()
    {
        return nuggetBonus;
    }

    public void SetNuggetBonus(float amount)
    {
        nuggetBonus = Mathf.Max(0f, amount);

        SaveData();

        Debug.Log(
            "BONUS GESETZT: +" +
            nuggetBonus
        );
    }

    // =====================================================
    // SPEICHERN
    // =====================================================

    public void SaveData()
    {
        PlayerPrefs.SetFloat(
            NuggetsKey,
            nuggets
        );

        PlayerPrefs.SetFloat(
            ScoreKey,
            score
        );

        PlayerPrefs.SetFloat(
            BonusKey,
            nuggetBonus
        );

        PlayerPrefs.Save();

        Debug.Log(
            "SCOREMANAGER SAVE | " +
            "Nuggets: " + nuggets +
            " | Bonus: +" + nuggetBonus
        );
    }

    // =====================================================
    // LADEN
    // =====================================================

    public void LoadData()
    {
        nuggets = PlayerPrefs.GetFloat(
            NuggetsKey,
            0f
        );

        score = PlayerPrefs.GetFloat(
            ScoreKey,
            0f
        );

        nuggetBonus = PlayerPrefs.GetFloat(
            BonusKey,
            0f
        );

        Debug.Log(
            "SCOREMANAGER LOAD | " +
            "Nuggets: " + nuggets +
            " | Bonus: +" + nuggetBonus
        );
    }

    // =====================================================
    // UI
    // =====================================================

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "Score: " + score;
        }

        if (nuggit_text != null)
        {
            nuggit_text.text =
                "Nuggets: " + nuggets;
        }
    }

    public void UpdateUIPublic()
    {
        UpdateUI();
    }

    // =====================================================
    // RESET
    // =====================================================

    public void ResetValues()
    {
        nuggets = 0f;
        score = 0f;
        nuggetBonus = 0f;

        PlayerPrefs.DeleteKey(NuggetsKey);
        PlayerPrefs.DeleteKey(ScoreKey);
        PlayerPrefs.DeleteKey(BonusKey);

        PlayerPrefs.Save();

        UpdateUI();

        Debug.Log(
            "Nuggets, Score und Bonus zurückgesetzt!"
        );
    }
}                       