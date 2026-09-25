using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HighScoreSystem : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private ScoreRunEventChannelSO _scoreRunStarted;


    private readonly Dictionary<string, int> _highScores =
        new Dictionary<string, int>();

    private ScoreRunContext? _currentRun;


    public ScoreRunContext? CurrentRun => _currentRun;


    private void OnEnable()
    {
        if (_scoreRunStarted != null)
        {
            _scoreRunStarted.Raised += HandleScoreRunStarted;
        }
    }


    private void OnDisable()
    {
        if (_scoreRunStarted != null)
        {
            _scoreRunStarted.Raised -= HandleScoreRunStarted;
        }
    }


    public int GetHighScore(string scoreId)
    {
        if (string.IsNullOrEmpty(scoreId))
        {
            return 0;
        }

        return _highScores.TryGetValue(scoreId, out int highScore)
            ? highScore
            : 0;
    }


    public int GetHighScore(ScoreRunContext context)
    {
        return GetHighScore(context.ScoreId);
    }


    public bool SubmitCurrentScore(int score)
    {
        if (!_currentRun.HasValue)
        {
            Debug.LogWarning(
                "[HighScoreSystem] No active ScoreRunContext.",
                this
            );

            return false;
        }

        ScoreRunContext context = _currentRun.Value;

        int previousHighScore =
            GetHighScore(context.ScoreId);

        if (score <= previousHighScore)
        {
            Debug.Log(
                $"[HighScoreSystem] Score {score} did not beat " +
                $"high score {previousHighScore} for {context.ScoreId}.",
                this
            );

            return false;
        }

        _highScores[context.ScoreId] = score;

        Debug.Log(
            $"[HighScoreSystem] New high score | " +
            $"{context.ScoreId}: {previousHighScore} -> {score}",
            this
        );

        return true;
    }


    private void HandleScoreRunStarted(ScoreRunContext context)
    {
        _currentRun = context;

        Debug.Log(
            $"[HighScoreSystem] Run started | " +
            $"Id: {context.ScoreId} | " +
            $"Name: {context.DisplayName} | " +
            $"HighScore: {GetHighScore(context)}",
            this
        );
    }
}