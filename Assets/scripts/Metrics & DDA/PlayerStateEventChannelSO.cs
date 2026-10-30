using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStateChanged",
    menuName = "Scriptable Objects/Events/Player State Event Channel"
)]
public sealed class PlayerStateEventChannelSO : ScriptableObject
{
    public event Action<PlayerState> Raised;


    public void RaiseEvent(PlayerState state) => RaiseEventInternal(state, null);

    public void RaiseEvent(PlayerState state, UnityEngine.Object publisher) => RaiseEventInternal(state, publisher);

    private void RaiseEventInternal(PlayerState state, UnityEngine.Object publisher)
    {
		if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
		{
			Raised?.Invoke(state);
			return;
		}
		ExperienceDiagnostics.Publish(this, Raised, state, publisher, "player_state=" + state);
    }
}
