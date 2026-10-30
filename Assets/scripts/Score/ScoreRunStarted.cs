using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ScoreRunStarted",
    menuName = "Scriptable Objects/Events/Score Run Event Channel"
)]
public sealed class ScoreRunEventChannelSO : ScriptableObject
{
    public event Action<ScoreRunContext> Raised;


    public void RaiseEvent(ScoreRunContext context) => RaiseEventInternal(context, null);

    public void RaiseEvent(ScoreRunContext context, UnityEngine.Object publisher) => RaiseEventInternal(context, publisher);

    private void RaiseEventInternal(ScoreRunContext context, UnityEngine.Object publisher)
    {
		if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
		{
			Raised?.Invoke(context);
			return;
		}
		ExperienceDiagnostics.Publish(this, Raised, context, publisher,
			"score_id=" + context.ScoreId + ";full=" + context.IsFullExperience);
    }
}
