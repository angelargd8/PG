using UnityEngine;

[System.Serializable]
public sealed class DannielDifficultyProfile
{
    [Range(0f, 1f)]
    [SerializeField] private float _enemyDensity = 1f;

    [Min(0f)]
    [SerializeField] private float _speedScale = 1f;


    public float EnemyDensity => _enemyDensity;

    public float SpeedScale => _speedScale;
}