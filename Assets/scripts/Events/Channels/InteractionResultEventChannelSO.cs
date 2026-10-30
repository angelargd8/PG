using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "InteractionResultEventChannel",
    menuName = "Scriptable Objects/Events/Interaction Result Event Channel"
)]
public sealed class InteractionResultEventChannelSO : ScriptableObject
{
    public event Action<InteractionResult> Raised;


    public void RaiseEvent(InteractionResult result) => RaiseEventInternal(result, null);

    public void RaiseEvent(InteractionResult result, UnityEngine.Object publisher) => RaiseEventInternal(result, publisher);

    private void RaiseEventInternal(InteractionResult result, UnityEngine.Object publisher)
    {
        if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
        {
            Raised?.Invoke(result);
            return;
        }
        if (ExperienceDiagnostics.Level == ExperienceDiagnosticLevel.Flow)
        {
            ExperienceDiagnostics.RecordFrequentEvent("InteractionResult", result.Outcome.ToString(), publisher);
            Raised?.Invoke(result);
            return;
        }
        ExperienceDiagnostics.Publish(this, Raised, result, publisher,
            "outcome=" + result.Outcome + ";type=" + result.InteractionType +
            ";minigame=" + result.MinigameId + ";timing_error_s=" +
            (result.TimingError.HasValue ? result.TimingError.Value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "unavailable"));
    }
}
