using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class FullProbeRuntime : MonoBehaviour, IExperienceRuntime, IExperiencePreloadable
{
    public sealed class Counts { public int Awake, Start, Update, Preload, Begin, End; }
    public static readonly Dictionary<string, Counts> Scenes = new();
    public static int Running, MaxRunning;
    public static bool ThrowOnPrepare;
    private Counts Counter
    {
        get
        {
            if (!Scenes.TryGetValue(gameObject.scene.name, out var counter))
                Scenes.Add(gameObject.scene.name, counter = new Counts());
            return counter;
        }
    }
    private void Awake() => Counter.Awake++;
    private void Start() => Counter.Start++;
    private void Update() => Counter.Update++;
    public IEnumerator Preload()
    {
        if (ThrowOnPrepare) throw new System.InvalidOperationException("Injected preload failure");
        Counter.Preload++;
        yield return null;
        yield return null;
    }
    public void BeginExperience() { Counter.Begin++; Running++; MaxRunning = Mathf.Max(MaxRunning, Running); }
    public void EndExperience() { Counter.End++; Running--; }
}
