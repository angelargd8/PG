using UnityEngine;

public sealed class ExperienceLaunchButton : MonoBehaviour
{
    [Header("Experience")]

    [SerializeField]
    private ExperienceDefinitionSO experience;


    [Header("Start Scene")]

    [Tooltip(
        "vacio para comenzar desde " +
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
    [SerializeField]
    private bool playFullSequence = true;


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


        int startIndex = 0;


        if (startScene != null)
        {
            startIndex =
                experience.GetSceneIndex(
                    startScene
                );


            if (startIndex < 0)
            {
                Debug.LogError(
                    $"La escena '{startScene.name}' " +
                    $"no pertenece a la experiencia " +
                    $"'{experience.name}'.",
                    this
                );

                return;
            }
        }


        ExperienceRequest request =
            new ExperienceRequest(
                experience,
                startIndex,
                playFullSequence
            );


        experienceRequested.RaiseEvent(
            request
        );
    }
}