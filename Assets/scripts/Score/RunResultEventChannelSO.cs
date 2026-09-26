using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RunResultReady",
    menuName = "Scriptable Objects/Events/Run Result Event Channel"
)]
public sealed class RunResultEventChannelSO : ScriptableObject
{
    public event Action<RunResult> Raised;


    public void RaiseEvent(RunResult result)
    {
        Raised?.Invoke(result);
    }
}