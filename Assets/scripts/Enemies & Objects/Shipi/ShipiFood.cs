using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiFood : MonoBehaviour
{
    public ShipiFoodDefinitionSO Definition { get; private set; }
    public ShipiCutDirection ExpectedDirection { get; private set; }
    public DifficultyLevel Difficulty { get; private set; }

    public ShipiMovePoint CurrentPoint { get; private set; }
    public int CurrentPointIndex { get; private set; }

    public double ExpectedCutTime { get; private set; }

    public bool IsResolved { get; private set; }


    public void Initialize(
        ShipiFoodDefinitionSO definition,
        ShipiCutDirection expectedDirection,
        DifficultyLevel difficulty)
    {
        Definition = definition;
        ExpectedDirection = expectedDirection;
        Difficulty = difficulty;

        CurrentPoint = null;
        CurrentPointIndex = -1;

        ExpectedCutTime = 0d;

        IsResolved = false;
    }


    public void MoveToPoint(
        ShipiMovePoint point,
        int pointIndex,
        double beatTime)
    {
        if (point == null)
        {
            return;
        }

        CurrentPoint = point;
        CurrentPointIndex = pointIndex;

        transform.position =
            point.Position;

        if (point.Type ==
            ShipiMovePointType.Cutting)
        {
            ExpectedCutTime = beatTime;
        }
    }


    public void MarkResolved()
    {
        IsResolved = true;
    }
}