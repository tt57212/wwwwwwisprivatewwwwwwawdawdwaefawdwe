
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [Header("Musik")]
    public AudioClip music;

    [Range(0f, 1f)]
    public float volume = 0.5f;

    public bool playOnStart = true;
    public bool loop = true;

    [Header("Optionaler Button")]
    public Button toggleButton;

    private AudioSource audioSource;
    private bool musicOn = true;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        // Musik bleibt beim Szenenwechsel
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleMusic);

        if (playOnStart && music != null)
            PlayMusic();
    }

    public void PlayMusic()
    {
        if (music == null)
        {
            Debug.LogWarning("Keine Musik ausgewählt!");
            return;
        }

        audioSource.clip = music;
        audioSource.volume = volume;
        audioSource.loop = loop;

        audioSource.Play();
        musicOn = true;
    }

    public void StopMusic()
    {
        audioSource.Stop();
        musicOn = false;
    }

    public void ToggleMusic()
    {
        if (musicOn)
            StopMusic();
        else
            PlayMusic();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }
}

