using System.Collections.Generic;
using UnityEngine;

public sealed class MetricsSystem : MonoBehaviour
{
    [Header("Window")]
    [SerializeField] private int _windowSize = 10;

    [Header("Events")]
    [SerializeField] private BoolEventChannelSO _gameplayPauseChanged;

    [Header("Logging")]
    [SerializeField] private MetricsLogger _metricsLogger;

    private readonly Queue<InteractionResult> _recentResults = new Queue<InteractionResult>();
    private bool _isPaused;
    private int _totalInteractions;
    private int _successfulInteractions;
    private int _failedInteractions;
    private int _missedInteractions;
    private double _totalTimingError;
    private int _timingSamples;
    private double _totalReactionTime;
    private int _reactionSamples;
    private float _totalSpatialAccuracy;
    private int _spatialSamples;
    private float _totalDirectionAccuracy;
    private int _directionSamples;

    public int TotalInteractions => _totalInteractions;
    public int SuccessfulInteractions => _successfulInteractions;
    public int FailedInteractions => _failedInteractions;
    public int MissedInteractions => _missedInteractions;

    public float SuccessRate =>
        _totalInteractions > 0
            ? (float)_successfulInteractions / _totalInteractions
            : 0f;

    public double AverageTimingError =>
        _timingSamples > 0
            ? _totalTimingError / _timingSamples
            : 0.0;

    public double AverageReactionTime =>
        _reactionSamples > 0
            ? _totalReactionTime / _reactionSamples
            : 0.0;

    public float AverageSpatialAccuracy =>
        _spatialSamples > 0
            ? _totalSpatialAccuracy / _spatialSamples
            : 0f;

    public float AverageDirectionAccuracy =>
        _directionSamples > 0
            ? _totalDirectionAccuracy / _directionSamples
            : 0f;


    private void OnEnable()
    {
        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised += HandlePauseChanged;
        }
    }


    private void OnDisable()
    {
        if (_gameplayPauseChanged != null)
        {
            _gameplayPauseChanged.Raised -= HandlePauseChanged;
        }
    }


    public void RegisterInteraction(InteractionResult result)
    {
        if (_isPaused)
        {
            return;
        }

        RegisterOutcome(result);
        RegisterTiming(result);
        RegisterReactionTime(result);
        RegisterSpatialAccuracy(result);
        RegisterDirectionAccuracy(result);

        AddToRecentWindow(result);

        if (_metricsLogger != null)
        {
            _metricsLogger.LogInteraction(result);
        }
    }


    private void RegisterOutcome(InteractionResult result)
    {
        _totalInteractions++;

        switch (result.Outcome)
        {
            case InteractionOutcome.Success:
                _successfulInteractions++;
                break;

            case InteractionOutcome.Failed:
                _failedInteractions++;
                break;

            case InteractionOutcome.Missed:
                _missedInteractions++;
                break;
        }
    }


    private void RegisterTiming(InteractionResult result)
    {
        if (!result.TimingError.HasValue)
        {
            return;
        }

        _totalTimingError += result.TimingError.Value;
        _timingSamples++;
    }


    private void RegisterReactionTime(InteractionResult result)
    {
        if (!result.ReactionTime.HasValue)
        {
            return;
        }

        _totalReactionTime += result.ReactionTime.Value;
        _reactionSamples++;
    }


    private void RegisterSpatialAccuracy(InteractionResult result)
    {
        if (!result.SpatialAccuracy.HasValue)
        {
            return;
        }

        _totalSpatialAccuracy += result.SpatialAccuracy.Value;
        _spatialSamples++;
    }


    private void RegisterDirectionAccuracy(InteractionResult result)
    {
        if (!result.DirectionAccuracy.HasValue)
        {
            return;
        }

        _totalDirectionAccuracy += result.DirectionAccuracy.Value;
        _directionSamples++;
    }


    private void AddToRecentWindow(InteractionResult result)
    {
        _recentResults.Enqueue(result);

        while (_recentResults.Count > _windowSize)
        {
            _recentResults.Dequeue();
        }
    }


    private void HandlePauseChanged(bool isPaused)
    {
        _isPaused = isPaused;
    }


    public void ResetMetrics()
    {
        _recentResults.Clear();

        _totalInteractions = 0;
        _successfulInteractions = 0;
        _failedInteractions = 0;
        _missedInteractions = 0;

        _totalTimingError = 0.0;
        _timingSamples = 0;

        _totalReactionTime = 0.0;
        _reactionSamples = 0;

        _totalSpatialAccuracy = 0f;
        _spatialSamples = 0;

        _totalDirectionAccuracy = 0f;
        _directionSamples = 0;
    }
}