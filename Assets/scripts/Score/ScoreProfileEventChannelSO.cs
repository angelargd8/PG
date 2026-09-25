using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ScoreProfileChanged",
    menuName = "Scriptable Objects/Events/Score Profile Event Channel"
)]
public sealed class ScoreProfileEventChannelSO : ScriptableObject
{
    public event Action<ScoreProfileSO> Raised;


    public void RaiseEvent(ScoreProfileSO profile)
    {
        Raised?.Invoke(profile);
    }
}