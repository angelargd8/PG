using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DifficultyChanged",
    menuName = "Scriptable Objects/Events/Difficulty Level Event Channel"
)]
public sealed class DifficultyLevelEventChannelSO : ScriptableObject
{
    public event Action<DifficultyLevel> Raised;

    public DifficultyLevel CurrentDifficulty { get; private set; } =
        DifficultyLevel.Normal;


    public void SetCurrentDifficulty(DifficultyLevel difficulty)
    {
        CurrentDifficulty = difficulty;
    }


    public void RaiseEvent(DifficultyLevel difficulty)
    {
        CurrentDifficulty = difficulty;
        Raised?.Invoke(difficulty);
    }
}