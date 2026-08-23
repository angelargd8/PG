using UnityEngine;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Event Channels")]

    [SerializeField]
    private ExperienceEventChannelSO ExperienceRequested;
    private ExperienceDefinitionSO experience;


    public void RequestedStartExperience()
    {
        // Aqui despues poner:
        // - Abrir panel principal
        // - Abrir configuración
        // - Abrir selección de escenas
        // - Cerrar paneles
    }

}
