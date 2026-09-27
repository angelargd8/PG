using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ScoreBonusAwarded",
    menuName = "Scriptable Objects/Events/Score Bonus Event Channel"
)]
public sealed class ScoreBonusEventChannelSO : ScriptableObject
{
    public event Action<ScoreBonus> Raised;


    public void RaiseEvent(ScoreBonus bonus)
    {
        Raised?.Invoke(bonus);
    }
}