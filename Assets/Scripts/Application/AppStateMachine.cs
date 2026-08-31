using System.Collections;
using UnityEngine;

public sealed class AppStateMachine : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private SceneFlowManager _sceneFlowManager;


    [Header("Event Channels")]

    [SerializeField]
    private ExperienceEventChannelSO experienceRequested;

    [SerializeField]
    private VoidEventChannelSO experienceTransitionStarted;

    [SerializeField]
    private VoidEventChannelSO experienceReady;

    [SerializeField]
    private VoidEventChannelSO _mainMenuEntered;

    [SerializeField] private VoidEventChannelSO _mainMenuRequested;


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


    private bool _isTransitioning;


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

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised += HandleMainMenuRequested;
        }
    }


    private void OnDisable()
    {
        if (experienceRequested != null)
        {
            experienceRequested.Raised -=
                HandleExperienceRequested;
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised -= HandleMainMenuRequested;
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
            _sceneFlowManager.LoadInitialMenu();


        // Permitir Awake / OnEnable
        yield return null;


        CurrentState =
            AppState.MainMenu;


        if (_mainMenuEntered != null)
        {
            Debug.Log(
                "AppStateMachine publica MainMenuEntered",
                this
            );

            _mainMenuEntered.RaiseEvent();
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
            $"AppStateMachine recibi� experiencia: " +
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


        if (_isTransitioning)
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
        _isTransitioning = true;

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
            _sceneFlowManager.TransitionToExperience(
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


        _isTransitioning = false;
    }

    // =========================
    // BACK TO MAIN MENU REQUEST
    // =========================
    private void HandleMainMenuRequested()
    {
        if (CurrentState != AppState.Experience)
        {
            return;
        }

        if (_isTransitioning)
        {
            return;
        }

        StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        _isTransitioning = true;

        CurrentState = AppState.Loading;

        yield return _sceneFlowManager.TransitionToMainMenu();

        yield return null;


        CurrentExperience = null;
        CurrentState = AppState.MainMenu;

        if (_mainMenuEntered != null)
        {
            _mainMenuEntered.RaiseEvent();
        }

        _isTransitioning = false;
    }
}