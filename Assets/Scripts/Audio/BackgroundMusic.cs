using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    [Header("Music Settings")]
    public bool playOnStart = true;
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public bool loop = true;

    [Header("Optional Clip")]
    public AudioClip musicClip;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        AudioClip clip = musicClip;

        if (clip == null)
            clip = Resources.Load<AudioClip>("background_music");

        if (clip == null)
        {
            Debug.LogWarning("BackgroundMusic: Could not load background_music AudioClip. Assign musicClip or place the mp3 in an Assets/Resources folder.");
            return;
        }

        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.volume = GetVolume();
        audioSource.playOnAwake = false;

        if (playOnStart)
            audioSource.Play();
    }

    public void PlayMusic()
    {
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();
    }

    public void StopMusic()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    public void SetVolume(float vol)
    {
        volume = Mathf.Clamp01(vol);
        if (audioSource != null)
            audioSource.volume = volume;
        if (Application.isPlaying)
            PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public float GetVolume()
    {
        if (volume < 0f)
            volume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        return volume;
    }

    public void TogglePause()
    {
        if (audioSource == null || audioSource.clip == null) return;
        if (audioSource.isPlaying)
            audioSource.Pause();
        else
            audioSource.Play();
    }
}