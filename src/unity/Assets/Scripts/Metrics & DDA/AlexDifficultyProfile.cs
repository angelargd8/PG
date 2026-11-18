using UnityEngine;

public enum AlexThrowPattern
{
    Wide = 0,
    Narrow = 1,
    LeftHighRightLow = 2,
    LeftLowRightHigh = 3,
    BothHigh = 4,
    BothLow = 5
}

[System.Serializable]
public sealed class AlexDifficultyProfile
{
    [Tooltip("Cada cuantos beats se lanza un par de objetos.")]
    [Min(1)] [SerializeField] private int _throwEveryBeats = 2;
    [Min(0.1f)] [SerializeField] private float _horizontalMultiplier = 0.6f;

    [Header("Low Targets")]
    [SerializeField] private bool _limitLowTargets = true;
    [Tooltip("Descenso maximo bajo el centro de impacto, incluso en beats intensos.")]
    [Min(0f)] [SerializeField] private float _maxDrop = 0.2f;

    [Header("Pattern Sequences")]
    [SerializeField] private AlexThrowPattern[] _patterns =
    {
        AlexThrowPattern.Wide, AlexThrowPattern.LeftHighRightLow,
        AlexThrowPattern.Narrow, AlexThrowPattern.LeftLowRightHigh, AlexThrowPattern.Wide
    };
    [SerializeField] private AlexThrowPattern[] _intensePatterns =
    {
        AlexThrowPattern.LeftHighRightLow, AlexThrowPattern.LeftLowRightHigh,
        AlexThrowPattern.BothHigh, AlexThrowPattern.Wide,
        AlexThrowPattern.BothLow, AlexThrowPattern.Narrow
    };

    public int ThrowEveryBeats => Mathf.Max(1, _throwEveryBeats);
    public float HorizontalMultiplier => Mathf.Max(0.1f, _horizontalMultiplier);

    public AlexDifficultyProfile() { }

    public AlexDifficultyProfile(int throwEveryBeats, float horizontalMultiplier,
        bool limitLowTargets, float maxDrop, AlexThrowPattern[] patterns)
    {
        _throwEveryBeats = throwEveryBeats;
        _horizontalMultiplier = horizontalMultiplier;
        _limitLowTargets = limitLowTargets;
        _maxDrop = maxDrop;
        _patterns = patterns;
    }

    public float GetDownwardOffset(float verticalOffset)
    {
        float offset = Mathf.Max(0f, verticalOffset);
        return _limitLowTargets ? Mathf.Min(offset, Mathf.Max(0f, _maxDrop)) : offset;
    }

    public AlexThrowPattern GetPattern(int index, bool intense)
    {
        var sequence = intense && _intensePatterns != null && _intensePatterns.Length > 0
            ? _intensePatterns : _patterns;
        if (sequence == null || sequence.Length == 0) return AlexThrowPattern.Wide;
        return sequence[Mathf.Max(0, index) % sequence.Length];
    }
}
