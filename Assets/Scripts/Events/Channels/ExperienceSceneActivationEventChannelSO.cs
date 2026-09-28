using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ExperienceSceneActivation", menuName = "Events/Experience Scene Activation")]
public sealed class ExperienceSceneActivationEventChannelSO : ScriptableObject
{
    public event Action<string, bool> Raised;
    public void RaiseEvent(string sceneName, bool active) => Raised?.Invoke(sceneName, active);
}
