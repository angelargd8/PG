using System.Collections;
using UnityEngine;

public sealed class ExperienceSceneBootstrap :
    MonoBehaviour
{
    // =========================
    // PRELOAD
    // =========================

    [Header("Preloaders")]

    [Tooltip(
        "Los componentes se preparan " +
        "en este orden."
    )]
    [SerializeField]
    private MonoBehaviour[] preloaders;


    // =========================
    // RUNTIME
    // =========================

    [Header("Runtime Systems")]

    [Tooltip(
        "Sistemas que comienzan cuando " +
        "la experiencia inicia."
    )]
    [SerializeField]
    private MonoBehaviour[] runtimeSystems;

    //========================
    // events
    // ==========================

    [Header("Events")]

    [SerializeField]
    private VoidEventChannelSO experienceReady;


    private void OnEnable()
    {
        if (experienceReady != null)
        {
            experienceReady.Raised +=
                HandleExperienceReady;
        }
    }


    private void OnDisable()
    {
        if (experienceReady != null)
        {
            experienceReady.Raised -=
                HandleExperienceReady;
        }


        EndExperience();
    }


    private void HandleExperienceReady()
    {
        BeginExperience();
    }

    // =========================
    // STATE
    // =========================

    private bool isPrepared;

    private bool isRunning;


    public bool IsPrepared =>
        isPrepared;

    public bool IsRunning =>
        isRunning;


    // =========================
    // PREPARE
    // =========================

    public IEnumerator Prepare()
    {
        if (isPrepared)
        {
            yield break;
        }


        Debug.Log(
            $"Preparando escena " +
            $"'{gameObject.scene.name}'.",
            this
        );


        if (preloaders != null)
        {
            for (
                int i = 0;
                i < preloaders.Length;
                i++
            )
            {
                MonoBehaviour behaviour =
                    preloaders[i];


                if (behaviour == null)
                {
                    continue;
                }


                if (
                    behaviour
                    is not IExperiencePreloadable preloadable
                )
                {
                    Debug.LogError(
                        $"{behaviour.name} no implementa " +
                        $"IExperiencePreloadable.",
                        behaviour
                    );

                    continue;
                }


                Debug.Log(
                    $"Preloading: " +
                    $"{behaviour.GetType().Name}",
                    behaviour
                );


                yield return
                    preloadable.Preload();


                yield return null;
            }
        }


        isPrepared = true;


        Debug.Log(
            $"Escena '{gameObject.scene.name}' preparada.",
            this
        );
    }


    // =========================
    // BEGIN
    // =========================

    public void BeginExperience()
    {
        if (!isPrepared)
        {
            Debug.LogError(
                "[ExperienceSceneBootstrap] " +
                "La escena todavía no está preparada.",
                this
            );

            return;
        }


        if (isRunning)
        {
            return;
        }


        isRunning = true;


        if (runtimeSystems != null)
        {
            for (
                int i = 0;
                i < runtimeSystems.Length;
                i++
            )
            {
                MonoBehaviour behaviour =
                    runtimeSystems[i];


                if (behaviour == null)
                {
                    continue;
                }


                if (
                    behaviour
                    is not IExperienceRuntime runtime
                )
                {
                    Debug.LogError(
                        $"{behaviour.name} no implementa " +
                        $"IExperienceRuntime.",
                        behaviour
                    );

                    continue;
                }


                runtime.BeginExperience();
            }
        }


        Debug.Log(
            $"Experiencia " +
            $"'{gameObject.scene.name}' iniciada.",
            this
        );
    }

    // =========================
    // END
    // =========================

    public void EndExperience()
    {
        if (!isRunning)
        {
            return;
        }


        if (runtimeSystems != null)
        {
            for (
                int i = runtimeSystems.Length - 1;
                i >= 0;
                i--
            )
            {
                MonoBehaviour behaviour =
                    runtimeSystems[i];


                if (behaviour == null)
                {
                    continue;
                }


                if (
                    behaviour
                    is IExperienceRuntime runtime
                )
                {
                    runtime.EndExperience();
                }
            }
        }


        isRunning = false;


        Debug.Log(
            $"Experiencia " +
            $"'{gameObject.scene.name}' detenida.",
            this
        );
    }
}