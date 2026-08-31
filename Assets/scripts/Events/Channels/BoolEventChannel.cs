using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BoolEventChannel",
    menuName = "Scriptable Objects/Events/Bool Event Channel"
)]
public sealed class BoolEventChannelSO : ScriptableObject
{
    public event Action<bool> Raised;

    public void RaiseEvent(bool value)
    {
        Raised?.Invoke(value);
    }
}