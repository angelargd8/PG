using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ScoreProfileChanged",
    menuName = "Scriptable Objects/Events/Score Profile Event Channel"
)]
public sealed class ScoreProfileEventChannelSO : ScriptableObject
{
    public event Action<ScoreProfileSO> Raised;


    public void RaiseEvent(ScoreProfileSO profile) => RaiseEventInternal(profile, null);

    public void RaiseEvent(ScoreProfileSO profile, UnityEngine.Object publisher) => RaiseEventInternal(profile, publisher);

    private void RaiseEventInternal(ScoreProfileSO profile, UnityEngine.Object publisher)
    {
		if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
		{
			Raised?.Invoke(profile);
			return;
		}
		ExperienceDiagnostics.Publish(this, Raised, profile, publisher,
			"profile=" + (profile != null ? profile.name : "none"));
    }
}
