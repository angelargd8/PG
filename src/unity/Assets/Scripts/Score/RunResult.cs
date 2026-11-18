public readonly struct RunResult
{
    public ScoreRunContext Context { get; }

    public int FinalScore { get; }
    public int PreviousHighScore { get; }
    public int HighScore { get; }

    public bool IsNewHighScore { get; }


    public RunResult(
        ScoreRunContext context,
        int finalScore,
        int previousHighScore,
        int highScore,
        bool isNewHighScore
    ){
        Context = context;

        FinalScore = finalScore;
        PreviousHighScore = previousHighScore;
        HighScore = highScore;

        IsNewHighScore = isNewHighScore;
    }
}