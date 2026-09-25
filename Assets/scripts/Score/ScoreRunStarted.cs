using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ScoreRunStarted",
    menuName = "Scriptable Objects/Events/Score Run Event Channel"
)]
public sealed class ScoreRunEventChannelSO : ScriptableObject
{
    public event Action<ScoreRunContext> Raised;


    public void RaiseEvent(ScoreRunContext context)
    {
        Raised?.Invoke(context);
    }
}