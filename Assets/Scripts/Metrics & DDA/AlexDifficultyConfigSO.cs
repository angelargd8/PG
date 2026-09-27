using UnityEngine;

[CreateAssetMenu(
    fileName = "AlexDifficultyConfig",
    menuName = "Scriptable Objects/Difficulty Config/Alex"
)]
public sealed class AlexDifficultyConfigSO : ScriptableObject
{
    [SerializeField] private AlexDifficultyProfile _easy = new AlexDifficultyProfile(
        4, 0.35f, true, 0.1f, new[]
        {
            AlexThrowPattern.Wide, AlexThrowPattern.LeftHighRightLow,
            AlexThrowPattern.Wide, AlexThrowPattern.LeftLowRightHigh
        });

    [SerializeField] private AlexDifficultyProfile _normal = new AlexDifficultyProfile();

    [SerializeField] private AlexDifficultyProfile _hard = new AlexDifficultyProfile(
        1, 1f, false, 0.5f, new[]
        {
            AlexThrowPattern.LeftHighRightLow, AlexThrowPattern.LeftLowRightHigh,
            AlexThrowPattern.Narrow, AlexThrowPattern.BothHigh,
            AlexThrowPattern.Wide, AlexThrowPattern.BothLow
        });

    public AlexDifficultyProfile GetProfile(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => _easy,
            DifficultyLevel.Hard => _hard,
            _ => _normal
        };
    }
}
