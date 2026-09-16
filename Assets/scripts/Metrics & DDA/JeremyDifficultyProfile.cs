using UnityEngine;

[System.Serializable]
public sealed class JeremyDifficultyProfile
{
    [SerializeField] private int _spawnEveryNBeats;
    [SerializeField] private float _movementSpeed;

    [Range(0f, 1f)]
    [SerializeField] private float _specificHandProbability;

    public int SpawnEveryNBeats => _spawnEveryNBeats;
    public float MovementSpeed => _movementSpeed;
    public float SpecificHandProbability => _specificHandProbability;
}