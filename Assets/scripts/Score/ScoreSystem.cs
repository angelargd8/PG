using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScoreSystem : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;
    [SerializeField] private ScoreProfileEventChannelSO _scoreProfileChanged;
    [SerializeField] private ScoreChangedEventChannelSO _scoreChanged;
    [SerializeField] private VoidEventChannelSO _experienceReady;
    [SerializeField] private VoidEventChannelSO _mainMenuRequested;
    [SerializeField] private VoidEventChannelSO _experienceCompleted;
    [SerializeField] private VoidEventChannelSO _songFinished;
    [SerializeField] private FinalScoreEventChannelSO _finalScoreSubmitted;
    [SerializeField] private ScoreBonusEventChannelSO _scoreBonusAwarded;


    private ScoreProfileSO _currentProfile;
    private bool _isExperienceActive;
    private bool _songHasFinished;


    public int CurrentScore { get; private set; }


    private void OnEnable()
    {
        if (_songFinished != null) _songFinished.Raised += HandleSongFinished;
        if (_interactionRegistered != null)
        {
            _interactionRegistered.Raised += HandleInteraction;
        }

        if (_scoreProfileChanged != null)
        {
            _scoreProfileChanged.Raised += HandleScoreProfileChanged;
        }

        if (_experienceReady != null)
        {
            _experienceReady.Raised += HandleExperienceReady;
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised += HandleMainMenuRequested;
        }

        if (_experienceCompleted != null)
        {
            _experienceCompleted.Raised += HandleExperienceCompleted;
        }

        if (_scoreBonusAwarded != null)
        {
            _scoreBonusAwarded.Raised += HandleScoreBonus;
        }
    }


    private void OnDisable()
    {
        if (_songFinished != null) _songFinished.Raised -= HandleSongFinished;
        if (_interactionRegistered != null)
        {
            _interactionRegistered.Raised -= HandleInteraction;
        }

        if (_scoreProfileChanged != null)
        {
            _scoreProfileChanged.Raised -= HandleScoreProfileChanged;
        }

        if (_experienceReady != null)
        {
            _experienceReady.Raised -= HandleExperienceReady;
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised -= HandleMainMenuRequested;
        }

        if (_experienceCompleted != null)
        {
            _experienceCompleted.Raised -= HandleExperienceCompleted;
        }

        if (_scoreBonusAwarded != null)
        {
            _scoreBonusAwarded.Raised -= HandleScoreBonus;
        }
    }


    private void HandleInteraction(InteractionResult result)
    {
        if (!_isExperienceActive || _songHasFinished)
        {
            return;
        }

        if (_currentProfile == null)
        {
            return;
        }

        ScoreEvaluation evaluation = _currentProfile.Evaluate(result);

        ApplyScore(evaluation, result.FeedbackPosition);
    }


    private void ApplyScore(ScoreEvaluation evaluation, Vector3? feedbackPosition)
    {
        int previousScore = CurrentScore;

        CurrentScore = Mathf.Max(
            0,
            CurrentScore + evaluation.ScoreDelta
        );

        int actualDelta = CurrentScore - previousScore;

        ScoreChange scoreChange = new ScoreChange(
            previousScore,
            CurrentScore,
            actualDelta,
            evaluation.TimingJudgement,
            feedbackPosition
        );

        if (_scoreChanged != null)
        {
            _scoreChanged.RaiseEvent(scoreChange);
        }
    }
    

    private void HandleScoreBonus(ScoreBonus bonus)
    {
        if (!_isExperienceActive || _songHasFinished)
        {
            return;
        }

        if (bonus.Points <= 0)
        {
            return;
        }

        int previousScore = CurrentScore;

        CurrentScore += bonus.Points;

        ScoreChange scoreChange = new ScoreChange(
            previousScore,
            CurrentScore,
            bonus.Points,
            TimingJudgement.None,
            bonus.Position
        );

        if (_scoreChanged != null)
        {
            _scoreChanged.RaiseEvent(scoreChange);
        }
    }


    private void HandleScoreProfileChanged(ScoreProfileSO profile)
    {
        _currentProfile = profile;
    }


    private void HandleExperienceReady()
    {
        _songHasFinished = false;
        _isExperienceActive = true;
        ResetScore();
    }


    private void HandleMainMenuRequested()
    {
        _isExperienceActive = false;
        _currentProfile = null;
    }


    private void ResetScore()
    {
        CurrentScore = 0;

        ScoreChange scoreChange = new ScoreChange(
            0,
            0,
            0,
            TimingJudgement.None,
            null
        );

        if (_scoreChanged != null)
        {
            _scoreChanged.RaiseEvent(scoreChange);
        }
    }

    private void HandleExperienceCompleted()
    {
        if (!_isExperienceActive)
        {
            return;
        }

        _isExperienceActive = false;

        Debug.Log(
            $"[ScoreSystem] Run completed | Final Score: {CurrentScore}",
            this
        );

        if (_finalScoreSubmitted != null)
        {
            _finalScoreSubmitted.RaiseEvent(CurrentScore);
        }
    }

    private void HandleSongFinished() => _songHasFinished = true;
}
