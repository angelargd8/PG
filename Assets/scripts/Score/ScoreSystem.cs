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
        if (_songFinished != null) { _songFinished.Raised += HandleSongFinished; ExperienceDiagnostics.LogSubscription("added", this, _songFinished); }
        if (_interactionRegistered != null)
        {
            _interactionRegistered.Raised += HandleInteraction;
            ExperienceDiagnostics.LogSubscription("added", this, _interactionRegistered);
        }

        if (_scoreProfileChanged != null)
        {
            _scoreProfileChanged.Raised += HandleScoreProfileChanged;
            ExperienceDiagnostics.LogSubscription("added", this, _scoreProfileChanged);
        }

        if (_experienceReady != null)
        {
            _experienceReady.Raised += HandleExperienceReady;
            ExperienceDiagnostics.LogSubscription("added", this, _experienceReady);
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised += HandleMainMenuRequested;
            ExperienceDiagnostics.LogSubscription("added", this, _mainMenuRequested);
        }

        if (_experienceCompleted != null)
        {
            _experienceCompleted.Raised += HandleExperienceCompleted;
            ExperienceDiagnostics.LogSubscription("added", this, _experienceCompleted);
        }

        if (_scoreBonusAwarded != null)
        {
            _scoreBonusAwarded.Raised += HandleScoreBonus;
            ExperienceDiagnostics.LogSubscription("added", this, _scoreBonusAwarded);
        }
    }


    private void OnDisable()
    {
        if (_songFinished != null) { _songFinished.Raised -= HandleSongFinished; ExperienceDiagnostics.LogSubscription("removed", this, _songFinished); }
        if (_interactionRegistered != null)
        {
            _interactionRegistered.Raised -= HandleInteraction;
            ExperienceDiagnostics.LogSubscription("removed", this, _interactionRegistered);
        }

        if (_scoreProfileChanged != null)
        {
            _scoreProfileChanged.Raised -= HandleScoreProfileChanged;
            ExperienceDiagnostics.LogSubscription("removed", this, _scoreProfileChanged);
        }

        if (_experienceReady != null)
        {
            _experienceReady.Raised -= HandleExperienceReady;
            ExperienceDiagnostics.LogSubscription("removed", this, _experienceReady);
        }

        if (_mainMenuRequested != null)
        {
            _mainMenuRequested.Raised -= HandleMainMenuRequested;
            ExperienceDiagnostics.LogSubscription("removed", this, _mainMenuRequested);
        }

        if (_experienceCompleted != null)
        {
            _experienceCompleted.Raised -= HandleExperienceCompleted;
            ExperienceDiagnostics.LogSubscription("removed", this, _experienceCompleted);
        }

        if (_scoreBonusAwarded != null)
        {
            _scoreBonusAwarded.Raised -= HandleScoreBonus;
            ExperienceDiagnostics.LogSubscription("removed", this, _scoreBonusAwarded);
        }
    }


    private void HandleInteraction(InteractionResult result)
    {
        if (!_isExperienceActive || _songHasFinished)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Detailed, "SCORE", "interaction_ignored", this, null,
                "reason=" + (!_isExperienceActive ? "experience_inactive" : "song_finished"));
            return;
        }

        if (_currentProfile == null)
        {
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Detailed, "SCORE", "interaction_ignored", this, null,
                "reason=score_profile_missing");
            return;
        }

        ScoreEvaluation evaluation = _currentProfile.Evaluate(result);

        if (ExperienceDiagnostics.Level == ExperienceDiagnosticLevel.Detailed)
            ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Detailed, "SCORE", "interaction_evaluated", this, null,
                "minigame=" + ExperienceDiagnostics.Field("name", result.MinigameId) +
                " outcome=" + result.Outcome + " timing_error_s=" +
                (result.TimingError.HasValue ? result.TimingError.Value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "unavailable") +
                " score_delta=" + evaluation.ScoreDelta);

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
            _scoreChanged.RaiseEvent(scoreChange, this);
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
            _scoreChanged.RaiseEvent(scoreChange, this);
        }
    }


    private void HandleScoreProfileChanged(ScoreProfileSO profile)
    {
        _currentProfile = profile;
    }


    private void HandleExperienceReady()
    {
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "SCORE", "score_run_started", this, null,
            "reason=ExperienceReady");
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
            _scoreChanged.RaiseEvent(scoreChange, this);
        }
    }

    private void HandleExperienceCompleted()
    {
        if (!_isExperienceActive)
        {
            return;
        }

        _isExperienceActive = false;
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "SCORE", "score_run_completed", this, null,
            "final_score=" + CurrentScore);

        Debug.Log(
            $"[ScoreSystem] Run completed | Final Score: {CurrentScore}",
            this
        );

        if (_finalScoreSubmitted != null)
        {
            _finalScoreSubmitted.RaiseEvent(CurrentScore, this);
        }
    }

    private void HandleSongFinished() => _songHasFinished = true;
}
