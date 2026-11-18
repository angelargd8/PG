using UnityEngine;

public readonly struct ScoreChange
{
    public int PreviousScore { get; }
    public int CurrentScore { get; }
    public int Delta { get; }
    public TimingJudgement TimingJudgement { get; }
    public Vector3? FeedbackPosition { get; }


    public ScoreChange(
        int previousScore,
        int currentScore,
        int delta,
        TimingJudgement timingJudgement,
        Vector3? feedbackPosition = null
    ){
        PreviousScore = previousScore;
        CurrentScore = currentScore;
        Delta = delta;
        TimingJudgement = timingJudgement;
        FeedbackPosition = feedbackPosition;
    }
}