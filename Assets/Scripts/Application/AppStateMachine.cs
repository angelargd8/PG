using System.Collections;
using UnityEngine;

public sealed class AppStateMachine : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private SceneFlowManager sceneFlowManager;


    [Header("Event Channels")]

    [SerializeField]
    private ExperienceEventChannelSO experienceRequested;

    [SerializeField]
    private VoidEventChannelSO experienceTransitionStarted;

    [SerializeField]
    private VoidEventChannelSO experienceReady;

    [SerializeField]
    private VoidEventChannelSO mainMenuEntered;


    public AppState CurrentState
    {
        get;
        private set;
    }


    public ExperienceDefinitionSO CurrentExperience
    {
        get;
        private set;
    }


    private bool isTransitioning;


    // =========================
    // SUBSCRIPTIONS
    // =========================

    private void OnEnable()
    {
        if (experienceRequested != null)
        {
            experienceRequested.Raised +=
                HandleExperienceRequested;
        }
    }


    private void OnDisable()
    {
        if (experienceRequested != null)
        {
            experienceRequested.Raised -=
                HandleExperienceRequested;
        }
    }


    // =========================
    // INITIAL MENU
    // =========================

    private IEnumerator Start()
    {
        CurrentState =
            AppState.Booting;


        yield return
            sceneFlowManager.LoadInitialMenu();


        // Permitir Awake / OnEnable
        yield return null;


        CurrentState =
            AppState.MainMenu;


        if (mainMenuEntered != null)
        {
            Debug.Log(
                "AppStateMachine publica MainMenuEntered",
                this
            );

            mainMenuEntered.RaiseEvent();
        }
        else
        {
            Debug.LogError(
                "MainMenuEntered no está asignado.",
                this
            );
        }
    }


    // =========================
    // EXPERIENCE REQUEST
    // =========================

    private void HandleExperienceRequested(
        ExperienceRequest request
    )
    {
        Debug.Log(
            $"AppStateMachine recibió experiencia: " +
            $"{request.Experience.DisplayName}, " +
            $"StartIndex: {request.StartSceneIndex}, " +
            $"Full: {request.PlayFullSequence}",
            this
        );


        if (CurrentState != AppState.MainMenu)
        {
            Debug.LogWarning(
                $"No se puede iniciar experiencia. " +
                $"Estado actual: {CurrentState}",
                this
            );

            return;
        }


        if (isTransitioning)
        {
            return;
        }


        StartCoroutine(
            StartExperienceRoutine(request)
        );
    }


    // =========================
    // START EXPERIENCE
    // =========================

    private IEnumerator StartExperienceRoutine(
        ExperienceRequest request
    )
    {
        isTransitioning = true;

        CurrentState =
            AppState.Loading;


        CurrentExperience =
            request.Experience;


        // =========================
        // TRANSITION STARTED
        // =========================

        if (experienceTransitionStarted != null)
        {
            experienceTransitionStarted.RaiseEvent();
        }


        // =========================
        // LOAD EXPERIENCE
        // =========================

        yield return
            sceneFlowManager.TransitionToExperience(
                request
            );


        yield return null;


        // =========================
        // EXPERIENCE READY
        // =========================

        CurrentState =
            AppState.Experience;


        if (experienceReady != null)
        {
            Debug.Log(
                $"ExperienceReady: " +
                $"{request.Experience.DisplayName}",
                this
            );

            experienceReady.RaiseEvent();
        }


        isTransitioning = false;
    }
}