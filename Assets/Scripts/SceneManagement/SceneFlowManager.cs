using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    [Header("Common Scenes")]

    [SerializeField]
    private string mainMenuScene =
        "MainMenu";

    [SerializeField]
    private string loadingScene =
        "LoadingScene";

    [SerializeField]
    private string experienceCoreScene =
        "ExperienceCore";


    private string currentExperienceScene;


    // =========================
    // MAIN MENU
    // =========================

    public IEnumerator LoadInitialMenu()
    {
        yield return
            LoadAdditive(mainMenuScene);

        SetActiveScene(
            mainMenuScene
        );
    }


    // =========================
    // EXPERIENCE
    // =========================

    public IEnumerator TransitionToExperience(
    ExperienceRequest request
)
    {
        ExperienceDefinitionSO experience =
            request.Experience;

        if (experience == null)
        {
            Debug.LogError(
                "ExperienceDefinition es null.",
                this
            );

            yield break;
        }


        ExperienceSceneDefinitionSO sceneDefinition =
            experience.GetScene(
                request.StartSceneIndex
            );


        if (sceneDefinition == null)
        {
            Debug.LogError(
                $"No existe la escena índice " +
                $"{request.StartSceneIndex} en " +
                $"'{experience.DisplayName}'.",
                this
            );

            yield break;
        }


        string targetScene =
            sceneDefinition.SceneName;


        Debug.Log(
            $"SceneFlowManager cargará '{targetScene}'. " +
            $"FullSequence: {request.PlayFullSequence}",
            this
        );


        // Loading
        yield return LoadAdditive(
            loadingScene
        );

        SetActiveScene(
            loadingScene
        );

        yield return null;


        // Quitar Main Menu
        yield return UnloadIfLoaded(
            mainMenuScene
        );


        // Experience Core
        yield return LoadAdditive(
            experienceCoreScene
        );


        // Escena de la experiencia
        yield return LoadAdditive(
            targetScene
        );


        Scene experienceScene =
            SceneManager.GetSceneByName(
                targetScene
            );


        if (!experienceScene.IsValid() ||
            !experienceScene.isLoaded)
        {
            Debug.LogError(
                $"La escena '{targetScene}' " +
                $"no pudo cargarse."
            );

            yield break;
        }


        // Danniel pasa a ser la escena activa
        SceneManager.SetActiveScene(
            experienceScene
        );


        // Permitir Awake / OnEnable
        yield return null;


        // =========================
        // PREPARAR LA EXPERIENCIA
        // =========================

        ExperienceSceneBootstrap bootstrap =
            FindExperienceBootstrap(
                experienceScene
            );


        if (bootstrap == null)
        {
            Debug.LogError(
                $"La escena '{targetScene}' no tiene " +
                $"ExperienceSceneBootstrap."
            );

            yield break;
        }


        yield return bootstrap.Prepare();


        // Un frame después del prewarm
        yield return null;


        currentExperienceScene =
            targetScene;


        // Quitar Loading
        yield return UnloadIfLoaded(
            loadingScene
        );
    }


    // =========================
    // LOAD
    // =========================

    private IEnumerator LoadAdditive(
        string sceneName
    )
    {
        Scene existing =
            SceneManager.GetSceneByName(
                sceneName
            );


        if (existing.isLoaded)
        {
            yield break;
        }


        if (!Application
            .CanStreamedLevelBeLoaded(
                sceneName
            ))
        {
            Debug.LogError(
                $"La escena '{sceneName}' " +
                $"NO está agregada al Build."
            );

            yield break;
        }


        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Additive
            );


        if (operation == null)
        {
            Debug.LogError(
                $"No se pudo iniciar carga de " +
                $"'{sceneName}'."
            );

            yield break;
        }


        while (!operation.isDone)
        {
            yield return null;
        }
    }


    // =========================
    // UNLOAD
    // =========================

    private IEnumerator UnloadIfLoaded(
        string sceneName
    )
    {
        Scene scene =
            SceneManager.GetSceneByName(
                sceneName
            );


        if (!scene.isLoaded)
        {
            yield break;
        }


        AsyncOperation operation =
            SceneManager.UnloadSceneAsync(
                scene
            );


        if (operation == null)
        {
            yield break;
        }


        while (!operation.isDone)
        {
            yield return null;
        }
    }


    // =========================
    // ACTIVE SCENE
    // =========================

    private void SetActiveScene(
        string sceneName
    )
    {
        Scene scene =
            SceneManager.GetSceneByName(
                sceneName
            );


        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            Debug.LogError(
                $"No se puede activar " +
                $"'{sceneName}'."
            );

            return;
        }


        SceneManager.SetActiveScene(
            scene
        );
    }

    private ExperienceSceneBootstrap FindExperienceBootstrap(
    Scene scene
)
    {
        GameObject[] roots =
            scene.GetRootGameObjects();


        for (int i = 0;
             i < roots.Length;
             i++)
        {
            ExperienceSceneBootstrap bootstrap =
                roots[i]
                    .GetComponentInChildren<
                        ExperienceSceneBootstrap>(
                            true
                        );


            if (bootstrap != null)
            {
                return bootstrap;
            }
        }


        return null;
    }

}