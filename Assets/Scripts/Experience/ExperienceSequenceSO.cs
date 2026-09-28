using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ExperienceSequence", menuName = "Scriptable Objects/Experience/Sequence")]
public sealed class ExperienceSequenceSO : ScriptableObject
{
    [Serializable]
    public sealed class Segment
    {
        public ExperienceSceneDefinitionSO Scene;
        [Min(0)] public double StartTime;
        [Min(0)] public double EndTime;
        [Tooltip("Transicion de entrada a este segmento. Vacio = corte directo.")]
        public ExperienceTransitionSO Transition;
    }

    [SerializeField] private Segment[] segments = Array.Empty<Segment>();
    [Tooltip("Anticipacion de la precarga. Al inicio se preparan los primeros dos segmentos.")]
    [Min(0)] [SerializeField] private float preloadLeadSeconds = 10;
    [Tooltip("Libera recursos de escenas descargadas. Medir su coste en el visor antes de desactivarlo.")]
    [SerializeField] private bool releaseUnusedAssets = true;
    [Tooltip("Si la prueba termina antes que la cancion, conserva su ultima escena hasta el final musical.")]
    [SerializeField] private bool holdLastSceneUntilSongEnds = true;
    public int Count => segments?.Length ?? 0;
    public float PreloadLeadSeconds => preloadLeadSeconds;
    public bool ReleaseUnusedAssets => releaseUnusedAssets;
    public Segment GetSegment(int index) => index >= 0 && index < Count ? segments[index] : null;

    // Half-open intervals: the incoming segment owns the exact boundary.
    public int FindSegment(double songTime)
    {
        if (double.IsNaN(songTime) || songTime < 0) return -1;
        for (int i = 0; i < Count; i++)
            if (songTime >= segments[i].StartTime && songTime < segments[i].EndTime) return i;
        return holdLastSceneUntilSongEnds && Count > 0 && songTime >= segments[Count - 1].EndTime
            ? Count - 1 : -1;
    }

    public bool Validate(out string error)
    {
        if (Count == 0) { error = "La secuencia no tiene segmentos."; return false; }
        double expectedStart = 0;
        for (int i = 0; i < Count; i++)
        {
            Segment segment = segments[i];
            if (segment == null || segment.Scene == null || string.IsNullOrWhiteSpace(segment.Scene.SceneName))
            { error = $"Segmento {i}: falta Scene."; return false; }
            if (double.IsNaN(segment.StartTime) || double.IsInfinity(segment.StartTime) ||
                double.IsNaN(segment.EndTime) || double.IsInfinity(segment.EndTime) ||
                segment.StartTime != expectedStart || segment.EndTime <= segment.StartTime)
            { error = $"Segmento {i}: los intervalos deben ser consecutivos, finitos y comenzar en 0."; return false; }
            expectedStart = segment.EndTime;
        }
        error = null;
        return true;
    }
}
