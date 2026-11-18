using UnityEngine;

// Strategies own their presentation. The director only supplies musical time and readiness.
public abstract class ExperienceTransitionSO : ScriptableObject
{
    public abstract double LeadTime { get; }
    public abstract double RevealTime { get; }
    public abstract void Present(ExperienceTransitionView view, double timeFromBoundary, bool waitingForScene);
}
