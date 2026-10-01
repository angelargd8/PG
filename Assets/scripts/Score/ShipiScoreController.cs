using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiScoreController :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("Score")]
    [SerializeField]
    private ScoreProfileSO _scoreProfile;

    [SerializeField]
    private ScoreProfileEventChannelSO
        _scoreProfileChanged;


    public void BeginExperience()
    {
        if (_scoreProfile == null)
        {
            Debug.LogError(
                "[ShipiScoreController] " +
                "No se asignó ScoreProfile.",
                this
            );

            return;
        }

        if (_scoreProfileChanged == null)
        {
            Debug.LogError(
                "[ShipiScoreController] " +
                "No se asignó ScoreProfileChanged.",
                this
            );

            return;
        }

        _scoreProfileChanged.RaiseEvent(
            _scoreProfile
        );

        Debug.Log(
            "[ShipiScoreController] " +
            "Score profile activado.",
            this
        );
    }


    public void EndExperience()
    {
    }
}