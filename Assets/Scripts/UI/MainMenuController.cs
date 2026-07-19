using UnityEngine;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Event Channels")]

    [SerializeField]
    private VoidEventChannelSO startExperienceRequested;



    public void RequestedStartExperience()
    {
        if (startExperienceRequested == null)
        {
            Debug.LogError("no se asigno StartExperienceRequested ", this);

            return;
        }

        startExperienceRequested.RaiseEvent();
    }

}
