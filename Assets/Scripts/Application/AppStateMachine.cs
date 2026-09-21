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


    // Suscribirse 
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

    // Desuscribirse
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


    private IEnumerator Start()
    {
        CurrentState =
            AppState.Booting;


        yield return
            _sceneFlowManager.LoadInitialMenu();

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



    private IEnumerator StartExperienceRoutine(
        ExperienceRequest request
    )
    {
        _isTransitioning = true;

        CurrentState =
            AppState.Loading;


        CurrentExperience =
            request.Experience;




        if (experienceTransitionStarted != null)
        {
            experienceTransitionStarted.RaiseEvent();
        }


        yield return
            _sceneFlowManager.TransitionToExperience(
                request
            );


        yield return null;


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

    public bool RequestMainMenu()
    {
        if (!isActiveAndEnabled || CurrentState != AppState.Experience || _isTransitioning)
        {
            return false;
        }

        // usar el mismo canal como el pause menu para que el equipo tmb reciba el evento de salida
        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.RaiseEvent();
        }
        else
        {
            HandleMainMenuRequested();
        }

        return true;
    }

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
