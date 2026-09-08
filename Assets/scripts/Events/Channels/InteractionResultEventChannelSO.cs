using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "InteractionResultEventChannel",
    menuName = "Scriptable Objects/Events/Interaction Result Event Channel"
)]
public sealed class InteractionResultEventChannelSO : ScriptableObject
{
    public event Action<InteractionResult> Raised;


    public void RaiseEvent(InteractionResult result)
    {
        Raised?.Invoke(result);
    }
}