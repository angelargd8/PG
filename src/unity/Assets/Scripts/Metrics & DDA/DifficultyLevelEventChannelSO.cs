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


    public void RaiseEvent(DifficultyLevel difficulty) => RaiseEventInternal(difficulty, null);

    public void RaiseEvent(DifficultyLevel difficulty, UnityEngine.Object publisher) => RaiseEventInternal(difficulty, publisher);

    private void RaiseEventInternal(DifficultyLevel difficulty, UnityEngine.Object publisher)
    {
        CurrentDifficulty = difficulty;
        if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
        {
            Raised?.Invoke(difficulty);
            return;
        }
        ExperienceDiagnostics.Publish(this, Raised, difficulty, publisher, "difficulty=" + difficulty);
    }
}
