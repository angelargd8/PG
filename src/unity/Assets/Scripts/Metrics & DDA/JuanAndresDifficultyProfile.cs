using UnityEngine;

[System.Serializable]
public sealed class JuanAndresDifficultyProfile
{
    [Min(1)]
    [SerializeField] private int _spawnEveryNBeats;

    [Min(0.1f)]
    [SerializeField] private float _responseWindow;

    [Min(1)]
    [SerializeField] private int _maxActiveTargets;

    [Range(0f, 1f)]
    [SerializeField] private float _dualTargetProbability;


    public int SpawnEveryNBeats => _spawnEveryNBeats;
    public float ResponseWindow => _responseWindow;
    public int MaxActiveTargets => _maxActiveTargets;
    public float DualTargetProbability => _dualTargetProbability;
}