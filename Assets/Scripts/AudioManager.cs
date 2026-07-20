using UnityEngine;
using UnityEngine.Audio;
using System.Collections;


public sealed class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;

    [SerializeField] private AudioSource ambienceSource;

    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private AudioSource loopSFXSource;


    [Header("Menu Music")]
    [SerializeField] private AudioClip mainMenuMusic;

    [SerializeField, Min(0f)]
    private float musicFadeDuration = 1f;

    [Header("Events Channels")]
    [SerializeField] private VoidEventChannelSO mainMenuEntered;

    [SerializeField] private VoidEventChannelSO experienceTransitionStarted;


    private Coroutine musicFadeCoroutine;
    private float defaultMusicVolume;


    private void Awake()
    {
        if (musicSource == null)
        {
            Debug.LogError("music source no esta asignado", 
                this);

            return;
        }

        defaultMusicVolume = musicSource.volume;
    }

    private void HandleExperienceTransitionStarted()
    {
        Debug.Log(
        "AudioManager recibio ExperienceTransitionStarted ",
        this);
        FadeOutMenuMusic();
    }

    // suscribe
    private void OnEnable()
    {
        if (mainMenuEntered != null)
        {
            mainMenuEntered.Raised += HandleMainMenuEntered;
        }

        if (experienceTransitionStarted != null)
        {
            experienceTransitionStarted.Raised +=
                HandleExperienceTransitionStarted;

        }
    }


    // unsuscribe
    private void OnDisable()
    {
        if (mainMenuEntered != null)
        {
            mainMenuEntered.Raised -= HandleMainMenuEntered;
        }

        if (experienceTransitionStarted != null)
        {
            experienceTransitionStarted.Raised -=
                HandleExperienceTransitionStarted;

        }

    }

    private void HandleMainMenuEntered()
    {
        Debug.Log("AudioManager recibio MainMenuEntered ", this);
        PlayMenuMusic();

    }


    
    private void PlayMenuMusic()
    {
        if (mainMenuMusic == null)
        {
            Debug.LogWarning(
                "La musica del MainMenu no esta asignada",
                this);

            return;
        }

        if (musicSource.clip == mainMenuMusic &&
            musicSource.isPlaying)
        {
            return;
        }

        StopCurrentFade();

        musicSource.Stop();
        musicSource.clip = mainMenuMusic;
        musicSource.loop = true;
        musicSource.volume = 0;
        musicSource.Play();

        musicFadeCoroutine = StartCoroutine(
                FadeMusicRoutine(
                    defaultMusicVolume,
                    musicFadeDuration,
                    false
                    )
                    );

    }

    private void FadeOutMenuMusic()
    {
        if (!musicSource.isPlaying)
        {
            return ;
        }

        StopCurrentFade();

        musicFadeCoroutine = StartCoroutine(
                FadeMusicRoutine(
                    0f,
                    musicFadeDuration,
                    true
                    )
                    );


    }

    private IEnumerator FadeMusicRoutine(float targetVolume, float duration, bool stopAfterFade)
    {
        float initialVolume = musicSource.volume;

        if (duration <= 0f)
        {
            musicSource.volume = targetVolume;
        }
        else
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(elapsedTime / duration);

                musicSource.volume = Mathf.Lerp(
                    initialVolume,
                    targetVolume,
                    progress);

                yield return null;
            }

            musicSource.volume = targetVolume;
        }

        if (stopAfterFade)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = defaultMusicVolume;
        }

        musicFadeCoroutine = null;


    }

    private void StopCurrentFade()
    {
        if (musicFadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = null;
    }


    public void PlaySFX( AudioClip clip, float volume = 1f)
    {

        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            clip,
            Mathf.Clamp01(volume));
    }


    public void PlayAmbience(AudioClip clip)
    {
        if (clip == null || ambienceSource == null)
        {
            return;
        }

        if (ambienceSource.clip == clip &&
            ambienceSource.isPlaying)
        {
            return;
        }

        ambienceSource.Stop();
        ambienceSource.clip = clip;
        ambienceSource.loop = true;
        ambienceSource.Play();

    }


    public void StopAmbience()
    {
        if (ambienceSource != null)
        {
            ambienceSource.Stop();
        }
    }

    public void PlayLoopSFX(AudioClip clip)
    {
        if (clip == null || loopSFXSource == null)
        {
            return;
        }

        if (loopSFXSource.clip == clip &&
            loopSFXSource.isPlaying)
        {
            return;
        }

        loopSFXSource.Stop();
        loopSFXSource.clip = clip;
        loopSFXSource.loop = true;
        loopSFXSource.Play();
    }

    public void StopLoopSFX()
    {
        if (loopSFXSource == null)
        {
            return;
        }

        loopSFXSource.Stop();
        loopSFXSource.clip = null;
    }

}
