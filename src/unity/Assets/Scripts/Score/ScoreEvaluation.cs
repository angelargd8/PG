public readonly struct ScoreEvaluation
{
    public int ScoreDelta { get; }
    public TimingJudgement TimingJudgement { get; }


    public ScoreEvaluation(int scoreDelta, TimingJudgement timingJudgement)
    {
        ScoreDelta = scoreDelta;
        TimingJudgement = timingJudgement;
    }
}