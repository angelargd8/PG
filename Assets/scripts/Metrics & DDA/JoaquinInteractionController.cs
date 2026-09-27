using System;
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class JoaquinInteractionController :
    MonoBehaviour,
    IExperienceRuntime
{
    private sealed class EnemyAttackProgress
    {
        public uint SpawnVersion;
        public int Hits;
    }


    [Header("Gameplay")]
    [SerializeField] private GuitarDamage _guitarDamage;
    [SerializeField] private EnemyWaveSpawner _enemyWaveSpawner;

    [Header("Metrics")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;
    [SerializeField] private DifficultyLevelEventChannelSO _difficultyChanged;

    [Header("Score")]
    [SerializeField] private ScoreProfileSO _scoreProfile;
    [SerializeField] private ScoreProfileEventChannelSO _scoreProfileChanged;
    [SerializeField] private ScoreBonusEventChannelSO _scoreBonusAwarded;

    [Header("Multi Hit")]
    [Tooltip("Evita registrar varios Success cuando un mismo barrido golpea varios enemigos fuera del beat.")]
    [Min(0f)]
    [SerializeField] private float _offBeatMergeWindow = 0.05f;


    [Header("Failure")]
    [Min(1)]
    [SerializeField] private int _hitsPerFailure = 3;


    [Header("Success Rate Limit")]
    [Tooltip("Cantidad de beats completos que deben pasar después de un Success antes de permitir otro.")]
    [Min(0)]
    [SerializeField] private int _successCooldownBeats = 5;


    private DifficultyLevel _currentDifficulty =
        DifficultyLevel.Normal;

    private double _lastOffBeatHitTime =
        double.NegativeInfinity;

    private int _lastComboMultiplier = 1;

    private bool _isRunning;
    private readonly Dictionary<EnemyMeleeAI, EnemyAttackProgress> _enemyAttackProgress =
        new Dictionary<EnemyMeleeAI, EnemyAttackProgress>();

    private readonly HashSet<EnemyMeleeAI> _subscribedEnemies =
        new HashSet<EnemyMeleeAI>();

    private ExperienceBeatPlayer _beatPlayer;
    private int _lastRegisteredSuccessBeat = -1;


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

        _currentDifficulty =
            _difficultyChanged.CurrentDifficulty;

        _beatPlayer =
            FindFirstObjectByType<ExperienceBeatPlayer>();

        _lastOffBeatHitTime =
            double.NegativeInfinity;

        _lastRegisteredSuccessBeat = -1;

        _lastComboMultiplier = 1;

        _difficultyChanged.Raised +=
            HandleDifficultyChanged;

        _guitarDamage.RhythmHitEvaluated +=
            HandleRhythmHitEvaluated;

        _enemyWaveSpawner.EnemySpawned +=
            HandleEnemySpawned;

        _scoreProfileChanged.RaiseEvent(
            _scoreProfile
        );

        _isRunning = true;
    }


    public void EndExperience()
    {
        if (_difficultyChanged != null)
        {
            _difficultyChanged.Raised -=
                HandleDifficultyChanged;
        }

        if (_guitarDamage != null)
        {
            _guitarDamage.RhythmHitEvaluated -=
                HandleRhythmHitEvaluated;
        }

        if (_enemyWaveSpawner != null)
        {
            _enemyWaveSpawner.EnemySpawned -=
                HandleEnemySpawned;
        }

        foreach (EnemyMeleeAI enemy in _subscribedEnemies)
        {
            if (enemy != null)
            {
                enemy.AttackPerformed -=
                    HandleEnemyAttackPerformed;
            }
        }

        _subscribedEnemies.Clear();
        _enemyAttackProgress.Clear();

        _beatPlayer = null;

        _lastOffBeatHitTime =
            double.NegativeInfinity;

        _lastRegisteredSuccessBeat = -1;

        _lastComboMultiplier = 1;

        _isRunning = false;
    }


    private void OnDisable()
    {
        EndExperience();
    }


    private void HandleRhythmHitEvaluated(GuitarRhythmHit hit, Vector3 feedbackPosition)
    {
        if (!_isRunning)
        {
            return;
        }

        if (ShouldMergeOffBeatHit(hit))
        {
            return;
        }

        if (CanRegisterSuccess(hit))
        {
            RegisterSuccess(hit, feedbackPosition);
        }

        RegisterComboBonus(hit, feedbackPosition);

        _lastComboMultiplier =
            hit.Multiplier;
    }


    private bool ShouldMergeOffBeatHit(GuitarRhythmHit hit)
    {
        if (hit.IsOnBeat)
        {
            return false;
        }

        double elapsed =
            hit.ActualTime - _lastOffBeatHitTime;

        if (elapsed >= 0.0 &&
            elapsed <= _offBeatMergeWindow)
        {
            return true;
        }

        _lastOffBeatHitTime =
            hit.ActualTime;

        return false;
    }


    private void RegisterSuccess(GuitarRhythmHit hit, Vector3 feedbackPosition)
    {
        InteractionResult result =
            new InteractionResult(
                minigameId: "Joaquin",
                interactionType: InteractionType.GuitarHit,
                outcome: InteractionOutcome.Success,
                difficulty: _currentDifficulty,
                expectedTime: hit.ExpectedTime,
                actualTime: hit.ActualTime,
                feedbackPosition: feedbackPosition
            );

        _interactionRegistered.RaiseEvent(
            result
        );
    }


    private void RegisterComboBonus(GuitarRhythmHit hit, Vector3 feedbackPosition)
    {
        if (!hit.AddedToCombo)
        {
            return;
        }

        if (hit.Multiplier <= _lastComboMultiplier)
        {
            return;
        }

        if (_scoreProfile.BonusPoints <= 0)
        {
            return;
        }

        ScoreBonus bonus =
            new ScoreBonus(
                _scoreProfile.BonusPoints,
                feedbackPosition
            );

        _scoreBonusAwarded.RaiseEvent(
            bonus
        );

        Debug.Log(
            $"[JoaquinInteractionController] " +
            $"Combo multiplier increased to x{hit.Multiplier} | " +
            $"Bonus: +{_scoreProfile.BonusPoints}",
            this
        );
    }


    private void HandleDifficultyChanged(DifficultyLevel difficulty)
    {
        _currentDifficulty = difficulty;

        _lastOffBeatHitTime =
            double.NegativeInfinity;

        _lastRegisteredSuccessBeat = -1;

        _enemyAttackProgress.Clear();
    }


    private void HandleEnemySpawned(EnemyMeleeAI enemy)
    {
        if (!_isRunning || enemy == null)
        {
            return;
        }

        EnemyController enemyController =
            enemy.GetComponent<EnemyController>();

        if (enemyController == null)
        {
            return;
        }

        if (_subscribedEnemies.Add(enemy))
        {
            enemy.AttackPerformed +=
                HandleEnemyAttackPerformed;
        }

        _enemyAttackProgress[enemy] =
            new EnemyAttackProgress
            {
                SpawnVersion =
                    enemyController.SpawnVersion,

                Hits = 0
            };
    }


    private void HandleEnemyAttackPerformed(EnemyMeleeAI enemy)
    {
        if (!_isRunning || enemy == null)
        {
            return;
        }

        EnemyController enemyController =
            enemy.GetComponent<EnemyController>();

        if (enemyController == null ||
            !enemyController.IsAlive)
        {
            return;
        }

        if (!_enemyAttackProgress.TryGetValue(
            enemy,
            out EnemyAttackProgress progress
        ) ||
            progress.SpawnVersion !=
            enemyController.SpawnVersion)
        {
            progress =
                new EnemyAttackProgress
                {
                    SpawnVersion =
                        enemyController.SpawnVersion,

                    Hits = 0
                };

            _enemyAttackProgress[enemy] =
                progress;
        }

        progress.Hits++;

        Debug.Log(
            $"[JoaquinInteractionController] " +
            $"Enemy hit player: " +
            $"{progress.Hits}/{_hitsPerFailure}",
            this
        );

        if (progress.Hits < _hitsPerFailure)
        {
            return;
        }

        progress.Hits = 0;

        RegisterPlayerHit();
    }


    private void RegisterPlayerHit()
    {
        double eventTime =
            _beatPlayer != null
                ? _beatPlayer.SongTime
                : 0.0;

        InteractionResult result =
            new InteractionResult(
                minigameId: "Joaquin",
                interactionType: InteractionType.PlayerHit,
                outcome: InteractionOutcome.Failed,
                difficulty: _currentDifficulty,
                expectedTime: eventTime
            );

        _interactionRegistered.RaiseEvent(
            result
        );

        Debug.Log(
            $"[JoaquinInteractionController] " +
            $"PlayerHit registered | " +
            $"Difficulty: {_currentDifficulty}",
            this
        );
    }


    private bool CanRegisterSuccess(GuitarRhythmHit hit)
    {
        if (_lastRegisteredSuccessBeat < 0)
        {
            _lastRegisteredSuccessBeat =
                hit.BeatIndex;

            return true;
        }

        if (hit.BeatIndex < _lastRegisteredSuccessBeat)
        {
            _lastRegisteredSuccessBeat =
                hit.BeatIndex;

            return true;
        }

        int beatsSinceSuccess =
            hit.BeatIndex -
            _lastRegisteredSuccessBeat;

        if (beatsSinceSuccess <= _successCooldownBeats)
        {
            return false;
        }

        _lastRegisteredSuccessBeat =
            hit.BeatIndex;

        return true;
    }


    public void SetHitsPerFailure(int hitsPerFailure)
    {
        _hitsPerFailure =
            Mathf.Max(1, hitsPerFailure);

        _enemyAttackProgress.Clear();
    }


    private bool ValidateReferences()
    {
        if (_guitarDamage == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] GuitarDamage no está asignado.",
                this
            );

            return false;
        }

        if (_interactionRegistered == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] InteractionRegistered no está asignado.",
                this
            );

            return false;
        }

        if (_difficultyChanged == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] DifficultyChanged no está asignado.",
                this
            );

            return false;
        }

        if (_scoreProfile == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] ScoreProfile no está asignado.",
                this
            );

            return false;
        }

        if (_scoreProfileChanged == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] ScoreProfileChanged no está asignado.",
                this
            );

            return false;
        }

        if (_scoreBonusAwarded == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] ScoreBonusAwarded no está asignado.",
                this
            );

            return false;
        }


        if (_enemyWaveSpawner == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] EnemyWaveSpawner no está asignado.",
                this
            );

            return false;
        }

        return true;
    }
}