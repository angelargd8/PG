using UnityEngine;

[DisallowMultipleComponent]
public sealed class TimingFeedbackSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloatingTimingFeedback _feedbackPrefab;

    [Header("Events")]
    [SerializeField] private ScoreChangedEventChannelSO _scoreChanged;


    private void OnEnable()
    {
        if (_scoreChanged != null)
        {
            _scoreChanged.Raised += HandleScoreChanged;
        }
    }


    private void OnDisable()
    {
        if (_scoreChanged != null)
        {
            _scoreChanged.Raised -= HandleScoreChanged;
        }
    }


    private void HandleScoreChanged(ScoreChange scoreChange)
    {
        Debug.Log(
            $"[TimingFeedbackSystem] ScoreChanged received | " +
            $"Judgement: {scoreChange.TimingJudgement} | " +
            $"HasPosition: {scoreChange.FeedbackPosition.HasValue}",
            this
        );

        if (scoreChange.TimingJudgement == TimingJudgement.None)
        {
            Debug.Log(
                "[TimingFeedbackSystem] Ignored because judgement is None.",
                this
            );

            return;
        }

        if (!scoreChange.FeedbackPosition.HasValue)
        {
            Debug.LogWarning(
                "[TimingFeedbackSystem] Ignored because FeedbackPosition is null.",
                this
            );

            return;
        }

        if (_feedbackPrefab == null)
        {
            Debug.LogError(
                "[TimingFeedbackSystem] Feedback prefab is not assigned.",
                this
            );

            return;
        }

        Debug.Log(
            $"[TimingFeedbackSystem] Creating feedback | " +
            $"Judgement: {scoreChange.TimingJudgement} | " +
            $"Position: {scoreChange.FeedbackPosition.Value}",
            this
        );

        FloatingTimingFeedback feedback = Instantiate(
            _feedbackPrefab,
            scoreChange.FeedbackPosition.Value,
            Quaternion.identity
        );

        feedback.Show(
            scoreChange.TimingJudgement,
            scoreChange.FeedbackPosition.Value
        );
    }
}