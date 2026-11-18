using UnityEngine;

[System.Serializable]
public sealed class JoaquinDifficultyProfile
{
    [Header("Enemy Health")]
    [Tooltip("Vida inicial de EnemyJoaquinn y EnemyJoaquincho al aparecer.")]
    [Min(1)]
    [SerializeField] private int _enemyHealth = 1;

    [Header("Enemy Spawning")]
    [Min(1)]
    [SerializeField] private int _normalEnemiesPerSpawn = 1;

    [Min(1)]
    [SerializeField] private int _strongEnemiesPerSpawn = 2;

    [Min(1)]
    [SerializeField] private int _introductionMaxActiveEnemies = 3;

    [Min(1)]
    [SerializeField] private int _maxActiveEnemies = 4;

    [Header("Failure")]
    [Min(1)]
    [SerializeField] private int _hitsPerFailure = 3;


    public int NormalEnemiesPerSpawn => _normalEnemiesPerSpawn;
    public int StrongEnemiesPerSpawn => _strongEnemiesPerSpawn;
    public int IntroductionMaxActiveEnemies => _introductionMaxActiveEnemies;
    public int MaxActiveEnemies => _maxActiveEnemies;
    public int HitsPerFailure => _hitsPerFailure;
    public int EnemyHealth => Mathf.Max(1, _enemyHealth);
}
