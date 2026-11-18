using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    [Header("Common Scenes")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string loadingScene = "LoadingScene";
    [SerializeField] private string experienceCoreScene = "ExperienceCore";
    [SerializeField] private FullExperienceDirector fullExperienceDirector;
    [Header("End Credits Events")]
    [SerializeField] private VoidEventChannelSO songFinished;
    [SerializeField] private VoidEventChannelSO experienceCompleted;
    [SerializeField] private BoolEventChannelSO gameplayPauseChanged;
    private string currentExperienceScene;
    private ExperienceDefinitionSO currentDefinition;
    private ExperienceTransitionView endingView;
    private string creditsSceneName;
    private bool ending, endingCancelled, completionSent;
    public bool TransitionSucceeded { get; private set; }

    private void OnEnable()
    {
        if (songFinished != null)
        {
            songFinished.Raised += HandleSongFinished;
            ExperienceDiagnostics.LogSubscription("added", this, songFinished);
        }
    }

    private void OnDisable()
    {
        if (songFinished != null)
        {
            songFinished.Raised -= HandleSongFinished;
            ExperienceDiagnostics.LogSubscription("removed", this, songFinished);
        }
        endingCancelled = true;
        if (endingView != null) endingView.Clear();
    }

    public bool ValidateRequest(ExperienceRequest request, out string error)
    {
        if (request.Experience == null) { error = "Falta Experience Definition."; ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "request_validation", this, null, "result=rejected " + ExperienceDiagnostics.Field("reason", error)); return false; }
        if (request.Experience.CreditsScene != null)
        {
            if (songFinished == null || experienceCompleted == null || request.Experience.CreditsTransition == null)
            { error = "Los creditos necesitan Song Finished, Experience Completed y Credits Transition."; ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "request_validation", this, null, "result=rejected " + ExperienceDiagnostics.Field("reason", error)); return false; }
            if (!Application.CanStreamedLevelBeLoaded(request.Experience.CreditsScene.SceneName))
            { error = $"Falta {request.Experience.CreditsScene.SceneName} en Build Settings."; ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "request_validation", this, null, "result=rejected " + ExperienceDiagnostics.Field("reason", error)); return false; }
        }
        if (request.PlayFullSequence)
        {
            var sequence = request.Experience.FullSequence;
            if (fullExperienceDirector == null || sequence == null)
            { error = "Asigna Full Experience Director y Full Sequence."; ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "request_validation", this, null, "result=rejected " + ExperienceDiagnostics.Field("reason", error)); return false; }
            if (!sequence.Validate(out error)) { ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "request_validation", this, null, "result=rejected " + ExperienceDiagnostics.Field("reason", error)); return false; }
            for (int i = 0; i < sequence.Count; i++)
                if (!Application.CanStreamedLevelBeLoaded(sequence.GetSegment(i).Scene.SceneName))
                { error = $"Falta {sequence.GetSegment(i).Scene.SceneName} en Build Settings."; ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "request_validation", this, null, "result=rejected " + ExperienceDiagnostics.Field("reason", error)); return false; }
        }
        else
        {
            var scene = request.Experience.GetScene(request.StartSceneIndex);
            if (scene == null || !Application.CanStreamedLevelBeLoaded(scene.SceneName))
            { error = "La escena individual no existe en Build Settings."; ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "request_validation", this, null, "result=rejected " + ExperienceDiagnostics.Field("reason", error)); return false; }
        }
        error = null;
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "request_validation", this, null,
            "result=accepted mode=" + (request.PlayFullSequence ? "full" : "individual") +
            " experience=" + ExperienceDiagnostics.Field("name", request.Experience.DisplayName));
        return true;
    }

    public IEnumerator LoadInitialMenu()
    {
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "initial_menu_loading", this, null,
            "scene=" + ExperienceDiagnostics.Field("name", mainMenuScene));
        yield return LoadAdditive(mainMenuScene, this);
        SetActiveScene(mainMenuScene);
    }

    public IEnumerator TransitionToExperience(ExperienceRequest request)
    {
        var transitionOperation = ExperienceDiagnostics.BeginOperation("transition_to_experience", this,
            "mode=" + (request.PlayFullSequence ? "full" : "individual") +
            " experience=" + ExperienceDiagnostics.Field("name", request.Experience != null ? request.Experience.DisplayName : "none"));
        bool completed = false;
        TransitionSucceeded = false;
        try
        {
            if (!ValidateRequest(request, out string error)) { Debug.LogError(error, this); yield break; }
            currentDefinition = request.Experience;
            endingCancelled = false;
            completionSent = false;
            yield return LoadAdditive(loadingScene, this);
            SetActiveScene(loadingScene);
            yield return null;
            yield return UnloadIfLoaded(mainMenuScene, this);
            yield return LoadAdditive(experienceCoreScene, this);
            if (request.PlayFullSequence)
            {
                yield return fullExperienceDirector.Prepare(request.Experience.FullSequence);
                if (!fullExperienceDirector.IsPrepared) yield break;

                SetActiveScene(experienceCoreScene);
            }
            else
            {
                currentExperienceScene = request.Experience.GetScene(request.StartSceneIndex).SceneName;
                yield return LoadAdditive(currentExperienceScene, this);
                Scene scene = SceneManager.GetSceneByName(currentExperienceScene);
                ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "scene_loaded", this, null,
                    "scene=" + ExperienceDiagnostics.Field("name", currentExperienceScene) + " loaded=" + scene.isLoaded);
                var bootstrap = FindExperienceBootstrap(scene);
                if (bootstrap == null) { Debug.LogError($"Falta bootstrap en {currentExperienceScene}.", this); yield break; }
                bootstrap.SetExternallyControlled(false);
                var prepareOperation = ExperienceDiagnostics.BeginOperation("individual_scene_prepare_wait", bootstrap,
                    "target_scene=" + ExperienceDiagnostics.Field("name", currentExperienceScene) + " mode=individual");
                bool failed = false;
                yield return ExperiencePreloadOperation.Run(bootstrap.Prepare(), exception =>
                {
                    failed = true;
                    ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "OP", "scene_prepare_exception", bootstrap,
                        prepareOperation.OperationId, "exception_type=" + exception.GetType().Name,
                        prepareOperation.RunId);
                    Debug.LogError($"{currentExperienceScene}: {exception}", this);
                });
                if (failed) { ExperienceDiagnostics.EndOperation(prepareOperation, "error", "reason=preloader_exception"); yield break; }
                if (!bootstrap.IsPrepared) { ExperienceDiagnostics.EndOperation(prepareOperation, "error", "reason=bootstrap_not_prepared"); yield break; }
                ExperienceDiagnostics.EndOperation(prepareOperation, "prepared", "target_scene=" + ExperienceDiagnostics.Field("name", currentExperienceScene));
                bool active = SceneManager.SetActiveScene(scene);
                ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "scene_activation_state", this, null,
                    "scene=" + ExperienceDiagnostics.Field("name", currentExperienceScene) + " active=" + active + " prepared=" + bootstrap.IsPrepared);
            }
            yield return UnloadIfLoaded(loadingScene, this);
            TransitionSucceeded = true;
            completed = true;
        }
        finally
        {
            ExperienceDiagnostics.EndOperation(transitionOperation, completed ? "completed" : "failed",
                "transition_succeeded=" + TransitionSucceeded);
        }
    }

    public static IEnumerator LoadAdditive(string sceneName) => LoadAdditive(sceneName, null);

    public static IEnumerator LoadAdditive(string sceneName, UnityEngine.Object requester)
    {
        var diagnostic = ExperienceDiagnostics.BeginOperation("scene_load_additive", requester,
            "target_scene=" + ExperienceDiagnostics.Field("name", sceneName));
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            ExperienceDiagnostics.EndOperation(diagnostic, "already_loaded", "scene_state=loaded");
            yield break;
        }
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        { Debug.LogError($"La escena '{sceneName}' no esta agregada al Build."); ExperienceDiagnostics.EndOperation(diagnostic, "error", "reason=not_in_build_settings"); yield break; }
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
            ExperienceDiagnostics.EndOperation(diagnostic, "error", "reason=load_operation_null");
            yield break;
        }
        bool completed = false;
        try
        {
            yield return operation;
            completed = true;
            bool loaded = SceneManager.GetSceneByName(sceneName).isLoaded;
            ExperienceDiagnostics.EndOperation(diagnostic, loaded ? "loaded" : "error",
                "scene_state=" + (loaded ? "loaded" : "not_loaded"));
        }
        finally
        {
            if (!completed) ExperienceDiagnostics.EndOperation(diagnostic, "cancelled", "reason=coroutine_interrupted");
        }
    }

    public static IEnumerator UnloadIfLoaded(string sceneName) => UnloadIfLoaded(sceneName, null);

    public static IEnumerator UnloadIfLoaded(string sceneName, UnityEngine.Object requester)
    {
        var diagnostic = ExperienceDiagnostics.BeginOperation("scene_unload", requester,
            "target_scene=" + ExperienceDiagnostics.Field("name", sceneName));
        if (string.IsNullOrEmpty(sceneName)) { ExperienceDiagnostics.EndOperation(diagnostic, "skipped", "reason=empty_scene_name"); yield break; }
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) { ExperienceDiagnostics.EndOperation(diagnostic, "already_unloaded", "scene_state=not_loaded"); yield break; }
        AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
        if (operation == null) { ExperienceDiagnostics.EndOperation(diagnostic, "error", "reason=unload_operation_null"); yield break; }
        bool completed = false;
        try
        {
            yield return operation;
            completed = true;
            bool unloaded = !SceneManager.GetSceneByName(sceneName).isLoaded;
            ExperienceDiagnostics.EndOperation(diagnostic, unloaded ? "unloaded" : "error",
                "scene_state=" + (unloaded ? "not_loaded" : "still_loaded"));
        }
        finally
        {
            if (!completed) ExperienceDiagnostics.EndOperation(diagnostic, "cancelled", "reason=coroutine_interrupted");
        }
    }

    private void SetActiveScene(string name)
    {
        Scene scene = SceneManager.GetSceneByName(name);
        if (scene.IsValid() && scene.isLoaded)
        {
            bool activated = SceneManager.SetActiveScene(scene);
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "OP", "scene_set_active", this, null,
                "phase=finish result=" + (activated ? "activated" : "failed") +
                " target_scene=" + ExperienceDiagnostics.Field("name", name));
        }
        else ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "OP", "scene_set_active", this, null,
            "phase=error result=not_loaded target_scene=" + ExperienceDiagnostics.Field("name", name));
    }

    public IEnumerator TransitionToMainMenu()
    {
        var operation = ExperienceDiagnostics.BeginOperation("transition_to_menu", this);
        bool completed = false;
        try
        {
            endingCancelled = true;
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "closing_cancel_requested", this, operation.OperationId,
                "credits_in_progress=" + ending);
            while (ending) yield return null;
            yield return UnloadIfLoaded(creditsSceneName, this);
            creditsSceneName = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            yield return LoadAdditive(loadingScene, this);
            SetActiveScene(loadingScene);
            if (fullExperienceDirector != null) yield return fullExperienceDirector.Shutdown();
            if (!string.IsNullOrEmpty(currentExperienceScene))
                FindExperienceBootstrap(SceneManager.GetSceneByName(currentExperienceScene))?.EndExperience();
            yield return UnloadIfLoaded(currentExperienceScene, this);
            yield return UnloadIfLoaded(experienceCoreScene, this);
            yield return LoadAdditive(mainMenuScene, this);
            SetActiveScene(mainMenuScene);
            currentExperienceScene = null;
            currentDefinition = null;
            yield return UnloadIfLoaded(loadingScene, this);
            completed = true;
        }
        finally
        {
            ExperienceDiagnostics.EndOperation(operation, completed ? "completed" : "cancelled",
                "main_menu_loaded=" + SceneManager.GetSceneByName(mainMenuScene).isLoaded);
        }
    }

    private void HandleSongFinished()
    {
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "song_finished_received", this, null,
            "has_definition=" + (currentDefinition != null) + " ending=" + ending +
            " completion_sent=" + completionSent + " ending_cancelled=" + endingCancelled);
        if (currentDefinition == null || ending || completionSent || endingCancelled)
        {
            string reason = currentDefinition == null ? "no_current_definition" : ending ? "ending_already_running" : completionSent ? "completion_already_sent" : "ending_cancelled";
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "song_finished_ignored", this, null,
                "reason=" + reason);
            return;
        }
        if (currentDefinition.CreditsScene == null)
        {
            CompleteExperience();
            return;
        }
        ending = true;
        StartCoroutine(ShowCredits());
    }

    private IEnumerator ShowCredits()
    {
        var creditsOperation = ExperienceDiagnostics.BeginOperation("credits_sequence", this,
            "credits_scene=" + ExperienceDiagnostics.Field("name", currentDefinition != null && currentDefinition.CreditsScene != null ? currentDefinition.CreditsScene.SceneName : "none"));
        bool creditsCompleted = false;
        var transition = currentDefinition.CreditsTransition;
        if (endingView == null)
        {
            
            var overlayHost = new GameObject("End Credits Transition");
            overlayHost.transform.SetParent(transform, false);
            endingView = overlayHost.AddComponent<ExperienceTransitionView>();
        }
        Time.timeScale = 0f;
        AudioListener.pause = true;
        gameplayPauseChanged?.RaiseEvent(true, this);
        try
        {
            yield return FadeCredits(transition, false);
            if (endingCancelled) yield break;
            if (fullExperienceDirector != null) yield return fullExperienceDirector.Shutdown();
            if (!string.IsNullOrEmpty(currentExperienceScene))
                FindExperienceBootstrap(SceneManager.GetSceneByName(currentExperienceScene))?.EndExperience();
            yield return UnloadIfLoaded(currentExperienceScene, this);
            currentExperienceScene = null;
            if (endingCancelled) yield break;

            creditsSceneName = currentDefinition.CreditsScene.SceneName;
            yield return LoadAdditive(creditsSceneName, this);
            if (endingCancelled) yield break;
            if (SceneManager.GetSceneByName(creditsSceneName).isLoaded)
            {
                SetActiveScene(creditsSceneName);
                // Let the credits canvas bind to the persistent XR camera while still covered.
                yield return null;
                yield return FadeCredits(transition, true);
                double until = Time.realtimeSinceStartupAsDouble + currentDefinition.CreditsVisibleSeconds;
                while (!endingCancelled && Time.realtimeSinceStartupAsDouble < until) yield return null;
                if (endingCancelled) yield break;
                MetricsUploader metricsUploader = FindFirstObjectByType<MetricsUploader>();
                if (metricsUploader != null)
                {
                    ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "metrics_upload_wait_started", this, creditsOperation.OperationId);
                    while (
                        !endingCancelled &&
                        !metricsUploader
                            .CurrentSessionUploadFinished
                    )
                    {
                        yield return null;
                    }
                    ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "metrics_upload_wait_finished", this, creditsOperation.OperationId,
                        "upload_finished=" + metricsUploader.CurrentSessionUploadFinished);
                }
                if (endingCancelled) yield break;
                yield return FadeCredits(transition, false);
            }
            if (endingCancelled) yield break;
            yield return UnloadIfLoaded(creditsSceneName, this);
            creditsSceneName = null;
            SetActiveScene(experienceCoreScene);
            if (endingCancelled) yield break;
            
            CompleteExperience();
            yield return FadeCredits(transition, true);
            creditsCompleted = !endingCancelled;
        }
        finally
        {
            endingView.Clear();
            ending = false;
            ExperienceDiagnostics.EndOperation(creditsOperation, creditsCompleted ? "completed" : "cancelled",
                "cancelled=" + endingCancelled);
        }
    }

    private IEnumerator FadeCredits(ExperienceTransitionSO transition, bool reveal)
    {
        var operation = ExperienceDiagnostics.BeginOperation(reveal ? "credits_reveal" : "credits_fade_out", this,
            "lead_time_s=" + transition.LeadTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
            " reveal_time_s=" + transition.RevealTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
        double duration = reveal ? transition.RevealTime : transition.LeadTime;
        double start = Time.realtimeSinceStartupAsDouble;
        while (!endingCancelled && Time.realtimeSinceStartupAsDouble - start < duration)
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - start;
            transition.Present(endingView, reveal ? elapsed : elapsed - duration, false);
            yield return null;
        }
        if (!endingCancelled) transition.Present(endingView, reveal ? duration : 0, !reveal);
        ExperienceDiagnostics.EndOperation(operation, endingCancelled ? "cancelled" : "completed",
            "actual_duration_s=" + (Time.realtimeSinceStartupAsDouble - start).ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
    }

    private void CompleteExperience()
    {
        if (completionSent || endingCancelled)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "experience_completion_ignored", this, null,
                "reason=" + (completionSent ? "already_sent" : "ending_cancelled"));
            return;
        }
        completionSent = true;
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "experience_completion_requested", this, null,
            "channel=" + ExperienceDiagnostics.Field("name", experienceCompleted != null ? experienceCompleted.name : "missing"));
        experienceCompleted?.RaiseEvent(this);
    }

    public static ExperienceSceneBootstrap FindExperienceBootstrap(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            var bootstrap = root.GetComponentInChildren<ExperienceSceneBootstrap>(true);
            if (bootstrap != null) return bootstrap;
        }
        return null;
    }
}
