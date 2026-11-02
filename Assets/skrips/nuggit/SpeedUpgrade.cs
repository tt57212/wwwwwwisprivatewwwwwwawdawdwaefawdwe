
using UnityEngine;
using UnityEngine.UI;

public class SpeedUpgrade : MonoBehaviour
{
    [Header("Player")]
    public movement_player player;

    [Header("Speed")]
    public float speedIncrease = 1f;

    [Header("Preis")]
    public float price = 1f;
    public float priceMultiplier = 1.5f;

    [Header("Button")]
    public Button buyButton;

    [Header("Taste")]
    public KeyCode buyKey = KeyCode.Alpha2;

    [Header("Sound")]
    public AudioClip purchaseSound;

    [Range(0f, 1f)]
    public float purchaseVolume = 1f;

    [Header("Start Speed")]
    public float defaultSpeed = 5f;

    private int level = 0;

    private const string LevelKey = "SpeedUpgrade_Level";
    private const string PriceKey = "SpeedUpgrade_Price";
    private const string SpeedKey = "SpeedUpgrade_Speed";

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<movement_player>();
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning(
                "SpeedUpgrade: Player Movement fehlt!"
            );
            return;
        }

        LoadSpeed();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuyUpgrade);
            buyButton.onClick.AddListener(BuyUpgrade);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(buyKey))
            BuyUpgrade();
    }

    public void BuyUpgrade()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning(
                "SpeedUpgrade: ScoreManager fehlt!"
            );
            return;
        }

        if (player == null)
        {
            player = FindFirstObjectByType<movement_player>();
        }

        if (player == null)
        {
            Debug.LogWarning(
                "SpeedUpgrade: Player Movement fehlt!"
            );
            return;
        }

        if (ScoreManager.Instance.GetNuggets() < price)
        {
            Debug.Log(
                "Nicht genug Nuggets!"
            );
            return;
        }

        // Nuggets bezahlen
        ScoreManager.Instance.RemoveNuggets(price);

        // Speed erhöhen
        player.moveSpeed += speedIncrease;

        // Level erhöhen
        level++;

        // Preis erhöhen
        price *= priceMultiplier;

        // SOFORT SPEICHERN
        SaveSpeed();

        // Sound
        if (purchaseSound != null)
        {
            AudioSource.PlayClipAtPoint(
                purchaseSound,
                transform.position,
                purchaseVolume
            );
        }

        Debug.Log(
            "SPEED GEKAUFT | " +
            "Level: " + level +
            " | Speed: " + player.moveSpeed +
            " | Preis: " + price
        );
    }

    // =====================================================
    // SPEICHERN
    // =====================================================

    private void SaveSpeed()
    {
        PlayerPrefs.SetInt(
            LevelKey,
            level
        );

        PlayerPrefs.SetFloat(
            PriceKey,
            price
        );

        PlayerPrefs.SetFloat(
            SpeedKey,
            player.moveSpeed
        );

        PlayerPrefs.Save();

        Debug.Log(
            "SPEED GESPEICHERT: " +
            player.moveSpeed
        );
    }

    // =====================================================
    // LADEN
    // =====================================================

    private void LoadSpeed()
    {
        level = PlayerPrefs.GetInt(
            LevelKey,
            0
        );

        price = PlayerPrefs.GetFloat(
            PriceKey,
            price
        );

        if (PlayerPrefs.HasKey(SpeedKey))
        {
            player.moveSpeed =
                PlayerPrefs.GetFloat(
                    SpeedKey
                );
        }
        else
        {
            player.moveSpeed =
                defaultSpeed;
        }

        Debug.Log(
            "SPEED GELADEN | " +
            "Level: " + level +
            " | Speed: " + player.moveSpeed +
            " | Preis: " + price
        );
    }

    // =====================================================
    // SAVE SYSTEM
    // =====================================================

    public int GetLevel()
    {
        return level;
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(
            0,
            newLevel
        );
    }

    // =====================================================
    // RESET
    // =====================================================

    public void ResetSpeed()
    {
        level = 0;
        price = 1f;

        if (player != null)
            player.moveSpeed = defaultSpeed;

        PlayerPrefs.DeleteKey(LevelKey);
        PlayerPrefs.DeleteKey(PriceKey);
        PlayerPrefs.DeleteKey(SpeedKey);

        PlayerPrefs.Save();

        Debug.Log(
            "SPEED UPGRADE ZURÜCKGESETZT!"
        );
    }
}

