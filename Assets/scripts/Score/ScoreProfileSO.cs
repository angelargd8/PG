using UnityEngine;

[CreateAssetMenu(
    fileName = "ScoreProfile",
    menuName = "Scriptable Objects/Score/Score Profile"
)]
public sealed class ScoreProfileSO : ScriptableObject
{
    [Header("Outcome Points")]
    [SerializeField] private int _successPoints = 100;
    [SerializeField] private int _failedPoints = -50;
    [SerializeField] private int _missedPoints = -75;


    [Header("Timing Thresholds")]
    [Min(0f)]
    [SerializeField] private float _perfectTimingThreshold = 0.08f;

    [Min(0f)]
    [SerializeField] private float _goodTimingThreshold = 0.16f;

    [Min(0f)]
    [SerializeField] private float _niceTimingThreshold = 0.25f;


    [Header("Timing Bonus")]
    [SerializeField] private int _perfectBonus = 50;
    [SerializeField] private int _goodBonus = 30;
    [SerializeField] private int _niceBonus = 15;


    [Header("Difficulty Multiplier")]
    [Min(0f)]
    [SerializeField] private float _easyMultiplier = 1f;

    [Min(0f)]
    [SerializeField] private float _normalMultiplier = 1.25f;

    [Min(0f)]
    [SerializeField] private float _hardMultiplier = 1.5f;


    private void OnValidate()
    {
        _perfectTimingThreshold =
            Mathf.Max(0f, _perfectTimingThreshold);

        _goodTimingThreshold =
            Mathf.Max(_perfectTimingThreshold, _goodTimingThreshold);

        _niceTimingThreshold =
            Mathf.Max(_goodTimingThreshold, _niceTimingThreshold);
    }


    public ScoreEvaluation Evaluate(InteractionResult result)
    {
        int scoreDelta = GetOutcomePoints(result.Outcome);

        TimingJudgement timingJudgement =
            result.WasSuccessful
                ? GetTimingJudgement(result.TimingError)
                : TimingJudgement.None;

        if (result.WasSuccessful)
        {
            scoreDelta += GetTimingBonus(timingJudgement);

            float difficultyMultiplier =
                GetDifficultyMultiplier(result.Difficulty);

            scoreDelta =
                Mathf.RoundToInt(scoreDelta * difficultyMultiplier);
        }

        return new ScoreEvaluation(scoreDelta, timingJudgement);
    }


    private int GetOutcomePoints(InteractionOutcome outcome)
    {
        return outcome switch
        {
            InteractionOutcome.Success => _successPoints,
            InteractionOutcome.Failed => _failedPoints,
            InteractionOutcome.Missed => _missedPoints,
            _ => 0
        };
    }


    private TimingJudgement GetTimingJudgement(double? timingError)
    {
        if (!timingError.HasValue)
        {
            return TimingJudgement.None;
        }

        if (timingError.Value <= _perfectTimingThreshold)
        {
            return TimingJudgement.Perfect;
        }

        if (timingError.Value <= _goodTimingThreshold)
        {
            return TimingJudgement.Good;
        }

        if (timingError.Value <= _niceTimingThreshold)
        {
            return TimingJudgement.Nice;
        }

        return TimingJudgement.None;
    }


    private int GetTimingBonus(TimingJudgement judgement)
    {
        return judgement switch
        {
            TimingJudgement.Perfect => _perfectBonus,
            TimingJudgement.Good => _goodBonus,
            TimingJudgement.Nice => _niceBonus,
            _ => 0
        };
    }


    private float GetDifficultyMultiplier(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => _easyMultiplier,
            DifficultyLevel.Hard => _hardMultiplier,
            _ => _normalMultiplier
        };
    }
}