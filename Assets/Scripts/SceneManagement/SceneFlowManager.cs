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
    private string currentExperienceScene;
    public bool TransitionSucceeded { get; private set; }

    public bool ValidateRequest(ExperienceRequest request, out string error)
    {
        if (request.Experience == null) { error = "Falta Experience Definition."; return false; }
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
        yield return LoadAdditive(loadingScene);
        SetActiveScene(loadingScene);
        yield return null;
        yield return UnloadIfLoaded(mainMenuScene);
        yield return LoadAdditive(experienceCoreScene);
        if (request.PlayFullSequence)
        {
            yield return fullExperienceDirector.Prepare(request.Experience.FullSequence);
            if (!fullExperienceDirector.IsPrepared) yield break;
            // Core remains active while both gameplay scenes are only prepared.
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
        yield return UnloadIfLoaded(loadingScene);
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
