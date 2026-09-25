using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TimingFeedbackSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloatingTimingFeedback _feedbackPrefab;
    [SerializeField] private Transform _poolRoot;

    [Header("Pool")]
    [Min(1)]
    [SerializeField] private int _initialPoolSize = 8;

    [Min(1)]
    [SerializeField] private int _maxPoolSize = 16;

    [Header("Events")]
    [SerializeField] private ScoreChangedEventChannelSO _scoreChanged;


    private readonly List<FloatingTimingFeedback> _pool =
        new List<FloatingTimingFeedback>();


    private void Awake()
    {
        PrewarmPool();
    }


    private void OnEnable()
    {
        if (_scoreChanged != null)
        {
            _scoreChanged.Raised += HandleScoreChanged;
        }
    }


    private void OnDisable()
    {
        if (_scoreChanged != null)
        {
            _scoreChanged.Raised -= HandleScoreChanged;
        }
    }


    private void OnValidate()
    {
        _initialPoolSize = Mathf.Max(1, _initialPoolSize);
        _maxPoolSize = Mathf.Max(_initialPoolSize, _maxPoolSize);
    }


    private void HandleScoreChanged(ScoreChange scoreChange)
    {
        if (scoreChange.TimingJudgement == TimingJudgement.None)
        {
            return;
        }

        if (!scoreChange.FeedbackPosition.HasValue)
        {
            return;
        }

        FloatingTimingFeedback feedback = GetAvailableFeedback();

        if (feedback == null)
        {
            return;
        }

        feedback.Show(
            scoreChange.TimingJudgement,
            scoreChange.FeedbackPosition.Value
        );
    }


    private void PrewarmPool()
    {
        if (_feedbackPrefab == null)
        {
            Debug.LogError(
                "[TimingFeedbackSystem] Feedback prefab is not assigned.",
                this
            );

            return;
        }

        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateFeedback();
        }
    }


    private FloatingTimingFeedback GetAvailableFeedback()
    {
        foreach (FloatingTimingFeedback feedback in _pool)
        {
            if (!feedback.gameObject.activeSelf)
            {
                return feedback;
            }
        }

        if (_pool.Count < _maxPoolSize)
        {
            return CreateFeedback();
        }

        Debug.LogWarning(
            "[TimingFeedbackSystem] Feedback pool reached maximum capacity.",
            this
        );

        return null;
    }


    private FloatingTimingFeedback CreateFeedback()
    {
        Transform parent = _poolRoot != null
            ? _poolRoot
            : transform;

        FloatingTimingFeedback feedback = Instantiate(
            _feedbackPrefab,
            parent
        );

        feedback.gameObject.SetActive(false);

        _pool.Add(feedback);

        return feedback;
    }
}