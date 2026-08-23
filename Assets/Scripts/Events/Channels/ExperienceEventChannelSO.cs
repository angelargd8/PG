using UnityEngine;
using System;

[CreateAssetMenu(
    fileName = "ExperienceEventChannel",
    menuName = "Scriptable Objects/Events/Experience Event Channel"
)]
public sealed class ExperienceEventChannelSO : ScriptableObject
{
    public event Action<ExperienceRequest> Raised;


    public void RaiseEvent(
        ExperienceRequest request
    )
    {
        if (request.Experience == null)
        {
            Debug.LogError(
                "La solicitud no contiene una experiencia"
            );

            return;
        }


        Raised?.Invoke(
            request
        );
    }
}