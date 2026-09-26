using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HighScoreSystem : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private ScoreRunEventChannelSO _scoreRunStarted;
    [SerializeField] private FinalScoreEventChannelSO _finalScoreSubmitted;
    [SerializeField] private RunResultEventChannelSO _runResultReady;


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

        if (_finalScoreSubmitted != null)
        {
            _finalScoreSubmitted.Raised += HandleFinalScoreSubmitted;
        }
    }


    private void OnDisable()
    {
        if (_scoreRunStarted != null)
        {
            _scoreRunStarted.Raised -= HandleScoreRunStarted;
        }

        if (_finalScoreSubmitted != null)
        {
            _finalScoreSubmitted.Raised -= HandleFinalScoreSubmitted;
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


    private void HandleScoreRunStarted(ScoreRunContext context)
    {
        _currentRun = context;

        // Debug.Log(
        //     $"[HighScoreSystem] Run started | " +
        //     $"Id: {context.ScoreId} | " +
        //     $"Name: {context.DisplayName} | " +
        //     $"HighScore: {GetHighScore(context)}",
        //     this
        // );
    }
    

    private void HandleFinalScoreSubmitted(int finalScore)
    {
        if (!_currentRun.HasValue)
        {
            Debug.LogWarning(
                "[HighScoreSystem] Final score received without active run.",
                this
            );

            return;
        }

        ScoreRunContext context = _currentRun.Value;

        int previousHighScore =
            GetHighScore(context.ScoreId);

        bool isNewHighScore =
            finalScore > previousHighScore;

        if (isNewHighScore)
        {
            _highScores[context.ScoreId] = finalScore;
        }

        int highScore =
            GetHighScore(context.ScoreId);

        RunResult result = new RunResult(
            context,
            finalScore,
            previousHighScore,
            highScore,
            isNewHighScore
        );

        Debug.Log(
            $"[HighScoreSystem] Run result | " +
            $"Id: {context.ScoreId} | " +
            $"Score: {finalScore} | " +
            $"Previous High Score: {previousHighScore} | " +
            $"High Score: {highScore} | " +
            $"New High Score: {isNewHighScore}",
            this
        );

        if (_runResultReady != null)
        {
            _runResultReady.RaiseEvent(result);
        }
    }
}