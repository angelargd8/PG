using System;
using UnityEngine;

public readonly struct InteractionResult
{
    public string MinigameId { get; }
    public string TargetId { get; }              // ¿Realmente lo voy a necesitar?

    public InteractionType InteractionType { get; }
    public InteractionOutcome Outcome { get; }
    public DifficultyLevel Difficulty { get; }

    public double ExpectedTime { get; }
    public double? ActualTime { get; }
    public double? TimingOffset { get; }
    public double? TimingError { get; }
    public double? ReactionTime { get; }

    public float? SpatialAccuracy { get; }
    public float? DirectionAccuracy { get; }
    public bool? UsedCorrectHand { get; }

    public bool WasSuccessful => Outcome == InteractionOutcome.Success;


    public InteractionResult(
        string minigameId,
        string targetId,
        InteractionType interactionType,
        InteractionOutcome outcome,
        DifficultyLevel difficulty,
        double expectedTime,
        double? actualTime = null,
        double? reactionTime = null,
        float? spatialAccuracy = null,
        float? directionAccuracy = null,
        bool? usedCorrectHand = null)
    {
        MinigameId = minigameId;
        TargetId = targetId;

        InteractionType = interactionType;
        Outcome = outcome;
        Difficulty = difficulty;

        ExpectedTime = expectedTime;
        ActualTime = actualTime;

        TimingOffset = actualTime.HasValue
            ? actualTime.Value - expectedTime
            : null;

        TimingError = TimingOffset.HasValue
            ? Math.Abs(TimingOffset.Value)
            : null;

        ReactionTime = reactionTime;

        SpatialAccuracy = spatialAccuracy.HasValue
            ? Mathf.Clamp01(spatialAccuracy.Value)
            : null;

        DirectionAccuracy = directionAccuracy.HasValue
            ? Mathf.Clamp01(directionAccuracy.Value)
            : null;

        UsedCorrectHand = usedCorrectHand;
    }
}