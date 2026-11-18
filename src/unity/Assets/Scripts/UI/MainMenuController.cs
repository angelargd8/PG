using UnityEngine;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Panels")]

    [SerializeField]
    private GameObject mainPanel;

    [SerializeField]
    private GameObject startScenePanel;


    public void OpenStartScenePanel()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (startScenePanel != null)
        {
            startScenePanel.SetActive(true);
        }
    }


    public void CloseStartScenePanel()
    {
        if (startScenePanel != null)
        {
            startScenePanel.SetActive(false);
        }

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
    }
}