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
        if (songFinished != null) songFinished.Raised += HandleSongFinished;
    }

    private void OnDisable()
    {
        if (songFinished != null) songFinished.Raised -= HandleSongFinished;
        endingCancelled = true;
        if (endingView != null) endingView.Clear();
    }

    public bool ValidateRequest(ExperienceRequest request, out string error)
    {
        if (request.Experience == null) { error = "Falta Experience Definition."; return false; }
        if (request.Experience.CreditsScene != null)
        {
            if (songFinished == null || experienceCompleted == null || request.Experience.CreditsTransition == null)
            { error = "Los creditos necesitan Song Finished, Experience Completed y Credits Transition."; return false; }
            if (!Application.CanStreamedLevelBeLoaded(request.Experience.CreditsScene.SceneName))
            { error = $"Falta {request.Experience.CreditsScene.SceneName} en Build Settings."; return false; }
        }
        if (request.PlayFullSequence)
        {
            var sequence = request.Experience.FullSequence;
            if (fullExperienceDirector == null || sequence == null)
            { error = "Asigna Full Experience Director y Full Sequence."; return false; }
            if (!sequence.Validate(out error)) return false;
            for (int i = 0; i < sequence.Count; i++)
                if (!Application.CanStreamedLevelBeLoaded(sequence.GetSegment(i).Scene.SceneName))
                { error = $"Falta {sequence.GetSegment(i).Scene.SceneName} en Build Settings."; return false; }
        }
        else
        {
            var scene = request.Experience.GetScene(request.StartSceneIndex);
            if (scene == null || !Application.CanStreamedLevelBeLoaded(scene.SceneName))
            { error = "La escena individual no existe en Build Settings."; return false; }
        }
        error = null;
        return true;
    }

    public IEnumerator LoadInitialMenu()
    {
        yield return LoadAdditive(mainMenuScene);
        SetActiveScene(mainMenuScene);
    }

    public IEnumerator TransitionToExperience(ExperienceRequest request)
    {
        TransitionSucceeded = false;
        if (!ValidateRequest(request, out string error)) { Debug.LogError(error, this); yield break; }
        currentDefinition = request.Experience;
        endingCancelled = false;
        completionSent = false;
        yield return LoadAdditive(loadingScene);
        SetActiveScene(loadingScene);
        yield return null;
        yield return UnloadIfLoaded(mainMenuScene);
        yield return LoadAdditive(experienceCoreScene);
        if (request.PlayFullSequence)
        {
            yield return fullExperienceDirector.Prepare(request.Experience.FullSequence);
            if (!fullExperienceDirector.IsPrepared) yield break;
            
            SetActiveScene(experienceCoreScene);
        }
        else
        {
            currentExperienceScene = request.Experience.GetScene(request.StartSceneIndex).SceneName;
            yield return LoadAdditive(currentExperienceScene);
            Scene scene = SceneManager.GetSceneByName(currentExperienceScene);
            var bootstrap = FindExperienceBootstrap(scene);
            if (bootstrap == null) { Debug.LogError($"Falta bootstrap en {currentExperienceScene}.", this); yield break; }
            bootstrap.SetExternallyControlled(false);
            bool failed = false;
            yield return ExperiencePreloadOperation.Run(bootstrap.Prepare(), exception =>
            {
                failed = true;
                Debug.LogError($"{currentExperienceScene}: {exception.Message}", this);
            });
            if (failed) yield break;
            if (!bootstrap.IsPrepared) yield break;
            SceneManager.SetActiveScene(scene);
        }
        yield return UnloadIfLoaded(loadingScene);
        TransitionSucceeded = true;
    }

    public static IEnumerator LoadAdditive(string sceneName)
    {
        if (SceneManager.GetSceneByName(sceneName).isLoaded) yield break;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        { Debug.LogError($"La escena '{sceneName}' no esta agregada al Build."); yield break; }
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (operation != null) yield return operation;
    }

    public static IEnumerator UnloadIfLoaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) yield break;
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) yield break;
        AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
        if (operation != null) yield return operation;
    }

    private static void SetActiveScene(string name)
    {
        Scene scene = SceneManager.GetSceneByName(name);
        if (scene.IsValid() && scene.isLoaded) SceneManager.SetActiveScene(scene);
    }

    public IEnumerator TransitionToMainMenu()
    {
        
        endingCancelled = true;
        while (ending) yield return null;
        yield return UnloadIfLoaded(creditsSceneName);
        creditsSceneName = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        yield return LoadAdditive(loadingScene);
        SetActiveScene(loadingScene);
        if (fullExperienceDirector != null) yield return fullExperienceDirector.Shutdown();
        if (!string.IsNullOrEmpty(currentExperienceScene))
            FindExperienceBootstrap(SceneManager.GetSceneByName(currentExperienceScene))?.EndExperience();
        yield return UnloadIfLoaded(currentExperienceScene);
        yield return UnloadIfLoaded(experienceCoreScene);
        yield return LoadAdditive(mainMenuScene);
        SetActiveScene(mainMenuScene);
        currentExperienceScene = null;
        currentDefinition = null;
        yield return UnloadIfLoaded(loadingScene);
    }

    private void HandleSongFinished()
    {
        if (currentDefinition == null || ending || completionSent || endingCancelled) return;
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
        var transition = currentDefinition.CreditsTransition;
        if (endingView == null)
        {
            
            var overlayHost = new GameObject("End Credits Transition");
            overlayHost.transform.SetParent(transform, false);
            endingView = overlayHost.AddComponent<ExperienceTransitionView>();
        }
        Time.timeScale = 0f;
        AudioListener.pause = true;
        gameplayPauseChanged?.RaiseEvent(true);
        try
        {
            yield return FadeCredits(transition, false);
            if (endingCancelled) yield break;
            if (fullExperienceDirector != null) yield return fullExperienceDirector.Shutdown();
            if (!string.IsNullOrEmpty(currentExperienceScene))
                FindExperienceBootstrap(SceneManager.GetSceneByName(currentExperienceScene))?.EndExperience();
            yield return UnloadIfLoaded(currentExperienceScene);
            currentExperienceScene = null;
            if (endingCancelled) yield break;

            creditsSceneName = currentDefinition.CreditsScene.SceneName;
            yield return LoadAdditive(creditsSceneName);
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
                yield return FadeCredits(transition, false);
            }
            if (endingCancelled) yield break;
            yield return UnloadIfLoaded(creditsSceneName);
            creditsSceneName = null;
            SetActiveScene(experienceCoreScene);
            if (endingCancelled) yield break;
            
            CompleteExperience();
            yield return FadeCredits(transition, true);
        }
        finally
        {
            endingView.Clear();
            ending = false;
            
        }
    }

    private IEnumerator FadeCredits(ExperienceTransitionSO transition, bool reveal)
    {
        double duration = reveal ? transition.RevealTime : transition.LeadTime;
        double start = Time.realtimeSinceStartupAsDouble;
        while (!endingCancelled && Time.realtimeSinceStartupAsDouble - start < duration)
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - start;
            transition.Present(endingView, reveal ? elapsed : elapsed - duration, false);
            yield return null;
        }
        if (!endingCancelled) transition.Present(endingView, reveal ? duration : 0, !reveal);
    }

    private void CompleteExperience()
    {
        if (completionSent || endingCancelled) return;
        completionSent = true;
        experienceCompleted?.RaiseEvent();
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
