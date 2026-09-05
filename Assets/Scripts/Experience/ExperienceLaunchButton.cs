using UnityEngine;

public sealed class ExperienceLaunchButton : MonoBehaviour
{
    [Header("Experience")]

    [SerializeField]
    private ExperienceDefinitionSO experience;


    [Header("Start Scene")]

    [Tooltip(
        "Vacio para comenzar desde " +
        "la primera escena."
    )]
    [SerializeField]
    private ExperienceSceneDefinitionSO startScene;


    [Header("Play Mode")]

    [Tooltip(
        "Si esta activo continua con las " +
        "siguientes escenas. Si está desactivado " +
        "solo reproduce la escena seleccionada."
    )]
    [SerializeField] private bool playFullSequence = true;


    [Header("Scene Selection")]

    [SerializeField] private ExperienceSceneSelector experienceSceneSelector;


    [Header("Event")]

    [SerializeField]
    private ExperienceEventChannelSO experienceRequested;


    public void RequestExperience()
    {
        if (experience == null)
        {
            Debug.LogError(
                "No se asigno ExperienceDefinition",
                this
            );

            return;
        }


        if (experienceRequested == null)
        {
            Debug.LogError(
                "No se asigno ExperienceRequested",
                this
            );

            return;
        }


        ExperienceSceneDefinitionSO requestedStartScene =
            startScene;

        bool requestedFullSequence =
            playFullSequence;


        if (experienceSceneSelector != null)
        {
            requestedStartScene =
                experienceSceneSelector.SelectedScene;

            requestedFullSequence =
                experienceSceneSelector.PlayFullSequence;
        }


        int startIndex = 0;


        if (requestedStartScene != null)
        {
            startIndex =
                experience.GetSceneIndex(
                    requestedStartScene
                );


            if (startIndex < 0)
            {
                Debug.LogError(
                    $"La escena '{requestedStartScene.name}' " +
                    $"no pertenece a la experiencia " +
                    $"'{experience.name}'.",
                    this
                );

                return;
            }
        }


        Debug.Log(
            $"Request Experience - StartIndex: {startIndex}, " +
            $"Full: {requestedFullSequence}, " +
            $"Scene: {(requestedStartScene != null ? requestedStartScene.DisplayName : "None")}",
            this
        );


        ExperienceRequest request =
            new ExperienceRequest(
                experience,
                startIndex,
                requestedFullSequence
            );


        experienceRequested.RaiseEvent(
            request
        );
    }
}