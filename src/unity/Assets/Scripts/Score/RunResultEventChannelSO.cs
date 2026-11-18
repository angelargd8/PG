using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RunResultReady",
    menuName = "Scriptable Objects/Events/Run Result Event Channel"
)]
public sealed class RunResultEventChannelSO : ScriptableObject
{
    public event Action<RunResult> Raised;


    public void RaiseEvent(RunResult result) => RaiseEventInternal(result, null);

    public void RaiseEvent(RunResult result, UnityEngine.Object publisher) => RaiseEventInternal(result, publisher);

    private void RaiseEventInternal(RunResult result, UnityEngine.Object publisher)
    {
		if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
		{
			Raised?.Invoke(result);
			return;
		}
		ExperienceDiagnostics.Publish(this, Raised, result, publisher,
			"final_score=" + result.FinalScore + ";high_score=" + result.HighScore + ";new_high_score=" + result.IsNewHighScore);
    }
}
