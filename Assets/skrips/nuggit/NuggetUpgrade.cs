
using UnityEngine;
using UnityEngine.UI;

public class NuggetUpgrade : MonoBehaviour
{
    [Header("Preis")]
    public float price = 1f;

    [Header("Nugget-Bonus")]
    public float nuggetIncrease = 1f;

    [Header("Preis steigt")]
    public float priceMultiplier = 1.5f;

    [Header("Button")]
    public Button buyButton;

    [Header("Taste")]
    public KeyCode buyKey = KeyCode.Alpha1;

    [Header("Sound")]
    public AudioClip purchaseSound;

    [Range(0f, 1f)]
    public float purchaseVolume = 1f;

    private int level = 0;

    private void Start()
    {
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
            Debug.LogWarning("ScoreManager fehlt!");
            return;
        }

        if (ScoreManager.Instance.GetNuggets() < price)
        {
            Debug.Log("Nicht genug Nuggets!");
            return;
        }

        // Bezahlen
        ScoreManager.Instance.RemoveNuggets(price);

        // Bonus erhöhen
        ScoreManager.Instance.AddNuggetBonus(
            nuggetIncrease
        );

        level++;

        // Preis erhöhen
        price *= priceMultiplier;

        // Sound
        if (purchaseSound != null)
        {
            AudioSource.PlayClipAtPoint(
                purchaseSound,
                transform.position,
                purchaseVolume
            );
        }

        // Sofort speichern
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.SaveGame();

        Debug.Log(
            "NUGGET UPGRADE GEKAUFT | " +
            "Level: " + level +
            " | Bonus: +" +
            ScoreManager.Instance.GetNuggetBonus() +
            " | Nächster Preis: " +
            price
        );
    }

    // =========================================================
    // SAVE SYSTEM
    // =========================================================

    public int GetLevel()
    {
        return level;
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(0, newLevel);
    }
}


