using System;
using System.IO;
using UnityEngine;

public sealed class MetricsLogger : MonoBehaviour
{
    [Header("File")]
    [SerializeField] private string _filePrefix = "Metrics";


    private string _filePath;


    private void Awake()
    {
        CreateLogFile();
    }


    public void LogInteraction(InteractionResult result)
    {
        string actualTime = result.ActualTime.HasValue
            ? result.ActualTime.Value.ToString("F4")
            : "N/A";

        string timingOffset = result.TimingOffset.HasValue
            ? result.TimingOffset.Value.ToString("F4")
            : "N/A";

        string timingError = result.TimingError.HasValue
            ? result.TimingError.Value.ToString("F4")
            : "N/A";

        string reactionTime = result.ReactionTime.HasValue
            ? result.ReactionTime.Value.ToString("F4")
            : "N/A";

        string spatialAccuracy = result.SpatialAccuracy.HasValue
            ? result.SpatialAccuracy.Value.ToString("F4")
            : "N/A";

        string directionAccuracy = result.DirectionAccuracy.HasValue
            ? result.DirectionAccuracy.Value.ToString("F4")
            : "N/A";

        string correctHand = result.UsedCorrectHand.HasValue
            ? result.UsedCorrectHand.Value.ToString()
            : "N/A";

        string line =
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | " +
            $"Minigame: {result.MinigameId} | " +
            $"Type: {result.InteractionType} | " +
            $"Outcome: {result.Outcome} | " +
            $"Difficulty: {result.Difficulty} | " +
            $"ExpectedTime: {result.ExpectedTime:F4} | " +
            $"ActualTime: {actualTime} | " +
            $"TimingOffset: {timingOffset} | " +
            $"TimingError: {timingError} | " +
            $"ReactionTime: {reactionTime} | " +
            $"SpatialAccuracy: {spatialAccuracy} | " +
            $"DirectionAccuracy: {directionAccuracy} | " +
            $"CorrectHand: {correctHand}";

        File.AppendAllText(_filePath, line + Environment.NewLine);
    }


    public void LogSummary(MetricsSystem metrics)
    {
        string summary =
            Environment.NewLine +
            "===== SESSION SUMMARY =====" + Environment.NewLine +
            $"Total Interactions: {metrics.TotalInteractions}" + Environment.NewLine +
            $"Successful: {metrics.SuccessfulInteractions}" + Environment.NewLine +
            $"Failed: {metrics.FailedInteractions}" + Environment.NewLine +
            $"Missed: {metrics.MissedInteractions}" + Environment.NewLine +
            $"Success Rate: {metrics.SuccessRate:F4}" + Environment.NewLine +
            $"Average Timing Error: {metrics.AverageTimingError:F4}" + Environment.NewLine +
            $"Average Reaction Time: {metrics.AverageReactionTime:F4}" + Environment.NewLine +
            $"Average Spatial Accuracy: {metrics.AverageSpatialAccuracy:F4}" + Environment.NewLine +
            $"Average Direction Accuracy: {metrics.AverageDirectionAccuracy:F4}" + Environment.NewLine +
            "===========================" + Environment.NewLine;

        File.AppendAllText(_filePath, summary);
    }


    private void CreateLogFile()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{_filePrefix}_{timestamp}.txt";

        _filePath = Path.Combine(Application.persistentDataPath, fileName);

        string header =
            "VR Metrics Log" + Environment.NewLine +
            $"Session Start: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine +
            $"File: {fileName}" + Environment.NewLine +
            Environment.NewLine;

        File.WriteAllText(_filePath, header);

        Debug.Log($"Metrics log created at: {_filePath}", this);
    }
}