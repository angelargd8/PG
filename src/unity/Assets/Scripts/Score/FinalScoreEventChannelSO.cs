using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "FinalScoreSubmitted",
    menuName = "Scriptable Objects/Events/Final Score Event Channel"
)]
public sealed class FinalScoreEventChannelSO : ScriptableObject
{
    public event Action<int> Raised;


    public void RaiseEvent(int score) => RaiseEventInternal(score, null);

    public void RaiseEvent(int score, UnityEngine.Object publisher) => RaiseEventInternal(score, publisher);

    private void RaiseEventInternal(int score, UnityEngine.Object publisher)
    {
		if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
		{
			Raised?.Invoke(score);
			return;
		}
		ExperienceDiagnostics.Publish(this, Raised, score, publisher, "final_score=" + score);
    }
}
