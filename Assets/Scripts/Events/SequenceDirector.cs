using UnityEngine;
using UnityEngine.Playables;

public class SequenceDirector : MonoBehaviour
{

    [Header("Timeline")]
    [SerializeField] private PlayableDirector playableDirector;

    [Header("Event Channel")]
    [SerializeField] private VoidEventChannelSO experienceReady;


    private bool sequenceStarted;


    private void OnEnable()
    {
        Debug.Log(
            $"SequenceDirector habilitado en la escena " +
            $"'{gameObject.scene.name}'.",
            this);

        if (experienceReady == null)
        {
            Debug.LogError(
                "ExperienceReady no está asignado en SequenceDirector",
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

        sequenceStarted = true;

        playableDirector.time = 0;
        playableDirector.Evaluate();
        playableDirector.Play();

        Debug.Log(
            "La secuencia musical comenzo",
            this);
    }

}
