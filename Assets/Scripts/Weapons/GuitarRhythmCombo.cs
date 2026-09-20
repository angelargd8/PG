using System;

public enum GuitarHitGrade
{
    OffBeat,
    Good,
    Perfect
}

public readonly struct GuitarRhythmHit
{
    public GuitarHitGrade Grade { get; }
    public int BeatIndex { get; }
    public double ExpectedTime { get; }
    public double ActualTime { get; }
    public double TimingOffset => ActualTime - ExpectedTime;
    public bool IsOnBeat => Grade != GuitarHitGrade.OffBeat;
    public bool AddedToCombo { get; }
    public int PointsAwarded { get; }
    public int Combo { get; }
    public int Multiplier { get; }
    public int TotalScore { get; }

    public GuitarRhythmHit(GuitarHitGrade grade, int beatIndex, double expectedTime,
        double actualTime, bool addedToCombo, int pointsAwarded, int combo, int multiplier, int totalScore)
    {
        Grade = grade;
        BeatIndex = beatIndex;
        ExpectedTime = expectedTime;
        ActualTime = actualTime;
        AddedToCombo = addedToCombo;
        PointsAwarded = pointsAwarded;
        Combo = combo;
        Multiplier = multiplier;
        TotalScore = totalScore;
    }
}

/// <summary>Scores contacts against the closest musical beat, including early hits.</summary>
public sealed class GuitarRhythmCombo
{
    // Float Inspector settings need a small tolerance at exact window boundaries.
    private const double BoundaryEpsilon = 0.0000001;
    private double lastRewardedBeatTime = double.NegativeInfinity;

    public int Combo { get; private set; }
    public int BestCombo { get; private set; }
    public int Multiplier { get; private set; } = 1;
    public int Score { get; private set; }

    public void Reset()
    {
        Combo = 0;
        BestCombo = 0;
        Multiplier = 1;
        Score = 0;
        lastRewardedBeatTime = double.NegativeInfinity;
    }

    public bool TryEvaluate(BeatMapSO map, double songTime, double perfectWindow, double goodWindow,
        int perfectPoints, int goodPoints, int hitsPerMultiplier, int maxMultiplier, out GuitarRhythmHit hit)
    {
        hit = default;
        if (map == null || map.Beats.Count == 0 || double.IsNaN(songTime) || double.IsInfinity(songTime))
        {
            return false;
        }

        int index = FindClosestBeat(map, songTime);
        double expectedTime = map.Beats[index].Time;
        double error = Math.Abs(songTime - expectedTime);
        perfectWindow = Math.Max(0.0, perfectWindow);
        goodWindow = Math.Max(perfectWindow, goodWindow);

        GuitarHitGrade grade = error <= perfectWindow + BoundaryEpsilon ? GuitarHitGrade.Perfect :
            error <= goodWindow + BoundaryEpsilon ? GuitarHitGrade.Good : GuitarHitGrade.OffBeat;
        bool addedToCombo = false;
        int points = 0;

        if (grade == GuitarHitGrade.OffBeat)
        {
            Combo = 0;
            Multiplier = 1;
        }
        else if (expectedTime > lastRewardedBeatTime)
        {
            // A sweep can damage multiple enemies, but rewards this musical beat only once.
            lastRewardedBeatTime = expectedTime;
            addedToCombo = true;
            Combo++;
            BestCombo = Math.Max(BestCombo, Combo);
            Multiplier = Math.Min(Math.Max(1, maxMultiplier), 1 + Combo / Math.Max(1, hitsPerMultiplier));
            points = Math.Max(0, grade == GuitarHitGrade.Perfect ? perfectPoints : goodPoints) * Multiplier;
            Score += points;
        }

        hit = new GuitarRhythmHit(grade, index, expectedTime, songTime, addedToCombo,
            points, Combo, Multiplier, Score);
        return true;
    }

    private static int FindClosestBeat(BeatMapSO map, double songTime)
    {
        int low = 0;
        int high = map.Beats.Count;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (map.Beats[middle].Time < songTime)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        if (low == 0)
        {
            return 0;
        }

        if (low == map.Beats.Count)
        {
            return low - 1;
        }

        return songTime - map.Beats[low - 1].Time <= map.Beats[low].Time - songTime ? low - 1 : low;
    }
}
