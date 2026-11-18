using UnityEngine;

[CreateAssetMenu(
    fileName = "DannielDifficultyConfig",
    menuName = "Scriptable Objects/Difficulty Config/Danniel"
)]
public sealed class DannielDifficultyConfigSO : ScriptableObject
{
    [SerializeField] private DannielDifficultyProfile _easy;
    [SerializeField] private DannielDifficultyProfile _normal;
    [SerializeField] private DannielDifficultyProfile _hard;


    public DannielDifficultyProfile GetProfile(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => _easy,
            DifficultyLevel.Hard => _hard,
            _ => _normal
        };
    }
}