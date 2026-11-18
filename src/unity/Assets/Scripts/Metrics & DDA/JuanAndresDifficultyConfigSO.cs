using UnityEngine;

[CreateAssetMenu(
    fileName = "JuanAndresDifficultyConfig",
    menuName = "Scriptable Objects/Difficulty Config/Juan Andres"
)]
public sealed class JuanAndresDifficultyConfigSO : ScriptableObject
{
    [SerializeField] private JuanAndresDifficultyProfile _easy;
    [SerializeField] private JuanAndresDifficultyProfile _normal;
    [SerializeField] private JuanAndresDifficultyProfile _hard;


    public JuanAndresDifficultyProfile GetProfile(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => _easy,
            DifficultyLevel.Hard => _hard,
            _ => _normal
        };
    }
}