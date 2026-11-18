#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DannielSegmentProbeSetup
{
    private const string Pending = "DannielSegmentProbe.Pending";
    static DannielSegmentProbeSetup() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (EditorApplication.isPlaying && EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("Danniel Shooting Probe").AddComponent<DannielSegmentProbe>();
    }
    public static void Start()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/DannielSegmentProbe.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
}
#endif


