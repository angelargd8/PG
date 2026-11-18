public readonly struct ExperienceRequest
{
    public ExperienceDefinitionSO Experience
    {
        get;
    }

    public int StartSceneIndex
    {
        get;
    }

    public bool PlayFullSequence
    {
        get;
    }


    public ExperienceRequest(
        ExperienceDefinitionSO experience,
        int startSceneIndex,
        bool playFullSequence
    )
    {
        Experience =
            experience;

        StartSceneIndex =
            startSceneIndex;

        PlayFullSequence =
            playFullSequence;
    }
}