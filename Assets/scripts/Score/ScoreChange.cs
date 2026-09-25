public readonly struct ScoreChange
{
    public int PreviousScore { get; }
    public int CurrentScore { get; }
    public int Delta { get; }
    public TimingJudgement TimingJudgement { get; }


    public ScoreChange(
        int previousScore,
        int currentScore,
        int delta,
        TimingJudgement timingJudgement
    ){
        PreviousScore = previousScore;
        CurrentScore = currentScore;
        Delta = delta;
        TimingJudgement = timingJudgement;
    }
}