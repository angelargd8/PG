using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerStateSystem : MonoBehaviour
{
    [Header("Window")]
    [Min(1)]
    [SerializeField] private int _windowSize = 8;

    [Min(1)]
    [SerializeField] private int _minimumSamplesForOverload = 5;

    [Min(1)]
    [SerializeField] private int _minimumPlayerActionsForOverload = 2;


    [Header("Idle")]
    [Min(0.1f)]
    [SerializeField] private float _idleSeconds = 5f;


    [Header("Overload")]
    [Range(0f, 1f)]
    [SerializeField] private float _overloadEnterErrorRate = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float _overloadExitErrorRate = 0.3f;

    [Min(1)]
    [SerializeField] private int _consecutiveErrorsForOverload = 3;


    [Header("Events")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;
    [SerializeField] private BoolEventChannelSO _gameplayPauseChanged;
    [SerializeField] private VoidEventChannelSO _experienceReady;
    [SerializeField] private VoidEventChannelSO _mainMenuRequested;
    [SerializeField] private PlayerStateEventChannelSO _playerStateChanged;


    [Header("Logging")]
    [SerializeField] private MetricsLogger _metricsLogger;


    private readonly Queue<InteractionResult> _recentResults =
        new Queue<InteractionResult>();

    private bool _isPaused;
    private bool _isExperienceActive;
    private bool _hasPlayerAction;

    private float _lastPlayerActionTime;
    private int _consecutiveErrors;
    private string _currentMinigameId;


    public PlayerState CurrentState { get; private set; } =
        PlayerState.Transitioning;

    public int RecentSampleCount => _recentResults.Count;
    public float RecentErrorRate => CalculateErrorRate();


    private void OnEnable()
    {
        if (_interactionRegistered != null)
        {
            _interactionRegistered.Raised += HandleInteraction;
        }

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised += HandlePauseChanged;
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

        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised -= HandlePauseChanged;
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

        _minimumSamplesForOverload =
            Mathf.Clamp(_minimumSamplesForOverload, 1, _windowSize);

        _minimumPlayerActionsForOverload =
            Mathf.Clamp(_minimumPlayerActionsForOverload, 1, _windowSize);

        _overloadExitErrorRate =
            Mathf.Min(_overloadExitErrorRate, _overloadEnterErrorRate);
    }


    private void Update()
    {
        if (!_isExperienceActive || _isPaused)
        {
            return;
        }

        if (CurrentState == PlayerState.Transitioning)
        {
            return;
        }

        EvaluateState();
    }


    private void HandleInteraction(InteractionResult result)
    {
        if (!_isExperienceActive || _isPaused)
        {
            return;
        }

        if (CurrentState == PlayerState.Transitioning)
        {
            return;
        }

        if (result.Outcome == InteractionOutcome.Unknown)
        {
            return;
        }

        HandleMinigameChange(result.MinigameId);

        bool isPlayerAction = IsPlayerAction(result);

        if (!isPlayerAction && CurrentState == PlayerState.Idle)
        {
            return;
        }

        if (isPlayerAction)
        {
            if (CurrentState == PlayerState.Idle)
            {
                ClearPerformanceWindow();
            }

            _hasPlayerAction = true;
            _lastPlayerActionTime = Time.time;
        }

        AddToRecentWindow(result);
        UpdateConsecutiveErrors(result);

        EvaluateState();
    }


    private void EvaluateState()
    {
        if (!_hasPlayerAction)
        {
            EnterIdle();
            return;
        }

        if (Time.time - _lastPlayerActionTime >= _idleSeconds)
        {
            EnterIdle();
            return;
        }

        if (CurrentState == PlayerState.Overloaded)
        {
            if (!ShouldExitOverloaded())
            {
                return;
            }
        }
        else if (ShouldEnterOverloaded())
        {
            SetState(PlayerState.Overloaded);
            return;
        }

        SetState(PlayerState.Engaged);
    }


    private bool ShouldEnterOverloaded()
    {
        if (_recentResults.Count < _minimumSamplesForOverload)
        {
            return false;
        }

        if (CountPlayerActions() < _minimumPlayerActionsForOverload)
        {
            return false;
        }

        if (_consecutiveErrors >= _consecutiveErrorsForOverload)
        {
            return true;
        }

        return CalculateErrorRate() >= _overloadEnterErrorRate;
    }


    private bool ShouldExitOverloaded()
    {
        if (_recentResults.Count < _minimumSamplesForOverload)
        {
            return true;
        }

        return
            CalculateErrorRate() <= _overloadExitErrorRate &&
            _consecutiveErrors == 0;
    }


    private bool IsPlayerAction(InteractionResult result)
    {
        return
            result.Outcome == InteractionOutcome.Success ||
            result.Outcome == InteractionOutcome.Failed;
    }


    private void UpdateConsecutiveErrors(InteractionResult result)
    {
        if (result.Outcome == InteractionOutcome.Success)
        {
            _consecutiveErrors = 0;
            return;
        }

        if (
            result.Outcome == InteractionOutcome.Failed ||
            result.Outcome == InteractionOutcome.Missed)
        {
            _consecutiveErrors++;
        }
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


    private void EnterIdle()
    {
        if (CurrentState != PlayerState.Idle)
        {
            ClearPerformanceWindow();
        }

        _hasPlayerAction = false;

        SetState(PlayerState.Idle);
    }


    private void ClearPerformanceWindow()
    {
        _recentResults.Clear();
        _consecutiveErrors = 0;
    }


    private void HandleMinigameChange(string minigameId)
    {
        if (_currentMinigameId == minigameId)
        {
            return;
        }

        _currentMinigameId = minigameId;

        ClearPerformanceWindow();
        _hasPlayerAction = false;
    }


    private void HandlePauseChanged(bool isPaused)
    {
        _isPaused = isPaused;
    }


    private void HandleExperienceReady()
    {
        _isExperienceActive = true;

        ResetStateData();

        SetState(PlayerState.Idle);
    }


    private void HandleMainMenuRequested()
    {
        _isExperienceActive = false;

        ResetStateData();

        SetState(PlayerState.Transitioning);
    }


    private void ResetStateData()
    {
        ClearPerformanceWindow();

        _hasPlayerAction = false;
        _currentMinigameId = null;
    }


    private void SetState(PlayerState state)
    {
        if (CurrentState == state)
        {
            return;
        }

        CurrentState = state;

        if (_metricsLogger != null)
        {
            _metricsLogger.LogPlayerState(CurrentState);
        }

        if (_playerStateChanged != null)
        {
            _playerStateChanged.RaiseEvent(CurrentState);
        }

        Debug.Log($"[PlayerStateSystem] State changed to {CurrentState}.", this);
    }
}