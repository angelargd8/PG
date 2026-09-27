using UnityEngine;

[CreateAssetMenu(
    fileName = "JeremyDifficultyConfig",
    menuName = "Scriptable Objects/Difficulty Config/Jeremy"
)]
public sealed class JeremyDifficultyConfigSO : ScriptableObject
{
    [SerializeField] private JeremyDifficultyProfile _easy;
    [SerializeField] private JeremyDifficultyProfile _normal;
    [SerializeField] private JeremyDifficultyProfile _hard;


    public JeremyDifficultyProfile GetProfile(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => _easy,
            DifficultyLevel.Hard => _hard,
            _ => _normal
        };
    }
}