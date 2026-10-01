using UnityEngine;

[DisallowMultipleComponent]
public sealed class DannielDifficultyController :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("References")]
    [SerializeField] private EnemySpawnDirector _enemySpawnDirector;
    [SerializeField] private SegmentPool _segmentPool;
    [SerializeField] private DannielRhythmDirector _rhythmDirector;

    [Header("Difficulty")]
    [SerializeField] private DannielDifficultyConfigSO _difficultyConfig;
    [SerializeField] private DifficultyLevelEventChannelSO _difficultyChanged;

    private bool _isRunning;


    public void BeginExperience()
    {
        if (_isRunning)
        {
            return;
        }

        if (_enemySpawnDirector == null ||
            _segmentPool == null ||
            _rhythmDirector == null ||
            _difficultyConfig == null)
        {
            Debug.LogError(
                "[DannielDifficultyController] Faltan referencias.",
                this
            );

            return;
        }

        _isRunning = true;

        DifficultyLevel difficulty =
            DifficultyLevel.Normal;

        if (_difficultyChanged != null)
        {
            _difficultyChanged.Raised +=
                HandleDifficultyChanged;

            difficulty =
                _difficultyChanged.CurrentDifficulty;
        }

        ApplyDifficulty(difficulty);
    }


    public void EndExperience()
    {
        if (_difficultyChanged != null)
        {
            _difficultyChanged.Raised -=
                HandleDifficultyChanged;
        }

        _isRunning = false;
    }


    private void OnDisable()
    {
        EndExperience();
    }


    private void HandleDifficultyChanged(
        DifficultyLevel difficulty
    )
    {
        if (!_isRunning)
        {
            return;
        }

        ApplyDifficulty(difficulty);
    }


    private void ApplyDifficulty(
        DifficultyLevel difficulty
    )
    {
        DannielDifficultyProfile profile =
            _difficultyConfig.GetProfile(
                difficulty
            );

        if (profile == null)
        {
            return;
        }

        _enemySpawnDirector.SetDifficulty(
            difficulty,
            profile.EnemyDensity
        );

        _segmentPool.SetDifficultySpeedScale(
            profile.SpeedScale
        );

        _rhythmDirector.SetDifficulty(
            difficulty
        );

        Debug.Log(
            $"[DannielDifficultyController] " +
            $"Difficulty: {difficulty} | " +
            $"EnemyDensity: {profile.EnemyDensity:F2} | " +
            $"SpeedScale: {profile.SpeedScale:F2}",
            this
        );
    }
}