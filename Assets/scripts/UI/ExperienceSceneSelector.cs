using UnityEngine;

public sealed class ExperienceSceneSelector : MonoBehaviour
{
    [Header("Experience Scenes")]

    [SerializeField]
    private ExperienceSceneDefinitionSO dannielScene;

    [SerializeField]
    private ExperienceSceneDefinitionSO jeremyScene;

    [SerializeField]
    private ExperienceSceneDefinitionSO juanAndresScene;

    [SerializeField]
    private ExperienceSceneDefinitionSO joaquinScene;

    [SerializeField]
    private ExperienceSceneDefinitionSO alexScene;

    [SerializeField]
    private ExperienceSceneDefinitionSO shipiScene;


    public ExperienceSceneDefinitionSO SelectedScene
    {
        get;
        private set;
    }


    public bool PlayFullSequence
    {
        get;
        private set;
    } = true;


    public void OnStartSceneChanged(int option)
    {
        Debug.Log(
            $"Dropdown changed. Option: {option}",
            this
        );
        
        switch (option)
        {
            case 0:
                SelectFullExperience();
                break;

            case 1:
                SelectScene(dannielScene);
                break;

            case 2:
                SelectScene(jeremyScene);
                break;

            case 3:
                SelectScene(juanAndresScene);
                break;

            case 4:
                SelectScene(joaquinScene);
                break;

            case 5:
                SelectScene(alexScene);
                break;

            case 6:
                SelectScene(shipiScene);
                break;

            default:
                Debug.LogError(
                    $"Opción de escena inválida: {option}",
                    this
                );
                break;
        }
    }


    private void SelectFullExperience()
    {
        SelectedScene = null;
        PlayFullSequence = true;

        Debug.Log(
            "Selected: Full Experience",
            this
        );
    }


    private void SelectScene(
        ExperienceSceneDefinitionSO scene
    )
    {
        if (scene == null)
        {
            Debug.LogError(
                "La escena seleccionada no está asignada.",
                this
            );

            return;
        }


        SelectedScene = scene;
        PlayFullSequence = false;

        Debug.Log(
            $"Selected scene: {scene.DisplayName}",
            this
        );
    }
}