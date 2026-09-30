using UnityEngine;

public enum ShipiMovePointType
{
    Entrance = 0,
    Transit = 1,
    Cutting = 2,
    Exit = 3,
    End = 4
}

[DisallowMultipleComponent]
public sealed class ShipiMovePoint : MonoBehaviour
{
    [SerializeField] private ShipiMovePointType _type;


    public ShipiMovePointType Type => _type;
    public Vector3 Position => transform.position;
}