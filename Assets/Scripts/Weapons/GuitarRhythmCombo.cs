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
    public int Combo { get; }
    public int Multiplier { get; }


    public GuitarRhythmHit(
        GuitarHitGrade grade,
        int beatIndex,
        double expectedTime,
        double actualTime,
        bool addedToCombo,
        int combo,
        int multiplier)
    {
        Grade = grade;
        BeatIndex = beatIndex;
        ExpectedTime = expectedTime;
        ActualTime = actualTime;
        AddedToCombo = addedToCombo;
        Combo = combo;
        Multiplier = multiplier;
    }
}

public sealed class GuitarRhythmCombo
{
    private const double BoundaryEpsilon = 0.0000001;

    private double _lastRewardedBeatTime =
        double.NegativeInfinity;


    public int Combo { get; private set; }
    public int BestCombo { get; private set; }
    public int Multiplier { get; private set; } = 1;


    public void Reset()
    {
        Combo = 0;
        BestCombo = 0;
        Multiplier = 1;

        _lastRewardedBeatTime =
            double.NegativeInfinity;
    }


    public bool TryEvaluate(
        BeatMapSO map,
        double songTime,
        double perfectWindow,
        double goodWindow,
        int hitsPerMultiplier,
        int maxMultiplier,
        out GuitarRhythmHit hit)
    {
        hit = default;

        if (map == null ||
            map.Beats.Count == 0 ||
            double.IsNaN(songTime) ||
            double.IsInfinity(songTime))
        {
            return false;
        }

        int index =
            FindClosestBeat(map, songTime);

        double expectedTime =
            map.Beats[index].Time;

        double error =
            Math.Abs(songTime - expectedTime);

        perfectWindow =
            Math.Max(0.0, perfectWindow);

        goodWindow =
            Math.Max(perfectWindow, goodWindow);

        GuitarHitGrade grade =
            error <= perfectWindow + BoundaryEpsilon
                ? GuitarHitGrade.Perfect
                : error <= goodWindow + BoundaryEpsilon
                    ? GuitarHitGrade.Good
                    : GuitarHitGrade.OffBeat;

        bool addedToCombo = false;

        if (grade == GuitarHitGrade.OffBeat)
        {
            Combo = 0;
            Multiplier = 1;
        }
        else if (expectedTime > _lastRewardedBeatTime)
        {
            _lastRewardedBeatTime =
                expectedTime;

            addedToCombo = true;

            Combo++;

            BestCombo =
                Math.Max(BestCombo, Combo);

            Multiplier =
                Math.Min(
                    Math.Max(1, maxMultiplier),
                    1 + Combo / Math.Max(1, hitsPerMultiplier)
                );
        }

        hit = new GuitarRhythmHit(
            grade,
            index,
            expectedTime,
            songTime,
            addedToCombo,
            Combo,
            Multiplier
        );

        return true;
    }


    private static int FindClosestBeat(BeatMapSO map, double songTime)
    {
        int low = 0;
        int high = map.Beats.Count;

        while (low < high)
        {
            int middle =
                low + (high - low) / 2;

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

        return songTime - map.Beats[low - 1].Time <=
            map.Beats[low].Time - songTime
                ? low - 1
                : low;
    }
}