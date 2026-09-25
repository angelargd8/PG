public readonly struct ScoreRunContext
{
    public string ScoreId { get; }
    public string DisplayName { get; }
    public bool IsFullExperience { get; }


    public ScoreRunContext(
        string scoreId,
        string displayName,
        bool isFullExperience
    )
    {
        ScoreId = scoreId;
        DisplayName = displayName;
        IsFullExperience = isFullExperience;
    }


    public static ScoreRunContext Create(
        ExperienceDefinitionSO experience,
        ExperienceSceneDefinitionSO scene,
        bool playFullSequence
    )
    {
        if (playFullSequence)
        {
            return new ScoreRunContext(
                $"{experience.ExperienceId}:full",
                experience.DisplayName,
                true
            );
        }

        return new ScoreRunContext(
            $"{experience.ExperienceId}:{scene.SceneId}",
            scene.DisplayName,
            false
        );
    }
}