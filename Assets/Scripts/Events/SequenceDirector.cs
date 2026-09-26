using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class SequenceDirector : MonoBehaviour
{

    [Header("Timeline")]
    [SerializeField] private PlayableDirector playableDirector;

    [Tooltip("Opcional si solo hay una pista de audio. Con varias pistas, asigna ExperienceMusicSource.")]
    [SerializeField] private AudioSource musicSource;

    [Header("Event Channel")]
    [SerializeField] private VoidEventChannelSO experienceReady;
    [SerializeField] private VoidEventChannelSO experienceCompleted;


    private bool sequenceStarted;
    private bool menuReturnRequested;
    private double musicEndTime;
    private AppStateMachine appStateMachine;


    private void OnEnable()
    {
        Debug.Log(
            $"SequenceDirector habilitado en la escena " +
            $"'{gameObject.scene.name}'.",
            this);

        if (experienceReady == null)
        {
            Debug.LogError(
                "ExperienceReady no esta asignado en SequenceDirector",
                this);

            return;
        }

        experienceReady.Raised += HandleExperienceReady;

        Debug.Log(
            $"SequenceDirector suscrito a '{experienceReady.name}' ",
            this);
    }

    private void OnDisable()
    {
        sequenceStarted = false;

        if (experienceReady != null)
        {
            experienceReady.Raised -= HandleExperienceReady;

        }
    }

    private void HandleExperienceReady()
    {
        if (sequenceStarted)
        {
            return;
        }

        if (playableDirector == null)
        {
            Debug.LogError(
                "PlayableDirector no esta asignado",
                this);

            return;
        }

        appStateMachine = FindFirstObjectByType<AppStateMachine>();
        if (appStateMachine == null)
        {
            Debug.LogWarning(
                "[SequenceDirector] Inicia desde Bootstrap para poder volver al MainMenu al terminar.",
                this);
        }

        musicEndTime = ResolveMusicEndTime();
        if (double.IsNaN(musicEndTime) || double.IsInfinity(musicEndTime) || musicEndTime <= 0)
        {
            Debug.LogError("[SequenceDirector] El Timeline necesita una duracion finita mayor que cero.", this);
            return;
        }

        menuReturnRequested = false;
        sequenceStarted = true;

        // Hold conserva el tiempo final;
        // None lo reinicia y Loop nunca se detiene
        // La experiencia reproduce la cancion una sola vez, y luego vuelve al menu
        playableDirector.extrapolationMode = DirectorWrapMode.Hold;
        playableDirector.time = 0;
        playableDirector.Evaluate();
        playableDirector.Play();

        Debug.Log(
            "La secuencia musical comenzo",
            this);
    }

    private void LateUpdate()
    {
        if (!sequenceStarted || menuReturnRequested || playableDirector == null ||
            appStateMachine == null || Time.timeScale <= 0f || AudioListener.pause)
        {
            return;
        }

        // No usar stopped: tambien ocurre al cancelar o descargar la experiencia
        // No Playing: Hold puede haber alcanzado ya el ultimo frame
        if (playableDirector.time + 0.000001 < musicEndTime)
        {
            return;
        }

        // Reintentar en el siguiente frame si otra transicion aun esta terminando.
        menuReturnRequested = true;
        playableDirector.Pause();

        if (experienceCompleted != null)
        {
            experienceCompleted.RaiseEvent();
        }

        Debug.Log(
            "[SequenceDirector] Cancion terminada. ExperienceCompleted publicado.",
            this
        );
    }


    private double ResolveMusicEndTime()
    {
        double timelineEnd = playableDirector.duration;
        if (playableDirector.playableAsset is not TimelineAsset timeline)
        {
            return timelineEnd;
        }

        AudioTrack selectedTrack = null;
        double audioEnd = 0;
        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            if (track is not AudioTrack audioTrack || audioTrack.mutedInHierarchy ||
                (musicSource != null && playableDirector.GetGenericBinding(track) != musicSource))
            {
                continue;
            }

            double trackEnd = 0;
            foreach (TimelineClip clip in audioTrack.GetClips())
            {
                if (clip.asset is AudioPlayableAsset audio && audio.clip != null)
                {
                    trackEnd = Math.Max(trackEnd, clip.end);
                }
            }

            if (trackEnd <= 0)
            {
                continue;
            }

            if (musicSource == null && selectedTrack != null)
            {
                Debug.LogWarning(
                    "[SequenceDirector] Hay varias pistas de audio. Asigna Music Source para " +
                    "identificar la cancion; por ahora se usara el final del Timeline.",
                    this);
                return timelineEnd;
            }

            selectedTrack = audioTrack;
            audioEnd = Math.Max(audioEnd, trackEnd);
        }

        // Los clips tienen su posicion, recorte y velocidad dentro del Timeline
        // El Timeline puede terminar antes si tiene una duracion fija mas corta
        return audioEnd > 0 ? Math.Min(audioEnd, timelineEnd) : timelineEnd;
    }

}
