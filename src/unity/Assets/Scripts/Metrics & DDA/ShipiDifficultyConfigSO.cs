using UnityEngine;

[CreateAssetMenu(
    fileName = "ShipiDifficultyConfig",
    menuName = "Scriptable Objects/Difficulty Config/Shipi"
)]
public sealed class ShipiDifficultyConfigSO : ScriptableObject
{
    [SerializeField] private ShipiDifficultyProfile _easy;
    [SerializeField] private ShipiDifficultyProfile _normal;
    [SerializeField] private ShipiDifficultyProfile _hard;


    public ShipiDifficultyProfile GetProfile(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => _easy,
            DifficultyLevel.Hard => _hard,
            _ => _normal
        };
    }
}