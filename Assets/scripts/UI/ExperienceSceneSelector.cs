
using System;
using TMPro;
using UnityEngine;

public sealed class ExperienceSceneSelector : MonoBehaviour
{
    [Header("Experience Scenes")]
    [SerializeField] private ExperienceSceneDefinitionSO dannielScene;
    [SerializeField] private ExperienceSceneDefinitionSO jeremyScene;
    [SerializeField] private ExperienceSceneDefinitionSO juanAndresScene;
    [SerializeField] private ExperienceSceneDefinitionSO joaquinScene;
    [SerializeField] private ExperienceSceneDefinitionSO alexScene;
    [SerializeField] private ExperienceSceneDefinitionSO shipiScene;

    [Header("UI")]
    [SerializeField] private TMP_Dropdown _sceneDropdown;

    private static int _lastSelectedOption = 0;

    public event Action SelectionChanged;

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

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSavedSelection()
    {
        _lastSelectedOption = 0;
    }

    private void Awake()
    {
        OnStartSceneChanged(_lastSelectedOption);
    }

    private void Start()
    {
        if (_sceneDropdown == null)
        {
            Debug.LogError(
                "Scene Dropdown no está asignado.",
                this
            );
            return;
        }

        _sceneDropdown.SetValueWithoutNotify(
            _lastSelectedOption
        );

        _sceneDropdown.RefreshShownValue();
    }

    public void OnStartSceneChanged(int option)
    {
        switch (option)
        {
            case 0:
                SelectFullExperience();
                break;

            case 1:
                SelectScene(dannielScene, option);
                break;

            case 2:
                SelectScene(jeremyScene, option);
                break;

            case 3:
                SelectScene(juanAndresScene, option);
                break;

            case 4:
                SelectScene(joaquinScene, option);
                break;

            case 5:
                SelectScene(alexScene, option);
                break;

            case 6:
                SelectScene(shipiScene, option);
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

        _lastSelectedOption = 0;

        Debug.Log("Selected: Full Experience", this);

        SelectionChanged?.Invoke();
    }

    private void SelectScene(
        ExperienceSceneDefinitionSO scene,
        int option)
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

        // Save selection for the next MainMenu instance.
        _lastSelectedOption = option;

        Debug.Log(
            $"Selected scene: {scene.DisplayName}",
            this
        );

        SelectionChanged?.Invoke();
    }
}
