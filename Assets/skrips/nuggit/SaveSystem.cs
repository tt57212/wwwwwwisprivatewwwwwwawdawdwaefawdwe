using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    [Header("Automatisches Speichern")]
    public bool autoSave = true;
    public float saveInterval = 5f;

    [Header("Shop Reset")]
    public bool shopIsOpen = false;

    private float saveTimer;

    // =========================================================
    // SAVE KEYS
    // =========================================================

    private const string NuggetsKey = "Save_Nuggets";
    private const string ScoreKey = "Save_Score";
    private const string BonusKey = "Save_NuggetBonus";

    private const string NuggetLevelKey = "NuggetUpgrade_Level";
    private const string NuggetPriceKey = "NuggetUpgrade_Price";

    private const string SpeedLevelKey = "SpeedUpgrade_Level";
    private const string SpeedPriceKey = "SpeedUpgrade_Price";
    private const string SpeedValueKey = "SpeedUpgrade_Speed";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Nach jedem Szenenwechsel erneut laden, da ScoreManager/
        // SpeedUpgrade/NuggetUpgrade nur in der Gameplay-Szene existieren
        // und beim ersten Start() von SaveSystem evtl. noch nicht da waren.
        LoadGame();
    }

    private void Start()
    {
        LoadGame();
    }

    private void Update()
    {
        // =====================================================
        // Ü = SPIELSTAND RESETTEN
        // =====================================================

        if (shopIsOpen && Input.GetKeyDown(KeyCode.U))
        {
            ResetGame();
        }

        // =====================================================
        // AUTOMATISCH SPEICHERN
        // =====================================================

        if (!autoSave)
            return;

        saveTimer += Time.deltaTime;

        if (saveTimer >= saveInterval)
        {
            saveTimer = 0f;
            SaveGame();
        }
    }

    // =========================================================
    // SHOP
    // =========================================================

    public void SetShopOpen(bool open)
    {
        shopIsOpen = open;

        Debug.Log(
            "Shop ist jetzt: " +
            (open ? "OFFEN" : "GESCHLOSSEN")
        );
    }

    // =========================================================
    // SPEICHERN
    // =========================================================

    public void SaveGame()
    {
        if (ScoreManager.Instance == null)
            return;

        PlayerPrefs.SetFloat(
            NuggetsKey,
            ScoreManager.Instance.GetNuggets()
        );

        PlayerPrefs.SetFloat(
            ScoreKey,
            ScoreManager.Instance.score
        );

        PlayerPrefs.SetFloat(
            BonusKey,
            ScoreManager.Instance.GetNuggetBonus()
        );

        NuggetUpgrade nuggetUpgrade =
            FindFirstObjectByType<NuggetUpgrade>();

        if (nuggetUpgrade != null)
        {
            PlayerPrefs.SetInt(
                NuggetLevelKey,
                nuggetUpgrade.GetLevel()
            );

            PlayerPrefs.SetFloat(
                NuggetPriceKey,
                nuggetUpgrade.price
            );
        }

        SpeedUpgrade speedUpgrade =
            FindFirstObjectByType<SpeedUpgrade>();

        if (speedUpgrade != null)
        {
            PlayerPrefs.SetInt(
                SpeedLevelKey,
                speedUpgrade.GetLevel()
            );

            PlayerPrefs.SetFloat(
                SpeedPriceKey,
                speedUpgrade.price
            );

            // Fallback: falls player nicht im Inspector gesetzt wurde
            // oder noch nicht gefunden wurde, den lokalen Spieler nehmen.
            if (speedUpgrade.player == null)
                speedUpgrade.player = movement_player.Local;

            if (speedUpgrade.player != null)
            {
                PlayerPrefs.SetFloat(
                    SpeedValueKey,
                    speedUpgrade.player.moveSpeed
                );
            }
            else
            {
                Debug.LogWarning("SaveSystem: Kein Player gefunden, Speed wird NICHT gespeichert!");
            }
        }

        PlayerPrefs.Save();

        Debug.Log(
            "AUTOMATISCH GESPEICHERT | " +
            "Nuggets: " +
            ScoreManager.Instance.GetNuggets() +
            " | Bonus: +" +
            ScoreManager.Instance.GetNuggetBonus()
        );
    }

    // =========================================================
    // LADEN
    // =========================================================

    public void LoadGame()
    {
        if (ScoreManager.Instance == null)
            return;

        ScoreManager.Instance.nuggets =
            PlayerPrefs.GetFloat(
                NuggetsKey,
                0f
            );

        ScoreManager.Instance.score =
            PlayerPrefs.GetFloat(
                ScoreKey,
                0f
            );

        ScoreManager.Instance.SetNuggetBonus(
            PlayerPrefs.GetFloat(
                BonusKey,
                0f
            )
        );

        NuggetUpgrade nuggetUpgrade =
            FindFirstObjectByType<NuggetUpgrade>();

        if (nuggetUpgrade != null)
        {
            nuggetUpgrade.SetLevel(
                PlayerPrefs.GetInt(
                    NuggetLevelKey,
                    0
                )
            );

            nuggetUpgrade.price =
                PlayerPrefs.GetFloat(
                    NuggetPriceKey,
                    nuggetUpgrade.price
                );
        }

        SpeedUpgrade speedUpgrade =
            FindFirstObjectByType<SpeedUpgrade>();

        if (speedUpgrade != null)
        {
            speedUpgrade.SetLevel(
                PlayerPrefs.GetInt(
                    SpeedLevelKey,
                    0
                )
            );

            speedUpgrade.price =
                PlayerPrefs.GetFloat(
                    SpeedPriceKey,
                    speedUpgrade.price
                );

            // Fallback: falls player nicht im Inspector gesetzt wurde
            // oder noch nicht gefunden wurde, den lokalen Spieler nehmen.
            if (speedUpgrade.player == null)
                speedUpgrade.player = movement_player.Local;

            if (speedUpgrade.player != null)
            {
                speedUpgrade.player.moveSpeed =
                    PlayerPrefs.GetFloat(
                        SpeedValueKey,
                        speedUpgrade.player.moveSpeed
                    );
            }
            else
            {
                Debug.LogWarning("SaveSystem: Kein Player gefunden, Speed wird NICHT geladen!");
            }
        }

        Debug.Log(
            "SPIELSTAND GELADEN | Nuggets: " +
            ScoreManager.Instance.GetNuggets() +
            " | Bonus: +" +
            ScoreManager.Instance.GetNuggetBonus()
        );
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetGame()
    {
        Debug.Log(
            "!!! SPIELSTAND WIRD ZURÜCKGESETZT !!!"
        );

        // Währung
        PlayerPrefs.DeleteKey(NuggetsKey);
        PlayerPrefs.DeleteKey(ScoreKey);
        PlayerPrefs.DeleteKey(BonusKey);

        // Nugget Upgrade
        PlayerPrefs.DeleteKey(NuggetLevelKey);
        PlayerPrefs.DeleteKey(NuggetPriceKey);

        // Speed Upgrade
        PlayerPrefs.DeleteKey(SpeedLevelKey);
        PlayerPrefs.DeleteKey(SpeedPriceKey);
        PlayerPrefs.DeleteKey(SpeedValueKey);

        PlayerPrefs.Save();

        // Aktuelle Werte zurücksetzen
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.nuggets = 0f;
            ScoreManager.Instance.score = 0f;
            ScoreManager.Instance.SetNuggetBonus(0f);
        }

        // Nugget Upgrade zurücksetzen
        NuggetUpgrade nuggetUpgrade =
            FindFirstObjectByType<NuggetUpgrade>();

        if (nuggetUpgrade != null)
        {
            nuggetUpgrade.SetLevel(0);
            nuggetUpgrade.price = 1f;
        }

        // Speed Upgrade zurücksetzen
        SpeedUpgrade speedUpgrade =
            FindFirstObjectByType<SpeedUpgrade>();

        if (speedUpgrade != null)
        {
            speedUpgrade.SetLevel(0);
            speedUpgrade.price = 1f;

            if (speedUpgrade.player != null)
            {
                speedUpgrade.player.moveSpeed = 5f;
            }
        }

        Debug.Log(
            "!!! SPIELSTAND KOMPLETT ZURÜCKGESETZT !!!"
        );
    }

    // =========================================================
    // BEENDEN / PAUSE
    // =========================================================

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveGame();
    }
}