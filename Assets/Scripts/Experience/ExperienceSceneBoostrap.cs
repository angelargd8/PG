using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public sealed class ExperienceSceneBootstrap : MonoBehaviour
{
    [Header("Preloaders (ordered)")]
    [SerializeField] private MonoBehaviour[] preloaders;
    [Header("Runtime Systems (ordered)")]
    [SerializeField] private MonoBehaviour[] runtimeSystems;
    [Header("Staged activation")]
    [Tooltip("Contenido de la escena, guardado INACTIVO. El bootstrap debe quedar fuera de este root.")]
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private ExperienceSceneActivationEventChannelSO sceneActivation;
    [Header("Events")]
    [SerializeField] private VoidEventChannelSO experienceReady;

    private bool externallyControlled;
    private readonly List<MonoBehaviour> preparedRuntimes = new();
    public bool IsPrepared { get; private set; }
    public bool IsRunning { get; private set; }
    public bool SupportsStagedActivation => gameplayRoot != null && sceneActivation != null &&
        !transform.IsChildOf(gameplayRoot.transform);

    private void OnEnable()
    {
        if (experienceReady != null) experienceReady.Raised += HandleExperienceReady;
        if (sceneActivation != null) sceneActivation.Raised += HandleSceneActivation;
    }

    private void OnDisable()
    {
        if (experienceReady != null) experienceReady.Raised -= HandleExperienceReady;
        if (sceneActivation != null) sceneActivation.Raised -= HandleSceneActivation;
        EndExperience();
    }

    public void SetExternallyControlled(bool value) => externallyControlled = value;
    private void HandleExperienceReady() { if (!externallyControlled) BeginExperience(); }
    private void HandleSceneActivation(string sceneName, bool active)
    {
        if (!externallyControlled || gameObject.scene.name != sceneName) return;
        if (active) BeginExperience(); else EndExperience();
    }

    public IEnumerator Prepare()
    {
        if (IsPrepared) yield break;
        if (gameplayRoot != null && gameplayRoot.activeSelf)
        {
            Debug.LogError("El Gameplay Root debe estar guardado inactivo para precargar sin gameplay.", this);
            yield break;
        }
        var orderedPreloaders = new List<MonoBehaviour>();
        if (preloaders != null) orderedPreloaders.AddRange(preloaders);
        preparedRuntimes.Clear();
        if (runtimeSystems != null) preparedRuntimes.AddRange(runtimeSystems);
        // Explicit lists preserve dependency order. Include prefab-local weapons/pools too.
        if (gameplayRoot != null)
            foreach (MonoBehaviour behaviour in gameplayRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || !behaviour.enabled) continue;
                if (behaviour is IExperiencePreloadable && !orderedPreloaders.Contains(behaviour))
                    orderedPreloaders.Add(behaviour);
                if (behaviour is IExperienceRuntime && !preparedRuntimes.Contains(behaviour))
                    preparedRuntimes.Add(behaviour);
            }
        if (orderedPreloaders.Count > 0)
        {
            foreach (MonoBehaviour behaviour in orderedPreloaders)
            {
                if (behaviour is not IExperiencePreloadable preloadable)
                {
                    Debug.LogError("Preloader ausente o sin IExperiencePreloadable.", this);
                    yield break;
                }
                yield return preloadable.Preload();
                yield return null;
            }
        }
        IsPrepared = true;
    }

    public void BeginExperience()
    {
        if (IsRunning) return;
        if (!IsPrepared)
        {
            Debug.LogError("La escena todavia no esta preparada.", this);
            return;
        }
        IsRunning = true;
        if (gameplayRoot != null) gameplayRoot.SetActive(true);
        if (preparedRuntimes.Count > 0)
            foreach (MonoBehaviour behaviour in preparedRuntimes)
                if (behaviour is IExperienceRuntime runtime) runtime.BeginExperience();
                else Debug.LogError("Runtime ausente o sin IExperienceRuntime.", this);
        Debug.Log($"[ExperienceSceneBootstrap] Activada: {gameObject.scene.name}", this);
    }

    public void EndExperience()
    {
        if (!IsRunning) return;
        IsRunning = false;
        for (int i = preparedRuntimes.Count - 1; i >= 0; i--)
            if (preparedRuntimes[i] is IExperienceRuntime runtime) runtime.EndExperience();
        if (gameplayRoot != null) gameplayRoot.SetActive(false);
        Debug.Log($"[ExperienceSceneBootstrap] Desactivada: {gameObject.scene.name}", this);
    }
}
