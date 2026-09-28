#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public sealed class FullExperienceProbeRunner : MonoBehaviour
{
    private int checks, readyCount, plays, completions;
    private AppStateMachine app;
    private FullExperienceDirector full;
    private PlayableDirector music;
    private ExperienceDefinitionSO experience;
    private VoidEventChannelSO ready, menu, completed;
    private ExperienceEventChannelSO requested;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FULLPROBE: " + message);
        checks++;
    }
    private void Start() => StartCoroutine(ExperiencePreloadOperation.Run(Run(), error =>
    {
        Debug.LogException(error); EditorApplication.Exit(1);
    }));
    private IEnumerator Until(Func<bool> condition, string label)
    {
        float start = Time.realtimeSinceStartup;
        while (!condition())
        {
            if (Time.realtimeSinceStartup - start > 25) throw new Exception("Timeout: " + label);
            Check(full == null || full.LoadedSceneCount <= 2, "max two resident gameplay scenes");
            yield return null;
        }
    }
    private void Seek(double time) { music.time = time; music.Evaluate(); }

    private IEnumerator Run()
    {
        app = FindFirstObjectByType<AppStateMachine>();
        full = FindFirstObjectByType<FullExperienceDirector>();
        experience = AssetDatabase.LoadAssetAtPath<ExperienceDefinitionSO>("Assets/Experience.asset");
        ready = AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>("Assets/Ready.asset");
        menu = AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>("Assets/MenuRequested.asset");
        completed = AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>("Assets/Completed.asset");
        requested = AssetDatabase.LoadAssetAtPath<ExperienceEventChannelSO>("Assets/Requested.asset");
        ready.Raised += () => readyCount++;
        completed.Raised += () => completions++;
        SceneManager.sceneLoaded += TrackMusic;
        var sequence = experience.FullSequence;
        Check(sequence.Validate(out _), "valid sequence");
        foreach (var sample in new[] { (0d, 0), (30.999, 0), (31d, 1), (124.999, 1), (125d, 2), (155.999, 2), (156d, 3), (187d, 3) })
            Check(sequence.FindSegment(sample.Item1) == sample.Item2, "half-open boundary " + sample.Item1);
        double original = sequence.GetSegment(1).StartTime;
        sequence.GetSegment(1).StartTime = 32;
        Check(!sequence.Validate(out _), "reject gap");
        sequence.GetSegment(1).StartTime = original;

        yield return Until(() => app.CurrentState == AppState.MainMenu, "initial menu");
        requested.RaiseEvent(new ExperienceRequest(experience, 0, true));
        yield return Until(() => full.ActiveSegmentIndex == 0, "Alex activation");
        Check(readyCount == 1 && plays == 1, "one Ready and one Play");
        Check(FullProbeRuntime.Scenes["Alex"].Begin == 1, "Alex began once");
        Check(FullProbeRuntime.Scenes["Joaquin"].Preload == 1, "next prepared before music");
        Check(FullProbeRuntime.Scenes["Joaquin"].Awake == 0 && FullProbeRuntime.Scenes["Joaquin"].Start == 0 && FullProbeRuntime.Scenes["Joaquin"].Update == 0, "preload never runs Awake/Start/Update");
        double before = music.time;
        for (int i = 0; i < 5; i++) yield return null;
        Check(music.time > before, "real song clock advances normally");
        music.timeUpdateMode = DirectorUpdateMode.Manual;
        Seek(30.8); yield return null; yield return null;
        Check(full.ActiveSegmentIndex == 0, "fade lead does not activate early");
        Seek(31); yield return null; yield return null;
        Check(full.ActiveSegmentIndex == 1, "31 activates Joaquin");
        Check(FullProbeRuntime.Scenes["Alex"].End == 1, "Alex ended once");
        yield return Until(() => !SceneManager.GetSceneByName("Alex").isLoaded, "unload old Alex");
        Check(plays == 1 && readyCount == 1 && music.time == 31, "music/Ready unchanged after transition");
        Time.timeScale = 0; AudioListener.pause = true;
        Seek(125); yield return null; yield return null;
        Check(full.ActiveSegmentIndex == 1, "pause does not activate next scene");
        Time.timeScale = 1; AudioListener.pause = false;
        yield return Until(() => full.ActiveSegmentIndex == 2, "late preload targets current song segment");
        Check(music.time == 125 && plays == 1, "late load never rewinds song");
        Seek(156);
        yield return Until(() => full.ActiveSegmentIndex == 3, "Jeremy activation");
        Check(FullProbeRuntime.MaxRunning == 1, "never two gameplay runtimes active");
        Check(readyCount == 1 && plays == 1, "all four segments share one playback");
        Seek(31);
        yield return Until(() => full.ActiveSegmentIndex == 1, "backward seek selects configuration");
        Seek(186);
        yield return Until(() => full.ActiveSegmentIndex == 3, "forward seek skips obsolete segments");
        Seek(187); yield return null; yield return null;
        Check(completions == 1 && FullProbeRuntime.Running == 0, "song end completes once and deactivates gameplay");
        menu.RaiseEvent();
        yield return Until(() => app.CurrentState == AppState.MainMenu, "return menu");
        Check(full.LoadedSceneCount == 0 && !SceneManager.GetSceneByName("ExperienceCore").isLoaded, "full cleanup");

        requested.RaiseEvent(new ExperienceRequest(experience, 1, false));
        yield return Until(() => app.CurrentState == AppState.Experience, "individual mode");
        var individual = SceneFlowManager.FindExperienceBootstrap(SceneManager.GetSceneByName("Joaquin"));
        Check(individual.IsRunning && full.ActiveSegmentIndex == -1, "individual still begins through Ready");
        Check(!SceneManager.GetSceneByName("Alex").isLoaded && plays == 2 && readyCount == 2, "individual only loads selected scene and new song");
        menu.RaiseEvent();
        yield return Until(() => app.CurrentState == AppState.MainMenu, "individual cleanup");
        Check(FullProbeRuntime.Running == 0, "no leftover individual gameplay");

        // A cancelled in-flight additive load must drain and unload, not leak into the menu.
        requested.RaiseEvent(new ExperienceRequest(experience, 0, true));
        yield return Until(() => full.ActiveSegmentIndex == 0, "second full run");
        music.timeUpdateMode = DirectorUpdateMode.Manual;
        Seek(130); yield return null;
        menu.RaiseEvent();
        yield return Until(() => app.CurrentState == AppState.MainMenu, "cancel in-flight preload");
        Check(full.LoadedSceneCount == 0 && FullProbeRuntime.Running == 0, "cancel drained all scenes");
        FullProbeRuntime.ThrowOnPrepare = true;
        requested.RaiseEvent(new ExperienceRequest(experience, 0, true));
        yield return Until(() => app.CurrentState == AppState.MainMenu, "failed preloader recovers");
        Check(full.LoadedSceneCount == 0 && plays == 3 && readyCount == 3, "failed preload does not start music");
        FullProbeRuntime.ThrowOnPrepare = false;
        Debug.Log($"FULLPROBE PASSED: {checks} checks; boundaries, preloads, continuous music, pause, seeks, completion, individual, cancellation and failure recovery.");
        EditorApplication.Exit(0);
    }

    private void TrackMusic(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "ExperienceCore") return;
        foreach (var root in scene.GetRootGameObjects())
        {
            music = root.GetComponentInChildren<PlayableDirector>();
            if (music != null) { music.played += _ => plays++; break; }
        }
    }
}
#endif
