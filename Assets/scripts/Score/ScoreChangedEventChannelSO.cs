using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ScoreChanged",
    menuName = "Scriptable Objects/Events/Score Changed Event Channel"
)]
public sealed class ScoreChangedEventChannelSO : ScriptableObject
{
    public event Action<ScoreChange> Raised;


    public void RaiseEvent(ScoreChange scoreChange)
    {
        Raised?.Invoke(scoreChange);
    }
}