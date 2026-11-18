using UnityEngine;

[DisallowMultipleComponent]
public sealed class JoaquinDifficultyController :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("References")]
    [SerializeField] private EnemyWaveSpawner _enemyWaveSpawner;
    [SerializeField] private JoaquinInteractionController _interactionController;

    [Header("Difficulty")]
    [SerializeField] private JoaquinDifficultyConfigSO _difficultyConfig;
    [SerializeField] private DifficultyLevelEventChannelSO _difficultyChanged;


    private bool _isRunning;


    public void BeginExperience()
    {
        if (_isRunning)
        {
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        _isRunning = true;

        _difficultyChanged.Raised +=
            HandleDifficultyChanged;

        ApplyDifficulty(
            _difficultyChanged.CurrentDifficulty
        );
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


    private void HandleDifficultyChanged(DifficultyLevel difficulty)
    {
        if (!_isRunning)
        {
            return;
        }

        ApplyDifficulty(difficulty);
    }


    private void ApplyDifficulty(DifficultyLevel difficulty)
    {
        JoaquinDifficultyProfile profile =
            _difficultyConfig.GetProfile(difficulty);

        if (profile == null)
        {
            return;
        }

        _enemyWaveSpawner.SetDifficulty(
            profile.NormalEnemiesPerSpawn,
            profile.StrongEnemiesPerSpawn,
            profile.IntroductionMaxActiveEnemies,
            profile.MaxActiveEnemies,
            profile.EnemyHealth
        );

        _interactionController.SetHitsPerFailure(
            profile.HitsPerFailure
        );

        Debug.Log(
            $"[JoaquinDifficultyController] " +
            $"Difficulty: {difficulty} | " +
            $"Normal Spawn: {profile.NormalEnemiesPerSpawn} | " +
            $"Strong Spawn: {profile.StrongEnemiesPerSpawn} | " +
            $"Intro Max: {profile.IntroductionMaxActiveEnemies} | " +
            $"Max Active: {profile.MaxActiveEnemies} | " +
            $"Enemy Health: {profile.EnemyHealth} | " +
            $"Hits Per Failure: {profile.HitsPerFailure}",
            this
        );
    }


    private bool ValidateReferences()
    {
        if (_enemyWaveSpawner == null)
        {
            Debug.LogError(
                "[JoaquinDifficultyController] EnemyWaveSpawner no está asignado.",
                this
            );

            return false;
        }

        if (_interactionController == null)
        {
            Debug.LogError(
                "[JoaquinDifficultyController] JoaquinInteractionController no está asignado.",
                this
            );

            return false;
        }

        if (_difficultyConfig == null)
        {
            Debug.LogError(
                "[JoaquinDifficultyController] DifficultyConfig no está asignado.",
                this
            );

            return false;
        }

        if (_difficultyChanged == null)
        {
            Debug.LogError(
                "[JoaquinDifficultyController] DifficultyChanged no está asignado.",
                this
            );

            return false;
        }

        return true;
    }
}
