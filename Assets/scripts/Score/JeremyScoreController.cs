using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyScoreController :
    MonoBehaviour,
    IExperienceRuntime
{
    [SerializeField] private ScoreProfileSO _scoreProfile;
    [SerializeField] private ScoreProfileEventChannelSO _scoreProfileChanged;


    public void BeginExperience()
    {
        if (_scoreProfile == null)
        {
            Debug.LogError(
                "[JeremyScoreController] No se asignó ScoreProfile.",
                this
            );

            return;
        }

        if (_scoreProfileChanged == null)
        {
            Debug.LogError(
                "[JeremyScoreController] No se asignó ScoreProfileChanged.",
                this
            );

            return;
        }

        _scoreProfileChanged.RaiseEvent(_scoreProfile);
    }


    public void EndExperience()
    {
    }
}