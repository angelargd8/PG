#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

[InitializeOnLoad]
public static class FullExperienceProbeSetup
{
    private static readonly System.Collections.Generic.List<ScriptableObject> keep = new();
    private const string Pending = "FullExperienceProbe.Pending";
    static FullExperienceProbeSetup() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        // Editor search-index startup errors must not leave batch-mode tests paused.
        if (EditorApplication.isPlaying && EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        Debug.Log("FULLPROBE: starting runtime checks");
        new GameObject("Probe Runner").AddComponent<FullExperienceProbeRunner>();
    }

    public static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static T Asset<T>(string name) where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        asset.hideFlags = HideFlags.DontUnloadUnusedAsset;
        keep.Add(asset);
        AssetDatabase.CreateAsset(asset, $"Assets/{name}.asset");
        return asset;
    }

    public static void Start()
    {
        try
        {
            var ready = Asset<VoidEventChannelSO>("Ready");
            var completed = Asset<VoidEventChannelSO>("Completed");
            var menu = Asset<VoidEventChannelSO>("MenuRequested");
            var requested = Asset<ExperienceEventChannelSO>("Requested");
            var activation = Asset<ExperienceSceneActivationEventChannelSO>("Activation");
            var fade = Asset<CameraFadeTransitionSO>("Fade");
            Set(fade, "overlayShader", Shader.Find("Experience/CameraFade"));
            EditorUtility.SetDirty(fade);
            var sequence = Asset<ExperienceSequenceSO>("Sequence");
            string[] names = { "Alex", "Joaquin", "Danniel", "Jeremy" };
            double[] times = { 0, 31, 125, 156, 187 };
            var definitions = new ExperienceSceneDefinitionSO[names.Length];
            var segments = new ExperienceSequenceSO.Segment[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                definitions[i] = Asset<ExperienceSceneDefinitionSO>(names[i]);
                Set(definitions[i], "sceneName", names[i]);
                EditorUtility.SetDirty(definitions[i]);
                segments[i] = new ExperienceSequenceSO.Segment { Scene = definitions[i], StartTime = times[i], EndTime = times[i + 1], Transition = fade };
                segments[i].PreloadBeforePlayback = names[i] == "Danniel";
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var content = new GameObject("Experience Content");
                content.SetActive(false);
                var runtime = content.AddComponent<FullProbeRuntime>();
                var bootstrap = new GameObject("ExperienceSceneBootstrap").AddComponent<ExperienceSceneBootstrap>();
                Set(bootstrap, "preloaders", new MonoBehaviour[] { runtime });
                Set(bootstrap, "runtimeSystems", new MonoBehaviour[] { runtime });
                Set(bootstrap, "gameplayRoot", content);
                Set(bootstrap, "sceneActivation", activation);
                Set(bootstrap, "experienceReady", ready);
                EditorSceneManager.SaveScene(scene, $"Assets/{names[i]}.unity");
            }
            Set(sequence, "segments", segments);
            EditorUtility.SetDirty(sequence);
            var experience = Asset<ExperienceDefinitionSO>("Experience");
            Set(experience, "fullSequence", sequence);
            Set(experience, "scenes", definitions);
            Set(experience, "displayName", "Probe Song");
            EditorUtility.SetDirty(experience);

            foreach (string name in new[] { "MainMenu", "LoadingScene" })
            {
                var empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject(name);
                EditorSceneManager.SaveScene(empty, $"Assets/{name}.unity");
            }

            // A real audio Timeline; tests seek between boundaries while playback keeps advancing.
            using (var writer = new BinaryWriter(File.Create("Assets/Song.wav")))
            {
                int bytes = 8000 * 187 * 2;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(8000); writer.Write(16000);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes); writer.Write(new byte[bytes]);
            }
            AssetDatabase.ImportAsset("Assets/Song.wav", ImportAssetOptions.ForceSynchronousImport);
            var timeline = Asset<TimelineAsset>("Music");
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = 187;
            var track = timeline.CreateTrack<AudioTrack>(null, "Song");
            var clip = track.CreateClip<AudioPlayableAsset>();
            ((AudioPlayableAsset)clip.asset).clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Song.wav");
            clip.duration = 187;
            EditorUtility.SetDirty(timeline);
            var core = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var music = new GameObject("Music");
            var director = music.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.playableAsset = timeline;
            var source = music.AddComponent<AudioSource>();
            source.playOnAwake = false;
            director.SetGenericBinding(track, source);
            var clock = music.AddComponent<ExperienceMusicClock>();
            Set(clock, "_playableDirector", director);
            var sequenceDirector = music.AddComponent<SequenceDirector>();
            Set(sequenceDirector, "playableDirector", director);
            Set(sequenceDirector, "musicSource", source);
            Set(sequenceDirector, "experienceReady", ready);
            Set(sequenceDirector, "experienceCompleted", completed);
            EditorSceneManager.SaveScene(core, "Assets/ExperienceCore.unity");

            var entry = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>();
            var flow = new GameObject("SceneFlow").AddComponent<SceneFlowManager>();
            var full = flow.gameObject.AddComponent<FullExperienceDirector>();
            Set(full, "experienceReady", ready); Set(full, "experienceCompleted", completed);
            Set(full, "mainMenuRequested", menu); Set(full, "sceneActivation", activation);
            Set(flow, "fullExperienceDirector", full);
            var app = new GameObject("AppState").AddComponent<AppStateMachine>();
            Set(app, "_sceneFlowManager", flow); Set(app, "experienceRequested", requested);
            Set(app, "experienceReady", ready); Set(app, "_mainMenuRequested", menu);
            Set(app, "_mainMenuEntered", Asset<VoidEventChannelSO>("MenuEntered"));
            EditorSceneManager.SaveScene(entry, "Assets/Bootstrap.unity");
            string[] paths = { "Bootstrap", "MainMenu", "LoadingScene", "ExperienceCore", "Alex", "Joaquin", "Danniel", "Jeremy" };
            EditorBuildSettings.scenes = Array.ConvertAll(paths, path => new EditorBuildSettingsScene($"Assets/{path}.unity", true));
            foreach (var asset in keep) { asset.hideFlags = HideFlags.None; EditorUtility.SetDirty(asset); }
            AssetDatabase.SaveAssets();
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
