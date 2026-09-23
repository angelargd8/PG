using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DynamicDifficultySystem : MonoBehaviour
{
    [Header("Difficulty")]
    [SerializeField] private DifficultyLevel _initialDifficulty =
        DifficultyLevel.Normal;


    [Header("Evaluation Window")]
    [Min(1)]
    [SerializeField] private int _windowSize = 8;

    [Min(1)]
    [SerializeField] private int _minimumSamplesForIncrease = 5;

    [Min(1)]
    [SerializeField] private int _minimumPlayerActionsForIncrease = 4;


    [Header("Increase")]
    [Range(0f, 1f)]
    [SerializeField] private float _increaseSuccessRate = 0.8f;


    [Header("Decrease")]
    [Min(1)]
    [SerializeField] private int _minimumSamplesForDecrease = 5;

    [Min(1)]
    [SerializeField] private int _minimumPlayerActionsForDecrease = 2;

    [Range(0f, 1f)]
    [SerializeField] private float _decreaseErrorRate = 0.6f;


    [Header("Events")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;
    [SerializeField] private PlayerStateEventChannelSO _playerStateChanged;
    [SerializeField] private DifficultyLevelEventChannelSO _difficultyChanged;
    [SerializeField] private VoidEventChannelSO _experienceReady;
    [SerializeField] private VoidEventChannelSO _mainMenuRequested;


    [Header("Logging")]
    [SerializeField] private MetricsLogger _metricsLogger;


    private readonly Queue<InteractionResult> _recentResults =
        new Queue<InteractionResult>();

    private PlayerState _currentPlayerState =
        PlayerState.Transitioning;

    private string _currentMinigameId;


    public DifficultyLevel CurrentDifficulty { get; private set; }

    public int RecentSampleCount => _recentResults.Count;

    public float RecentSuccessRate => CalculateSuccessRate();


    private void Awake()
    {
        CurrentDifficulty = _initialDifficulty;

        if (_difficultyChanged != null)
        {
            _difficultyChanged.SetCurrentDifficulty(CurrentDifficulty);
        }
    }


    private void OnEnable()
    {
        if (_interactionRegistered != null)
        {
            _interactionRegistered.Raised += HandleInteraction;
        }

        if (_playerStateChanged != null)
        {
            _playerStateChanged.Raised += HandlePlayerStateChanged;
        }

        if (_experienceReady != null)
        {
            _experienceReady.Raised += HandleExperienceReady;
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised += HandleMainMenuRequested;
        }
    }


    private void OnDisable()
    {
        if (_interactionRegistered != null)
        {
            _interactionRegistered.Raised -= HandleInteraction;
        }

        if (_playerStateChanged != null)
        {
            _playerStateChanged.Raised -= HandlePlayerStateChanged;
        }

        if (_experienceReady != null)
        {
            _experienceReady.Raised -= HandleExperienceReady;
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised -= HandleMainMenuRequested;
        }
    }


    private void OnValidate()
    {
        _windowSize = Mathf.Max(1, _windowSize);

        _minimumSamplesForIncrease =
            Mathf.Clamp(_minimumSamplesForIncrease, 1, _windowSize);

        _minimumPlayerActionsForIncrease =
            Mathf.Clamp(_minimumPlayerActionsForIncrease, 1, _windowSize);
    }


   private void HandleInteraction(InteractionResult result)
    {
        if (result.Outcome == InteractionOutcome.Unknown)
        {
            return;
        }

        HandleMinigameChange(result.MinigameId);

        if (result.Difficulty != CurrentDifficulty)
        {
            return;
        }

        bool isPlayerAction = IsPlayerAction(result);

        if (_currentPlayerState == PlayerState.Transitioning)
        {
            return;
        }

        if (_currentPlayerState == PlayerState.Idle)
        {
            if (isPlayerAction)
            {
                AddToRecentWindow(result);
            }

            return;
        }

        AddToRecentWindow(result);

        if (_currentPlayerState == PlayerState.Overloaded)
        {
            EvaluateOverloadedPerformance();
            return;
        }

        if (
            _currentPlayerState == PlayerState.Engaged &&
            isPlayerAction)
        {
            EvaluateEngagedPerformance();
        }
    }


    private void HandlePlayerStateChanged(PlayerState state)
    {
        PlayerState previousState = _currentPlayerState;
        _currentPlayerState = state;

        if (
            state == PlayerState.Idle ||
            state == PlayerState.Transitioning)
        {
            ClearEvaluationWindow();
            return;
        }

        if (
            state == PlayerState.Overloaded &&
            previousState != PlayerState.Overloaded)
        {
            float successRate = CalculateSuccessRate();

            string reason =
                $"PlayerState: Overloaded | " +
                $"Minigame: {_currentMinigameId ?? "N/A"} | " +
                $"RecentSuccessRate: {successRate:F2}";

            DecreaseDifficulty(reason);
        }
    }


    private void EvaluateEngagedPerformance()
    {
        if (_recentResults.Count < _minimumSamplesForIncrease)
        {
            return;
        }

        if (CountPlayerActions() < _minimumPlayerActionsForIncrease)
        {
            return;
        }

        float successRate = CalculateSuccessRate();

        if (successRate < _increaseSuccessRate)
        {
            return;
        }

        string reason =
            $"High recent performance | " +
            $"Minigame: {_currentMinigameId ?? "N/A"} | " +
            $"SuccessRate: {successRate:F2}";

        IncreaseDifficulty(reason);
    }

    private void EvaluateOverloadedPerformance()
    {
        if (CurrentDifficulty == DifficultyLevel.Easy)
        {
            return;
        }

        if (_recentResults.Count < _minimumSamplesForDecrease)
        {
            return;
        }

        if (CountPlayerActions() < _minimumPlayerActionsForDecrease)
        {
            return;
        }

        float errorRate = CalculateErrorRate();

        if (errorRate < _decreaseErrorRate)
        {
            return;
        }

        string reason =
            $"Persistent overload | " +
            $"Minigame: {_currentMinigameId ?? "N/A"} | " +
            $"ErrorRate: {errorRate:F2}";

        DecreaseDifficulty(reason);
    }

    private float CalculateErrorRate()
    {
        if (_recentResults.Count == 0)
        {
            return 0f;
        }

        int errors = 0;

        foreach (InteractionResult result in _recentResults)
        {
            if (
                result.Outcome == InteractionOutcome.Failed ||
                result.Outcome == InteractionOutcome.Missed)
            {
                errors++;
            }
        }

        return (float)errors / _recentResults.Count;
    }

    private void IncreaseDifficulty(string reason)
    {
        DifficultyLevel nextDifficulty = CurrentDifficulty switch
        {
            DifficultyLevel.Easy => DifficultyLevel.Normal,
            DifficultyLevel.Normal => DifficultyLevel.Hard,
            _ => DifficultyLevel.Hard
        };

        ApplyDifficulty(nextDifficulty, reason);
    }

    private void DecreaseDifficulty(string reason)
    {
        DifficultyLevel nextDifficulty = CurrentDifficulty switch
        {
            DifficultyLevel.Hard => DifficultyLevel.Normal,
            DifficultyLevel.Normal => DifficultyLevel.Easy,
            _ => DifficultyLevel.Easy
        };

        ApplyDifficulty(nextDifficulty, reason);
    }

    private void ApplyDifficulty(DifficultyLevel difficulty, string reason)
    {
        if (CurrentDifficulty == difficulty)
        {
            ClearEvaluationWindow();
            return;
        }

        DifficultyLevel previousDifficulty = CurrentDifficulty;
        CurrentDifficulty = difficulty;

        if (_difficultyChanged != null)
        {
            _difficultyChanged.RaiseEvent(CurrentDifficulty);
        }

        if (_metricsLogger != null)
        {
            _metricsLogger.LogDifficultyChange(
                previousDifficulty,
                CurrentDifficulty,
                reason
            );
        }

        Debug.Log(
            $"[DynamicDifficultySystem] " +
            $"{previousDifficulty} -> {CurrentDifficulty} | {reason}",
            this
        );

        ClearEvaluationWindow();
    }

    private void HandleMinigameChange(string minigameId)
    {
        if (_currentMinigameId == minigameId)
        {
            return;
        }

        _currentMinigameId = minigameId;

        ClearEvaluationWindow();
    }

    private void HandleExperienceReady()
    {
        ClearEvaluationWindow();

        _currentMinigameId = null;

        if (_difficultyChanged != null)
        {
            _difficultyChanged.RaiseEvent(CurrentDifficulty);
        }
    }

    private void HandleMainMenuRequested()
    {
        ClearEvaluationWindow();

        _currentMinigameId = null;
        _currentPlayerState = PlayerState.Transitioning;

        CurrentDifficulty = _initialDifficulty;

        if (_difficultyChanged != null)
        {
            _difficultyChanged.SetCurrentDifficulty(CurrentDifficulty);
        }
    }

    private bool IsPlayerAction(InteractionResult result)
    {
        return
            result.Outcome == InteractionOutcome.Success ||
            result.Outcome == InteractionOutcome.Failed;
    }

    private void AddToRecentWindow(InteractionResult result)
    {
        _recentResults.Enqueue(result);

        while (_recentResults.Count > _windowSize)
        {
            _recentResults.Dequeue();
        }
    }

    private int CountPlayerActions()
    {
        int actions = 0;

        foreach (InteractionResult result in _recentResults)
        {
            if (IsPlayerAction(result))
            {
                actions++;
            }
        }

        return actions;
    }

    private float CalculateSuccessRate()
    {
        if (_recentResults.Count == 0)
        {
            return 0f;
        }

        int successes = 0;

        foreach (InteractionResult result in _recentResults)
        {
            if (result.Outcome == InteractionOutcome.Success)
            {
                successes++;
            }
        }

        return (float)successes / _recentResults.Count;
    }

    private void ClearEvaluationWindow()
    {
        _recentResults.Clear();
    }
}