using UnityEngine;

[System.Serializable]
public sealed class ShipiDifficultyProfile
{
    [Min(1)]
    [SerializeField] private int _moveEveryNBeats;


    public int MoveEveryNBeats => _moveEveryNBeats;
}