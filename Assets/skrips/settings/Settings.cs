using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Settings : MonoBehaviour
{
    public static Settings Instance;

    [Header("Panel (ESC öffnet/schließt)")]
    public GameObject settingsPanel;

    [Header("Lautstärke")]
    public AudioMixer audioMixer;
    public string volumeParameter = "MasterVolume";
    public Slider volumeSlider;

    [Header("Maus-Empfindlichkeit")]
    public Slider mouseSensitivitySlider;
    public float defaultMouseSensitivity = 0.15f;

    [Header("Touch-Empfindlichkeit")]
    public Slider touchSensitivitySlider;
    public float defaultTouchSensitivity = 0.15f;

    [Header("Steuerungsmodus")]
    public Dropdown controlModeDropdown;

    private bool settingsOpen = false;

    // =========================================================
    // SAVE KEYS
    // =========================================================

    private const string VolumeKey = "Settings_Volume";
    private const string MouseSensitivityKey = "Settings_MouseSensitivity";
    private const string TouchSensitivityKey = "Settings_TouchSensitivity";
    private const string ControlModeKey = "Settings_ControlMode";

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

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void Start()
    {
        LoadSettings();

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetVolume);

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);

        if (touchSensitivitySlider != null)
            touchSensitivitySlider.onValueChanged.AddListener(SetTouchSensitivity);

        if (controlModeDropdown != null)
            controlModeDropdown.onValueChanged.AddListener(SetControlMode);
    }

    private void Update()
    {
        if (settingsPanel == null)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleSettings();
        }
    }

    // =========================================================
    // ÖFFNEN / SCHLIESSEN (ESC)
    // =========================================================

    public void ToggleSettings()
    {
        if (!settingsOpen)
            OpenSettings();
        else
            CloseSettings();
    }

    public void OpenSettings()
    {
        if (settingsPanel == null)
            return;

        settingsOpen = true;
        settingsPanel.SetActive(true);

        if (movement_player.Local != null)
            movement_player.Local.SetUIOpen(true);
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel == null)
            return;

        settingsOpen = false;
        settingsPanel.SetActive(false);

        if (movement_player.Local != null)
            movement_player.Local.SetUIOpen(false);
    }

    // =========================================================
    // LAUTSTÄRKE
    // =========================================================

    public void SetVolume(float value)
    {
        if (audioMixer != null)
        {
            // Slider liefert 0..1 -> in Dezibel umrechnen
            float dB = value > 0.0001f
                ? Mathf.Log10(value) * 20f
                : -80f;

            audioMixer.SetFloat(volumeParameter, dB);
        }

        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    // =========================================================
    // MAUS-EMPFINDLICHKEIT
    // =========================================================

    public void SetMouseSensitivity(float value)
    {
        if (movement_player.Local != null)
            movement_player.Local.mouseSensitivity = value;

        PlayerPrefs.SetFloat(MouseSensitivityKey, value);
        PlayerPrefs.Save();
    }

    // =========================================================
    // TOUCH-EMPFINDLICHKEIT
    // =========================================================

    public void SetTouchSensitivity(float value)
    {
        if (movement_player.Local != null)
            movement_player.Local.touchSensitivity = value;

        PlayerPrefs.SetFloat(TouchSensitivityKey, value);
        PlayerPrefs.Save();
    }

    // =========================================================
    // STEUERUNGSMODUS
    // =========================================================

    public void SetControlMode(int index)
    {
        if (movement_player.Local != null)
            movement_player.Local.controlMode = (movement_player.ControlMode)index;

        PlayerPrefs.SetInt(ControlModeKey, index);
        PlayerPrefs.Save();
    }

    // =========================================================
    // LADEN
    // =========================================================

    public void LoadSettings()
    {
        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        float mouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, defaultMouseSensitivity);
        float touchSensitivity = PlayerPrefs.GetFloat(TouchSensitivityKey, defaultTouchSensitivity);
        int controlMode = PlayerPrefs.GetInt(ControlModeKey, (int)movement_player.ControlMode.Auto);

        SetVolume(volume);
        SetMouseSensitivity(mouseSensitivity);
        SetTouchSensitivity(touchSensitivity);
        SetControlMode(controlMode);

        if (volumeSlider != null)
            volumeSlider.value = volume;

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.value = mouseSensitivity;

        if (touchSensitivitySlider != null)
            touchSensitivitySlider.value = touchSensitivity;

        if (controlModeDropdown != null)
            controlModeDropdown.value = controlMode;
    }

    // =========================================================
    // ANWENDEN AUF NEU GESPAWNTEN SPIELER
    // =========================================================

    // Aufrufen, sobald ein neuer Spieler (z.B. nach Szenenwechsel
    // oder Netzwerk-Spawn) lokal initialisiert wurde, damit die
    // gespeicherten Werte übernommen werden.
    public void ApplyToLocalPlayer()
    {
        if (movement_player.Local == null)
            return;

        movement_player.Local.mouseSensitivity =
            PlayerPrefs.GetFloat(MouseSensitivityKey, defaultMouseSensitivity);

        movement_player.Local.touchSensitivity =
            PlayerPrefs.GetFloat(TouchSensitivityKey, defaultTouchSensitivity);

        movement_player.Local.controlMode =
            (movement_player.ControlMode)PlayerPrefs.GetInt(
                ControlModeKey,
                (int)movement_player.ControlMode.Auto
            );
    }
}