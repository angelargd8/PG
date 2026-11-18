using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BoolEventChannel",
    menuName = "Scriptable Objects/Events/Bool Event Channel"
)]
public sealed class BoolEventChannelSO : ScriptableObject
{
    public event Action<bool> Raised;

    public void RaiseEvent(bool value) => RaiseEventInternal(value, null);

    public void RaiseEvent(bool value, UnityEngine.Object publisher) => RaiseEventInternal(value, publisher);

    private void RaiseEventInternal(bool value, UnityEngine.Object publisher)
    {
        if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
        {
            Raised?.Invoke(value);
            return;
        }
        ExperienceDiagnostics.Publish(this, Raised, value, publisher, "value=" + value);
    }
}
