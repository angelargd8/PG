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
    private string transitionPhase = "none";
    private int transitionSegmentIndex = -1;
    private ExperienceTransitionSO activeTransition;
    public bool IsPrepared { get; private set; }
    public string Error { get; private set; }
    public int ActiveSegmentIndex => activeIndex;
    public int LoadedSceneCount => loaded.Count;

    private void OnEnable()
    {
        view = GetComponent<ExperienceTransitionView>();
        if (experienceReady != null)
        {
            experienceReady.Raised += HandleReady;
            ExperienceDiagnostics.LogSubscription("added", this, experienceReady);
        }
        if (experienceCompleted != null) { experienceCompleted.Raised += StopPlayback; ExperienceDiagnostics.LogSubscription("added", this, experienceCompleted); }
        if (mainMenuRequested != null) { mainMenuRequested.Raised += StopPlayback; ExperienceDiagnostics.LogSubscription("added", this, mainMenuRequested); }
    }

    private void OnDisable()
    {
        if (experienceReady != null) { experienceReady.Raised -= HandleReady; ExperienceDiagnostics.LogSubscription("removed", this, experienceReady); }
        if (experienceCompleted != null) { experienceCompleted.Raised -= StopPlayback; ExperienceDiagnostics.LogSubscription("removed", this, experienceCompleted); }
        if (mainMenuRequested != null) { mainMenuRequested.Raised -= StopPlayback; ExperienceDiagnostics.LogSubscription("removed", this, mainMenuRequested); }
        StopPlayback();
    }

    public IEnumerator Prepare(ExperienceSequenceSO configuration)
    {
        var operation = ExperienceDiagnostics.BeginOperation("full_sequence_prepare", this,
            "segment_count=" + (configuration != null ? configuration.Count : 0));
        bool prepared = false;
        sequence = configuration;
        stopping = false;
        Error = null;
        IsPrepared = false;
        activeIndex = -1;
        try
        {
            if (sequence == null || !sequence.Validate(out _))
            { Fail("Full Experience necesita una secuencia valida."); yield break; }
            if (sceneActivation == null || experienceReady == null)
            { Fail("Full Experience necesita sus Event Channels."); yield break; }
            clock = FindFirstObjectByType<ExperienceMusicClock>();
            if (clock == null) { Fail("Falta ExperienceMusicClock en ExperienceCore."); yield break; }
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "CONFIG", "sequence_configuration", this, operation.OperationId,
                "sequence=" + ExperienceDiagnostics.Field("name", sequence.name) +
                " segment_count=" + sequence.Count +
                " preload_lead_seconds=" + sequence.PreloadLeadSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                " release_unused_assets=" + sequence.ReleaseUnusedAssets +
                " hold_last_scene_until_song_ends=" + sequence.HoldLastSceneUntilSongEnds);
            for (int i = 0; i < sequence.Count; i++)
            {
                var configuredSegment = sequence.GetSegment(i);
                ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "CONFIG", "segment_configuration", this, operation.OperationId,
                    "segment_index=" + i + " scene=" + ExperienceDiagnostics.Field("name", configuredSegment.Scene.SceneName) +
                    " start_time_s=" + configuredSegment.StartTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    " end_time_s=" + configuredSegment.EndTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    " preload_before_playback=" + configuredSegment.PreloadBeforePlayback +
                    " transition=" + ExperienceDiagnostics.Field("name", configuredSegment.Transition != null ? configuredSegment.Transition.name : "none") +
                    " transition_lead_s=" + (configuredSegment.Transition != null ? configuredSegment.Transition.LeadTime : 0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    " transition_reveal_s=" + (configuredSegment.Transition != null ? configuredSegment.Transition.RevealTime : 0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
                if (i >= 2 && !sequence.GetSegment(i).PreloadBeforePlayback) continue;
                ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "startup_segment_preload_selected", this, operation.OperationId,
                    "segment_index=" + i + " scene=" + ExperienceDiagnostics.Field("name", sequence.GetSegment(i).Scene.SceneName) +
                    " start_time_s=" + sequence.GetSegment(i).StartTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    " end_time_s=" + sequence.GetSegment(i).EndTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    " preload_before_playback=" + sequence.GetSegment(i).PreloadBeforePlayback);
                yield return LoadSegment(i);
                if (Error != null) yield break;
            }
            IsPrepared = Error == null;
            prepared = IsPrepared;
        }
        finally
        {
            ExperienceDiagnostics.EndOperation(operation, prepared ? "prepared" : Error != null ? "error" : "interrupted",
                "loaded_scene_count=" + loaded.Count + " error=" + ExperienceDiagnostics.Field("message", Error ?? "none"));
        }
    }

    private void HandleReady()
    {
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "full_experience_ready_received", this, null,
            "is_prepared=" + IsPrepared + " stopping=" + stopping);
        if (IsPrepared && !stopping)
        {
            running = true;
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "segment_evaluation_started", this, null,
                "reason=experience_ready");
        }
        else ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "segment_evaluation_not_started", this, null,
            "reason=" + (!IsPrepared ? "not_prepared" : "stopping"));
        // Activation happens in Update, after all global ExperienceReady listeners (score/DDA/music).
    }

    private void Update()
    {
        if (!running || clock == null || !clock.IsPlaying || Time.timeScale <= 0 || AudioListener.pause) return;
        double songTime = clock.SongTime;
        int desired = sequence.FindSegment(songTime);
        if (desired != activeIndex)
        {
            string previousName = activeIndex >= 0 ? sequence.GetSegment(activeIndex).Scene.SceneName : "none";
            string nextName = desired >= 0 ? sequence.GetSegment(desired).Scene.SceneName : "none";
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "segment_selected", this, null,
                "previous_index=" + activeIndex + " next_index=" + desired +
                " previous_scene=" + ExperienceDiagnostics.Field("name", previousName) +
                " next_scene=" + ExperienceDiagnostics.Field("name", nextName) +
                " music_time_s=" + songTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            DeactivateCurrent();
            if (desired >= 0)
            {
                string name = sequence.GetSegment(desired).Scene.SceneName;
                if (loaded.TryGetValue(name, out ExperienceSceneBootstrap bootstrap) && bootstrap.IsPrepared)
                {
                    bool sceneSetActive = SceneManager.SetActiveScene(bootstrap.gameObject.scene);
                    ExperienceDiagnostics.SetSegmentIndex(desired);
                    ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "gameplay_scene_set_active", this, null,
                        "segment_index=" + desired + " scene=" + ExperienceDiagnostics.Field("name", name) + " result=" + sceneSetActive);
                    sceneActivation.RaiseEvent(name, true, this);
                    if (!bootstrap.IsRunning) { Fail($"La escena {name} no recibio su activacion."); return; }
                    activeIndex = desired;
                    ExperienceDiagnostics.SetSegmentIndex(activeIndex);
                    revealedAt = songTime;
                    ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "segment_active", this, null,
                        "segment_index=" + activeIndex + " scene=" + ExperienceDiagnostics.Field("name", name) +
                        " music_time_s=" + songTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
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
        if (desired < 0) { SetTransitionPhase("none", -1, null, songTime); view.Clear(); return; }
        var segment = sequence.GetSegment(desired);
        bool waiting = activeIndex != desired;
        if (waiting)
        {
            SetTransitionPhase(segment.Transition != null ? "waiting_for_scene" : "none", desired, segment.Transition, songTime);
            if (segment.Transition != null) segment.Transition.Present(view, 0, true);
            else view.Clear();
            return;
        }
        // Fade out anticipates the boundary. Gameplay switches at the boundary itself.
        var next = sequence.GetSegment(desired + 1);
        if (next?.Transition != null && songTime >= next.StartTime - next.Transition.LeadTime)
        {
            SetTransitionPhase("outgoing", desired + 1, next.Transition, songTime);
            next.Transition.Present(view, songTime - next.StartTime, false);
            return;
        }
        // A late load gets a complete reveal, measured with the same musical clock.
        if (desired > 0 && segment.Transition != null && songTime < revealedAt + segment.Transition.RevealTime)
        {
            SetTransitionPhase("incoming", desired, segment.Transition, songTime);
            segment.Transition.Present(view, songTime - revealedAt, false);
        }
        else { SetTransitionPhase("none", -1, null, songTime); view.Clear(); }
    }

    private void SetTransitionPhase(string nextPhase, int segmentIndex, ExperienceTransitionSO transition, double songTime)
    {
        if (transitionPhase == nextPhase) return;
        string previous = transitionPhase;
        int previousSegmentIndex = transitionSegmentIndex;
        ExperienceTransitionSO previousTransition = activeTransition;
        transitionPhase = nextPhase;
        if (previous != "none")
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "TRANSITION", "transition_phase_finished", this, null,
                "phase=" + previous + " segment_index=" + previousSegmentIndex +
                " lead_time_s=" + (previousTransition != null ? previousTransition.LeadTime : 0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                " reveal_time_s=" + (previousTransition != null ? previousTransition.RevealTime : 0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                " music_time_s=" + songTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        transitionSegmentIndex = nextPhase == "none" ? -1 : segmentIndex;
        activeTransition = nextPhase == "none" ? null : transition;
        if (nextPhase != "none")
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "TRANSITION", "transition_phase_started", this, null,
                "phase=" + nextPhase + " segment_index=" + segmentIndex +
                " lead_time_s=" + (transition != null ? transition.LeadTime : 0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                " reveal_time_s=" + (transition != null ? transition.RevealTime : 0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                " music_time_s=" + songTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
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
        var operation = ExperienceDiagnostics.BeginOperation("maintain_scene_window", this,
            "desired_segment=" + desired + " music_time_s=" + songTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        string result = "completed";
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
                yield return SceneFlowManager.UnloadIfLoaded(name, this);
                loaded.Remove(name);
            }
            if (obsolete.Count > 0 && sequence.ReleaseUnusedAssets)
            {
                var assetOperation = ExperienceDiagnostics.BeginOperation("release_unused_assets", this,
                    "after_scene_unloads=" + obsolete.Count);
                bool released = false;
                try
                {
                    yield return Resources.UnloadUnusedAssets();
                    released = true;
                }
                finally
                {
                    ExperienceDiagnostics.EndOperation(assetOperation, released ? "completed" : "interrupted");
                }
            }
            if (stopping || desired < 0) { result = "cancelled_or_no_active_segment"; yield break; }
            if (!loaded.ContainsKey(current)) yield return LoadSegment(desired);
            if (stopping || Error != null) { result = Error != null ? "error" : "cancelled"; yield break; }
            if (next != null && !loaded.ContainsKey(next) && songTime >= nextSegment.StartTime - sequence.PreloadLeadSeconds)
            {
                ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "rolling_preload_started", this, operation.OperationId,
                    "segment_index=" + (desired + 1) + " scene=" + ExperienceDiagnostics.Field("name", next) +
                    " preload_lead_s=" + sequence.PreloadLeadSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    " time_to_boundary_s=" + (nextSegment.StartTime - songTime).ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
                yield return LoadSegment(desired + 1);
            }
        }
        finally
        {
            busy = false;
            ExperienceDiagnostics.EndOperation(operation, result,
                "loaded_scene_count=" + loaded.Count + " stopping=" + stopping);
        }
    }

    private IEnumerator LoadSegment(int index)
    {
        string name = sequence.GetSegment(index).Scene.SceneName;
        if (loaded.ContainsKey(name))
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "segment_load_skipped", this, null,
                "segment_index=" + index + " scene=" + ExperienceDiagnostics.Field("name", name) + " reason=already_loaded");
            yield break;
        }
        yield return SceneFlowManager.LoadAdditive(name, this);
        Scene scene = SceneManager.GetSceneByName(name);
        if (!scene.IsValid() || !scene.isLoaded) { ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "segment_scene_load_failed", this, null, "segment_index=" + index + " scene=" + ExperienceDiagnostics.Field("name", name)); Fail($"No se pudo cargar {name}."); yield break; }
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "segment_scene_loaded", this, null,
            "segment_index=" + index + " scene=" + ExperienceDiagnostics.Field("name", name) + " prepared=false active=false");
        ExperienceSceneBootstrap bootstrap = SceneFlowManager.FindExperienceBootstrap(scene);
        // Track even malformed scenes so cleanup still unloads them.
        loaded.Add(name, bootstrap);
        if (bootstrap == null || !bootstrap.SupportsStagedActivation)
        { Fail($"{name} necesita bootstrap, Gameplay Root inactivo y Scene Activation."); yield break; }
        bootstrap.SetExternallyControlled(true);
        if (stopping) yield break;
        yield return ExperiencePreloadOperation.Run(bootstrap.Prepare(), exception => Fail($"{name}: {exception}"));
        if (!bootstrap.IsPrepared) Fail($"No se pudo preparar {name}.");
        else ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "segment_scene_prepared", bootstrap, null,
            "segment_index=" + index + " scene=" + ExperienceDiagnostics.Field("name", name) + " prepared=true active=false");
    }

    private void DeactivateCurrent()
    {
        if (activeIndex < 0) return;
        string name = sequence.GetSegment(activeIndex).Scene.SceneName;
        int previousIndex = activeIndex;
        activeIndex = -1;
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "segment_deactivation_requested", this, null,
            "segment_index=" + previousIndex + " scene=" + ExperienceDiagnostics.Field("name", name));
        sceneActivation.RaiseEvent(name, false, this);
        ExperienceDiagnostics.SetSegmentIndex(-1);
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
            yield return SceneFlowManager.UnloadIfLoaded(name, this);
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
        if (wasRunning && mainMenuRequested != null) mainMenuRequested.RaiseEvent(this);
    }
}
