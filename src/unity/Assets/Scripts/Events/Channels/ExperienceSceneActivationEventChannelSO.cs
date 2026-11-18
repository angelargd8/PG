using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ExperienceSceneActivation", menuName = "Events/Experience Scene Activation")]
public sealed class ExperienceSceneActivationEventChannelSO : ScriptableObject
{
    public event Action<string, bool> Raised;
    public void RaiseEvent(string sceneName, bool active) => RaiseEventInternal(sceneName, active, null);

    public void RaiseEvent(string sceneName, bool active, UnityEngine.Object publisher) => RaiseEventInternal(sceneName, active, publisher);

    private void RaiseEventInternal(string sceneName, bool active, UnityEngine.Object publisher)
    {
        if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
        {
            Raised?.Invoke(sceneName, active);
            return;
        }
        ExperienceDiagnostics.Publish(this, Raised, sceneName, active, publisher,
            "scene=" + sceneName + ";active=" + active);
    }
}
