using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "FinalScoreSubmitted",
    menuName = "Scriptable Objects/Events/Final Score Event Channel"
)]
public sealed class FinalScoreEventChannelSO : ScriptableObject
{
    public event Action<int> Raised;


    public void RaiseEvent(int score)
    {
        Raised?.Invoke(score);
    }
}