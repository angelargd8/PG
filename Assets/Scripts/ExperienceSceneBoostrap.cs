using System.Collections;
using UnityEngine;

public sealed class ExperienceSceneBootstrap
    : MonoBehaviour
{
    [Header("Preloaders")]

    [Tooltip(
        "Los componentes se preparan " +
        "en este orden "
    )]
    [SerializeField]
    private MonoBehaviour[] preloaders;


    bool isPrepared;


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


        for (int i = 0;
             i < preloaders.Length;
             i++)
        {
            MonoBehaviour behaviour =
                preloaders[i];


            if (behaviour == null)
            {
                continue;
            }


            if (behaviour
                is not IExperiencePreloadable preloadable)
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


            // Dejar respirar un frame
            // entre sistemas
            yield return null;
        }


        isPrepared = true;


        Debug.Log(
            $"Escena '{gameObject.scene.name}' preparada ",
            this
        );
    }
}