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
            ExperienceDiagnostics.LogSubscription("added", this, experienceRequested);
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised += HandleMainMenuRequested;
            ExperienceDiagnostics.LogSubscription("added", this, _mainMenuRequested);
        }
    }

    // Desuscribirse
    private void OnDisable()
    {
        if (experienceRequested != null)
        {
            experienceRequested.Raised -=
                HandleExperienceRequested;
            ExperienceDiagnostics.LogSubscription("removed", this, experienceRequested);
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised -= HandleMainMenuRequested;
            ExperienceDiagnostics.LogSubscription("removed", this, _mainMenuRequested);
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
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "state_changed", this, null,
            "state=MainMenu reason=initial_menu_loaded");


        if (_mainMenuEntered != null)
        {
            Debug.Log(
                "AppStateMachine publica MainMenuEntered",
                this
            );

            _mainMenuEntered.RaiseEvent(this);
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
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "experience_request_received", this, null,
            "experience=" + ExperienceDiagnostics.Field("name", request.Experience != null ? request.Experience.DisplayName : "null") +
            " mode=" + (request.PlayFullSequence ? "full" : "individual") + " start_index=" + request.StartSceneIndex);
        if (!_sceneFlowManager.ValidateRequest(request, out string error))
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "experience_request_rejected", this, null,
                ExperienceDiagnostics.Field("reason", error));
            Debug.LogError(error, this);
            ExperienceDiagnostics.EndRun("request_rejected");
            return;
        }
        Debug.Log(
            $"AppStateMachine recibi� experiencia: " +
            $"{request.Experience.DisplayName}, " +
            $"StartIndex: {request.StartSceneIndex}, " +
            $"Full: {request.PlayFullSequence}",
            this
        );


        if (CurrentState != AppState.MainMenu)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "experience_request_ignored", this, null,
                "reason=state_not_main_menu " + ExperienceDiagnostics.Field("state", CurrentState.ToString()));
            Debug.LogWarning(
                $"No se puede iniciar experiencia. " +
                $"Estado actual: {CurrentState}",
                this
            );

            return;
        }


        if (_isTransitioning)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "experience_request_ignored", this, null,
                "reason=transition_in_progress");
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
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "state_changed", this, null,
            "state=Loading reason=experience_request");




        if (experienceTransitionStarted != null)
        {
            experienceTransitionStarted.RaiseEvent(this);
        }


        yield return
            _sceneFlowManager.TransitionToExperience(
                request
            );


        yield return null;


        if (!_sceneFlowManager.TransitionSucceeded)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "experience_start_failed", this, null,
                "reason=scene_flow_transition_failed");
            yield return ReturnToMainMenuRoutine();
            yield break;
        }

        CurrentState =
            AppState.Experience;
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "state_changed", this, null,
            "state=Experience reason=scene_flow_prepared");


        if (experienceReady != null)
        {
            Debug.Log(
                $"ExperienceReady: " +
                $"{request.Experience.DisplayName}",
                this
            );

            experienceReady.RaiseEvent(this);
        }


        _isTransitioning = false;
    }

    public bool RequestMainMenu()
    {
        if (!isActiveAndEnabled || CurrentState != AppState.Experience || _isTransitioning)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "main_menu_request_ignored", this, null,
                "active=" + isActiveAndEnabled + " state=" + CurrentState + " transitioning=" + _isTransitioning);
            return false;
        }

        // usar el mismo canal como el pause menu para que el equipo tmb reciba el evento de salida
        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.RaiseEvent(this);
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
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "main_menu_request_ignored", this, null,
                "reason=state_not_experience state=" + CurrentState);
            return;
        }

        if (_isTransitioning)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "main_menu_request_ignored", this, null,
                "reason=transition_in_progress");
            return;
        }

        StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        _isTransitioning = true;

        CurrentState = AppState.Loading;
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "state_changed", this, null,
            "state=Loading reason=return_to_menu");

        yield return _sceneFlowManager.TransitionToMainMenu();

        yield return null;


        CurrentExperience = null;
        CurrentState = AppState.MainMenu;
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "state_changed", this, null,
            "state=MainMenu reason=return_complete");

        if (_mainMenuEntered != null)
        {
            _mainMenuEntered.RaiseEvent(this);
        }

        _isTransitioning = false;
        ExperienceDiagnostics.EndRun("returned_to_menu");
    }
}
