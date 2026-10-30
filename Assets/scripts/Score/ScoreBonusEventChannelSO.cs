using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ScoreBonusAwarded",
    menuName = "Scriptable Objects/Events/Score Bonus Event Channel"
)]
public sealed class ScoreBonusEventChannelSO : ScriptableObject
{
    public event Action<ScoreBonus> Raised;


    public void RaiseEvent(ScoreBonus bonus) => RaiseEventInternal(bonus, null);

    public void RaiseEvent(ScoreBonus bonus, UnityEngine.Object publisher) => RaiseEventInternal(bonus, publisher);

    private void RaiseEventInternal(ScoreBonus bonus, UnityEngine.Object publisher)
    {
		if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
		{
			Raised?.Invoke(bonus);
			return;
		}
		ExperienceDiagnostics.Publish(this, Raised, bonus, publisher, "points=" + bonus.Points);
    }
}
