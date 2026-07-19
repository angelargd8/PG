using System.Collections;
using UnityEngine;

public sealed class AppStateMachine : MonoBehaviour
{
    [Header("Dependencies")]


    [SerializeField]
    private SceneFlowManager sceneFlowManager;

    [Header("Event Channels")]
    [SerializeField]
    private VoidEventChannelSO startExperienceRequested;


    [SerializeField] 
    private VoidEventChannelSO mainMenuEntered;

    public AppState CurrentState { get; private set; }

    private bool isTransitioning;


    // suscribe
    private void OnEnable()
    {
        if (startExperienceRequested != null)
        {
            startExperienceRequested.Raised += HandleStartExperienceRequested;

        }
    }

    // unsuscribe
    private void OnDisable()
    {
        if (startExperienceRequested != null)
        {
            startExperienceRequested.Raised -= HandleStartExperienceRequested;

        }
    }


    private void HandleStartExperienceRequested()
    {
        if (CurrentState != AppState.MainMenu || isTransitioning)
        {
            return;
        }

        StartCoroutine(StartExperienceRoutine());
    }

    private IEnumerator Start()
    {
        CurrentState = AppState.Booting;
        yield return sceneFlowManager.LoadInitialMenu();

        // Permite que todos los objetos del MainMenu completen
        // Awake y OnEnable antes de publicar el evento.
        yield return null;

        CurrentState = AppState.MainMenu;

        if (mainMenuEntered == null)
        {
            Debug.LogError(
                "MainMenuEntered no está asignado en AppStateMachine.",
                this);

            yield break;
        }

        Debug.Log(
            $"AppStateMachine publica '{mainMenuEntered.name}'.",
            this);

        mainMenuEntered.RaiseEvent();


    }



    private IEnumerator StartExperienceRoutine()
    {
        isTransitioning = true;
        CurrentState = AppState.Loading;

        yield return sceneFlowManager.TransitionToPrototype();

        CurrentState = AppState.Experience;

        isTransitioning = false;
    }



}
