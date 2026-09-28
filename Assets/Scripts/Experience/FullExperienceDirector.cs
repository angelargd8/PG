using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(ExperienceTransitionView))]
public sealed class FullExperienceDirector : MonoBehaviour
{
    [SerializeField] private VoidEventChannelSO experienceReady;
    [SerializeField] private VoidEventChannelSO experienceCompleted;
    [SerializeField] private VoidEventChannelSO mainMenuRequested;
    [SerializeField] private ExperienceSceneActivationEventChannelSO sceneActivation;

    private readonly Dictionary<string, ExperienceSceneBootstrap> loaded = new();
    private ExperienceSequenceSO sequence;
    private ExperienceMusicClock clock;
    private ExperienceTransitionView view;
    private bool running, stopping, busy;
    private int activeIndex = -1;
    private double revealedAt;
    public bool IsPrepared { get; private set; }
    public string Error { get; private set; }
    public int ActiveSegmentIndex => activeIndex;
    public int LoadedSceneCount => loaded.Count;

    private void OnEnable()
    {
        view = GetComponent<ExperienceTransitionView>();
        if (experienceReady != null) experienceReady.Raised += HandleReady;
        if (experienceCompleted != null) experienceCompleted.Raised += StopPlayback;
        if (mainMenuRequested != null) mainMenuRequested.Raised += StopPlayback;
    }

    private void OnDisable()
    {
        if (experienceReady != null) experienceReady.Raised -= HandleReady;
        if (experienceCompleted != null) experienceCompleted.Raised -= StopPlayback;
        if (mainMenuRequested != null) mainMenuRequested.Raised -= StopPlayback;
        StopPlayback();
    }

    public IEnumerator Prepare(ExperienceSequenceSO configuration)
    {
        sequence = configuration;
        stopping = false;
        Error = null;
        IsPrepared = false;
        activeIndex = -1;
        if (sequence == null || !sequence.Validate(out _))
        { Fail("Full Experience necesita una secuencia valida."); yield break; }
        if (sceneActivation == null || experienceReady == null)
        { Fail("Full Experience necesita sus Event Channels."); yield break; }
        clock = FindFirstObjectByType<ExperienceMusicClock>();
        if (clock == null) { Fail("Falta ExperienceMusicClock en ExperienceCore."); yield break; }
        for (int i = 0; i < sequence.Count; i++)
        {
            if (i >= 2 && !sequence.GetSegment(i).PreloadBeforePlayback) continue;
            yield return LoadSegment(i);
            if (Error != null) yield break;
        }
        IsPrepared = Error == null;
    }

    private void HandleReady()
    {
        if (IsPrepared && !stopping) running = true;
        // Activation happens in Update, after all global ExperienceReady listeners (score/DDA/music).
    }

    private void Update()
    {
        if (!running || clock == null || !clock.IsPlaying || Time.timeScale <= 0 || AudioListener.pause) return;
        double songTime = clock.SongTime;
        int desired = sequence.FindSegment(songTime);
        if (desired != activeIndex)
        {
            DeactivateCurrent();
            if (desired >= 0)
            {
                string name = sequence.GetSegment(desired).Scene.SceneName;
                if (loaded.TryGetValue(name, out ExperienceSceneBootstrap bootstrap) && bootstrap.IsPrepared)
                {
                    SceneManager.SetActiveScene(bootstrap.gameObject.scene);
                    sceneActivation.RaiseEvent(name, true);
                    if (!bootstrap.IsRunning) { Fail($"La escena {name} no recibio su activacion."); return; }
                    activeIndex = desired;
                    revealedAt = songTime;
                }
            }
        }
        PresentTransition(desired, songTime);
        if (!busy && NeedsMaintenance(desired, songTime))
        {
            busy = true;
            StartCoroutine(MaintainScenes(desired, songTime));
        }
    }

    private void PresentTransition(int desired, double songTime)
    {
        if (desired < 0) { view.Clear(); return; }
        var segment = sequence.GetSegment(desired);
        bool waiting = activeIndex != desired;
        if (waiting)
        {
            if (segment.Transition != null) segment.Transition.Present(view, 0, true);
            else view.Clear();
            return;
        }
        // Fade out anticipates the boundary. Gameplay switches at the boundary itself.
        var next = sequence.GetSegment(desired + 1);
        if (next?.Transition != null && songTime >= next.StartTime - next.Transition.LeadTime)
        {
            next.Transition.Present(view, songTime - next.StartTime, false);
            return;
        }
        // A late load gets a complete reveal, measured with the same musical clock.
        if (desired > 0 && segment.Transition != null && songTime < revealedAt + segment.Transition.RevealTime)
            segment.Transition.Present(view, songTime - revealedAt, false);
        else view.Clear();
    }

