using UnityEngine;

[CreateAssetMenu(
    fileName = "JoaquinDifficultyConfig",
    menuName = "Scriptable Objects/Difficulty Config/Joaquin"
)]
public sealed class JoaquinDifficultyConfigSO : ScriptableObject
{
    [SerializeField] private JoaquinDifficultyProfile _easy;
    [SerializeField] private JoaquinDifficultyProfile _normal;
    [SerializeField] private JoaquinDifficultyProfile _hard;


    public JoaquinDifficultyProfile GetProfile(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => _easy,
            DifficultyLevel.Hard => _hard,
            _ => _normal
        };
    }
}