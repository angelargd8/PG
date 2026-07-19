using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField]
    private string mainMenuScene = "MainMenu";

    [SerializeField]
    private string loadingScene = "LoadingScene";


    [SerializeField]
    private string prototypeScene = "SampleScene";

    //TODO: put the real scenes later

    public IEnumerator LoadInitialMenu()
    {
        yield return LoadAdditive(mainMenuScene);
        SetActiveScene(mainMenuScene);
    }


    public IEnumerator TransitionToPrototype()
    {
        // 1. Mostrar Loading.
        yield return LoadAdditive(loadingScene);
        SetActiveScene(loadingScene);

        // Espera un frame para que Loading pueda renderizarse.
        yield return null;

        // 2. Retirar el menú.
        yield return UnloadIfLoaded(mainMenuScene);

        // 3. Cargar la escena de prueba.
        yield return LoadAdditive(prototypeScene);
        SetActiveScene(prototypeScene);

        // Espera un frame para que la nueva escena se inicialice.
        yield return null;

        // 4. Retirar Loading.
        yield return UnloadIfLoaded(loadingScene);
    }

    private IEnumerator LoadAdditive(string sceneName)
    {
        Scene existingScene = SceneManager.GetSceneByName(sceneName);

        if (existingScene.isLoaded)
        {
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"La escena '{sceneName}' no está agregada al build.");

            yield break;
        }


        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);


        if (operation == null)
        {
            Debug.LogError(
                $"No se pudo iniciar la carga de '{sceneName}'.");

            yield break;
        }


        while (!operation.isDone)
        {
            yield return null;
        }

    }


    private IEnumerator UnloadIfLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (!scene.isLoaded)
        {
            yield break;
        }

        AsyncOperation operation =
            SceneManager.UnloadSceneAsync(scene);

        if (operation == null)
        {
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }
    }


    private void SetActiveScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError(
                $"No se puede activar la escena '{sceneName}'.");

            return;
        }

        SceneManager.SetActiveScene(scene);
    }





}
