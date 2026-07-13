using UnityEngine;
using System;

/* Basic Event Channel */

[CreateAssetMenu(
    fileName = "VoidEventChannelSO", 
    menuName = "Scriptable Objects/Void Event Channel")]

public sealed class VoidEventChannelSO : ScriptableObject
{
    public event Action Raised;

    public void Raise()
    {
        Raised?.Invoke();
    }
}
