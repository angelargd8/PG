using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ScoreChanged",
    menuName = "Scriptable Objects/Events/Score Changed Event Channel"
)]
public sealed class ScoreChangedEventChannelSO : ScriptableObject
{
    public event Action<ScoreChange> Raised;


    public void RaiseEvent(ScoreChange scoreChange) => RaiseEventInternal(scoreChange, null);

    public void RaiseEvent(ScoreChange scoreChange, UnityEngine.Object publisher) => RaiseEventInternal(scoreChange, publisher);

    private void RaiseEventInternal(ScoreChange scoreChange, UnityEngine.Object publisher)
    {
		if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
		{
			Raised?.Invoke(scoreChange);
			return;
		}
		if (ExperienceDiagnostics.Level == ExperienceDiagnosticLevel.Flow)
		{
			ExperienceDiagnostics.RecordFrequentEvent("ScoreChanged", "published", publisher);
			Raised?.Invoke(scoreChange);
			return;
		}
		ExperienceDiagnostics.Publish(this, Raised, scoreChange,
			publisher, "score=" + scoreChange.CurrentScore + ";delta=" + scoreChange.Delta + ";judgement=" + scoreChange.TimingJudgement);
    }
}