    private bool NeedsMaintenance(int desired, double songTime)
    {
        string current = sequence.GetSegment(desired)?.Scene.SceneName;
        var next = desired >= 0 ? sequence.GetSegment(desired + 1) : null;
        foreach (string name in loaded.Keys)
            if (name != current && name != next?.Scene.SceneName && !sequence.RetainStartupPreload(name, songTime)) return true;
        return current != null && !loaded.ContainsKey(current) ||
            next != null && songTime >= next.StartTime - sequence.PreloadLeadSeconds && !loaded.ContainsKey(next.Scene.SceneName);
    }

    private IEnumerator MaintainScenes(int desired, double songTime)
    {
        try
        {
            string current = sequence.GetSegment(desired)?.Scene.SceneName;
            var nextSegment = sequence.GetSegment(desired + 1);
            string next = desired >= 0 ? nextSegment?.Scene.SceneName : null;
            // Keep explicitly requested startup preloads until their segment has ended.
            // Other scenes still use the current + next rolling window.
            var obsolete = new List<string>();
            foreach (string name in loaded.Keys)
                if (name != current && name != next && !sequence.RetainStartupPreload(name, songTime)) obsolete.Add(name);
            foreach (string name in obsolete)
            {
                yield return SceneFlowManager.UnloadIfLoaded(name);
                loaded.Remove(name);
            }
            if (obsolete.Count > 0 && sequence.ReleaseUnusedAssets)
                yield return Resources.UnloadUnusedAssets();
            if (stopping || desired < 0) yield break;
            if (!loaded.ContainsKey(current)) yield return LoadSegment(desired);
            if (stopping || Error != null) yield break;
            if (next != null && !loaded.ContainsKey(next) && songTime >= nextSegment.StartTime - sequence.PreloadLeadSeconds)
                yield return LoadSegment(desired + 1);
        }
        finally { busy = false; }
    }

    private IEnumerator LoadSegment(int index)
    {
        string name = sequence.GetSegment(index).Scene.SceneName;
        if (loaded.ContainsKey(name)) yield break;
        yield return SceneFlowManager.LoadAdditive(name);
        Scene scene = SceneManager.GetSceneByName(name);
        if (!scene.IsValid() || !scene.isLoaded) { Fail($"No se pudo cargar {name}."); yield break; }
        ExperienceSceneBootstrap bootstrap = SceneFlowManager.FindExperienceBootstrap(scene);
        // Track even malformed scenes so cleanup still unloads them.
        loaded.Add(name, bootstrap);
        if (bootstrap == null || !bootstrap.SupportsStagedActivation)
        { Fail($"{name} necesita bootstrap, Gameplay Root inactivo y Scene Activation."); yield break; }
        bootstrap.SetExternallyControlled(true);
        if (stopping) yield break;
        yield return ExperiencePreloadOperation.Run(bootstrap.Prepare(), exception => Fail($"{name}: {exception.Message}"));
        if (!bootstrap.IsPrepared) Fail($"No se pudo preparar {name}.");
    }

    private void DeactivateCurrent()
    {
        if (activeIndex < 0) return;
        string name = sequence.GetSegment(activeIndex).Scene.SceneName;
        activeIndex = -1;
        sceneActivation.RaiseEvent(name, false);
    }

    private void StopPlayback()
    {
        running = false;
        stopping = true;
        DeactivateCurrent();
        if (view != null) view.Clear();
    }

    public IEnumerator Shutdown()
    {
        StopPlayback();
        // Unity scene loads cannot be cancelled. Drain the in-flight operation before unloading.
        while (busy) yield return null;
        foreach (string name in new List<string>(loaded.Keys))
            yield return SceneFlowManager.UnloadIfLoaded(name);
        loaded.Clear();
        IsPrepared = false;
        sequence = null;
        clock = null;
    }

    private void Fail(string message)
    {
        Error = message;
        bool wasRunning = running;
        StopPlayback();
        Debug.LogError($"[FullExperienceDirector] {message}", this);
        if (wasRunning && mainMenuRequested != null) mainMenuRequested.RaiseEvent();
    }
}
