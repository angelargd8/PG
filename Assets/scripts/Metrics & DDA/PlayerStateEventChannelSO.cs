using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStateChanged",
    menuName = "Scriptable Objects/Events/Player State Event Channel"
)]
public sealed class PlayerStateEventChannelSO : ScriptableObject
{
    public event Action<PlayerState> Raised;


    public void RaiseEvent(PlayerState state)
    {
        Raised?.Invoke(state);
    }
}