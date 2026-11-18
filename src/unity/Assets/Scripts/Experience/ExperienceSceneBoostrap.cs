using System.Collections;
using System;
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
        if (experienceReady != null)
        {
            experienceReady.Raised += HandleExperienceReady;
            ExperienceDiagnostics.LogSubscription("added", this, experienceReady);
        }
        if (sceneActivation != null)
        {
            sceneActivation.Raised += HandleSceneActivation;
            ExperienceDiagnostics.LogSubscription("added", this, sceneActivation);
        }
    }

    private void OnDisable()
    {
        if (experienceReady != null)
        {
            experienceReady.Raised -= HandleExperienceReady;
            ExperienceDiagnostics.LogSubscription("removed", this, experienceReady);
        }
        if (sceneActivation != null)
        {
            sceneActivation.Raised -= HandleSceneActivation;
            ExperienceDiagnostics.LogSubscription("removed", this, sceneActivation);
        }
        EndExperience();
    }

    public void SetExternallyControlled(bool value)
    {
        externallyControlled = value;
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "scene_activation_control_set", this, null,
            "externally_controlled=" + value);
    }
    private void HandleExperienceReady()
    {
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Detailed, "FLOW", "experience_ready_handler_entered", this, null,
            "externally_controlled=" + externallyControlled);
        if (!externallyControlled) BeginExperience();
        else ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "activation_ignored", this, null,
            "reason=externally_controlled mode=full");
    }
    private void HandleSceneActivation(string sceneName, bool active)
    {
        if (!externallyControlled || gameObject.scene.name != sceneName)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Detailed, "FLOW", "scene_activation_ignored", this, null,
                "requested_scene=" + ExperienceDiagnostics.Field("name", sceneName) +
                " owned_scene=" + ExperienceDiagnostics.Field("name", gameObject.scene.name) +
                " externally_controlled=" + externallyControlled);
            return;
        }
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "scene_activation_received", this, null,
            "scene=" + ExperienceDiagnostics.Field("name", sceneName) + " active=" + active);
        if (active) BeginExperience(); else EndExperience();
    }

    public IEnumerator Prepare()
    {
        var operation = ExperienceDiagnostics.BeginOperation("scene_prepare", this,
            "scene=" + ExperienceDiagnostics.Field("name", gameObject.scene.name));
        string result = "interrupted";
        int preloaderCount = 0;
        int runtimeCount = 0;
        try
        {
            if (IsPrepared) { result = "already_prepared"; yield break; }
            if (gameplayRoot != null && gameplayRoot.activeSelf)
            {
                Debug.LogError("El Gameplay Root debe estar guardado inactivo para precargar sin gameplay.", this);
                result = "rejected_gameplay_root_active";
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
            preloaderCount = orderedPreloaders.Count;
            runtimeCount = preparedRuntimes.Count;
            if (orderedPreloaders.Count > 0)
            {
                foreach (MonoBehaviour behaviour in orderedPreloaders)
                {
                    if (behaviour is not IExperiencePreloadable preloadable)
                    {
                        Debug.LogError("Preloader ausente o sin IExperiencePreloadable.", this);
                        result = "error_invalid_preloader";
                        yield break;
                    }
                    var preloadOperation = ExperienceDiagnostics.BeginOperation("preloader", behaviour,
                        "preloader=" + ExperienceDiagnostics.Field("type", behaviour.GetType().Name));
                    bool preloadCompleted = false;
                    try
                    {
                        yield return preloadable.Preload();
                        preloadCompleted = true;
                    }
                    finally
                    {
                        ExperienceDiagnostics.EndOperation(preloadOperation, preloadCompleted ? "completed" : "interrupted");
                    }
                    yield return null;
                }
            }
            IsPrepared = true;
            result = "prepared";
        }
        finally
        {
            ExperienceDiagnostics.EndOperation(operation, result,
                "preloader_count=" + preloaderCount + " runtime_count=" + runtimeCount);
        }
    }

    public void BeginExperience()
    {
        if (IsRunning)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Detailed, "FLOW", "begin_experience_ignored", this, null,
                "reason=already_running scene=" + ExperienceDiagnostics.Field("name", gameObject.scene.name));
            return;
        }
        if (!IsPrepared)
        {
            Debug.LogError("La escena todavia no esta preparada.", this);
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "begin_experience_rejected", this, null,
                "reason=scene_not_prepared scene=" + ExperienceDiagnostics.Field("name", gameObject.scene.name));
            return;
        }
        IsRunning = true;
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "begin_experience", this, null,
            "scene=" + ExperienceDiagnostics.Field("name", gameObject.scene.name) + " runtime_count=" + preparedRuntimes.Count);
        if (gameplayRoot != null) gameplayRoot.SetActive(true);
        if (preparedRuntimes.Count > 0)
            foreach (MonoBehaviour behaviour in preparedRuntimes)
                if (behaviour is IExperienceRuntime runtime)
                {
                    var runtimeOperation = ExperienceDiagnostics.BeginOperation("runtime_begin", behaviour,
                        "runtime=" + ExperienceDiagnostics.Field("type", behaviour.GetType().Name));
                    bool returned = false;
                    try
                    {
                        runtime.BeginExperience();
                        returned = true;
                    }
                    catch (Exception exception)
                    {
                        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "OP", "runtime_begin_exception", behaviour,
                            runtimeOperation.OperationId, "exception_type=" + exception.GetType().Name +
                            " exception_message=" + ExperienceDiagnostics.Field("message", exception.Message));
                        throw;
                    }
                    finally
                    {
                        ExperienceDiagnostics.EndOperation(runtimeOperation, returned ? "returned" : "error");
                    }
                }
                else Debug.LogError("Runtime ausente o sin IExperienceRuntime.", this);
        Debug.Log($"[ExperienceSceneBootstrap] Activada: {gameObject.scene.name}", this);
    }

    public void EndExperience()
    {
        if (!IsRunning)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Detailed, "FLOW", "end_experience_ignored", this, null,
                "reason=not_running scene=" + ExperienceDiagnostics.Field("name", gameObject.scene.name));
            return;
        }
        IsRunning = false;
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "end_experience", this, null,
            "scene=" + ExperienceDiagnostics.Field("name", gameObject.scene.name) + " runtime_count=" + preparedRuntimes.Count);
        for (int i = preparedRuntimes.Count - 1; i >= 0; i--)
            if (preparedRuntimes[i] != null && preparedRuntimes[i] is IExperienceRuntime runtime)
            {
                var runtimeOperation = ExperienceDiagnostics.BeginOperation("runtime_end", preparedRuntimes[i],
                    "runtime=" + ExperienceDiagnostics.Field("type", preparedRuntimes[i].GetType().Name) + " reverse_index=" + i);
                bool returned = false;
                try
                {
                    runtime.EndExperience();
                    returned = true;
                }
                catch (Exception exception)
                {
                    ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "OP", "runtime_end_exception", preparedRuntimes[i],
                        runtimeOperation.OperationId, "exception_type=" + exception.GetType().Name +
                        " exception_message=" + ExperienceDiagnostics.Field("message", exception.Message));
                    throw;
                }
                finally
                {
                    ExperienceDiagnostics.EndOperation(runtimeOperation, returned ? "returned" : "error");
                }
            }
        if (gameplayRoot != null) gameplayRoot.SetActive(false);
        Debug.Log($"[ExperienceSceneBootstrap] Desactivada: {gameObject.scene.name}", this);
    }
}
