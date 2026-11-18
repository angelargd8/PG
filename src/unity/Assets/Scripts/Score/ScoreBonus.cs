using UnityEngine;

public readonly struct ScoreBonus
{
    public int Points { get; }
    public Vector3 Position { get; }


    public ScoreBonus(int points, Vector3 position)
    {
        Points = points;
        Position = position;
    }
}