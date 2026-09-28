using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class XRControllerModelVisibility : MonoBehaviour
{
    [SerializeField] private Transform[] controllerVisuals;
    [SerializeField] private BoolEventChannelSO gameplayPauseChanged;
    [SerializeField] private string mainMenuScene = "MainMenu";
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<bool> originalForceOff = new List<bool>();

    private void Awake()
    {
        if (controllerVisuals == null) return;
        foreach (Transform root in controllerVisuals)
        {
            if (root == null) continue;
            foreach (Renderer item in root.GetComponentsInChildren<Renderer>(true))
            {
                renderers.Add(item);
                originalForceOff.Add(item.forceRenderingOff);
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        if (gameplayPauseChanged != null) gameplayPauseChanged.Raised += HandlePauseChanged;
        Refresh();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        if (gameplayPauseChanged != null) gameplayPauseChanged.Raised -= HandlePauseChanged;
        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i] != null) renderers[i].forceRenderingOff = originalForceOff[i];
    }

    private void HandleActiveSceneChanged(Scene previous, Scene current) => Refresh();
    private void HandlePauseChanged(bool paused) => Refresh();

    private void Refresh()
    {
        var pauseMenu = FindFirstObjectByType<PauseMenuController>();
        bool visible = SceneManager.GetActiveScene().name == mainMenuScene ||
            (pauseMenu != null && pauseMenu.IsPaused);
        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i] != null)
                renderers[i].forceRenderingOff = !visible || originalForceOff[i];
    }
}
