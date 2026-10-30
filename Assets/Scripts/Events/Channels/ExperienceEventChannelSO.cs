using UnityEngine;
using System;

[CreateAssetMenu(
    fileName = "ExperienceEventChannel",
    menuName = "Scriptable Objects/Events/Experience Event Channel"
)]
public sealed class ExperienceEventChannelSO : ScriptableObject
{
    public event Action<ExperienceRequest> Raised;


    public void RaiseEvent(ExperienceRequest request) => RaiseEventInternal(request, null);

    public void RaiseEvent(ExperienceRequest request, UnityEngine.Object publisher) => RaiseEventInternal(request, publisher);

    private void RaiseEventInternal(ExperienceRequest request, UnityEngine.Object publisher)
    {
        if (request.Experience == null)
        {
            Debug.LogError(
                "La solicitud no contiene una experiencia"
            );

            return;
        }


        if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
        {
            Raised?.Invoke(request);
            return;
        }
        ExperienceDiagnostics.Publish(this, Raised, request, publisher,
            "experience=" + request.Experience.name + ";start_index=" + request.StartSceneIndex + ";mode=" + (request.PlayFullSequence ? "full" : "individual"));
    }
}
