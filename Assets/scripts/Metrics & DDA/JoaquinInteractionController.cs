using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class JoaquinInteractionController :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("Gameplay")]
    [SerializeField] private GuitarDamage _guitarDamage;

    [Header("Metrics")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;
    [SerializeField] private DifficultyLevelEventChannelSO _difficultyChanged;

    [Header("Score")]
    [SerializeField] private ScoreProfileSO _scoreProfile;
    [SerializeField] private ScoreProfileEventChannelSO _scoreProfileChanged;
    [SerializeField] private ScoreBonusEventChannelSO _scoreBonusAwarded;

    [Header("Multi Hit")]
    [Tooltip("Evita registrar varios Success cuando un mismo barrido golpea varios enemigos fuera del beat.")]
    [Min(0f)]
    [SerializeField] private float _offBeatMergeWindow = 0.05f;


    private DifficultyLevel _currentDifficulty =
        DifficultyLevel.Normal;

    private double _lastOffBeatHitTime =
        double.NegativeInfinity;

    private int _lastComboMultiplier = 1;

    private bool _isRunning;


    public void BeginExperience()
    {
        if (_isRunning)
        {
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        _currentDifficulty =
            _difficultyChanged.CurrentDifficulty;

        _lastOffBeatHitTime =
            double.NegativeInfinity;

        _lastComboMultiplier = 1;

        _difficultyChanged.Raised +=
            HandleDifficultyChanged;

        _guitarDamage.RhythmHitEvaluated +=
            HandleRhythmHitEvaluated;

        _scoreProfileChanged.RaiseEvent(
            _scoreProfile
        );

        _isRunning = true;
    }


    public void EndExperience()
    {
        if (_difficultyChanged != null)
        {
            _difficultyChanged.Raised -=
                HandleDifficultyChanged;
        }

        if (_guitarDamage != null)
        {
            _guitarDamage.RhythmHitEvaluated -=
                HandleRhythmHitEvaluated;
        }

        _lastOffBeatHitTime =
            double.NegativeInfinity;

        _lastComboMultiplier = 1;

        _isRunning = false;
    }


    private void OnDisable()
    {
        EndExperience();
    }


    private void HandleRhythmHitEvaluated(GuitarRhythmHit hit, Vector3 feedbackPosition)
    {
        if (!_isRunning)
        {
            return;
        }

        if (ShouldMergeOffBeatHit(hit))
        {
            return;
        }

        RegisterSuccess(
            hit,
            feedbackPosition
        );

        RegisterComboBonus(
            hit,
            feedbackPosition
        );

        _lastComboMultiplier =
            hit.Multiplier;
    }


    private bool ShouldMergeOffBeatHit(GuitarRhythmHit hit)
    {
        if (hit.IsOnBeat)
        {
            return false;
        }

        double elapsed =
            hit.ActualTime - _lastOffBeatHitTime;

        if (elapsed >= 0.0 &&
            elapsed <= _offBeatMergeWindow)
        {
            return true;
        }

        _lastOffBeatHitTime =
            hit.ActualTime;

        return false;
    }


    private void RegisterSuccess(GuitarRhythmHit hit, Vector3 feedbackPosition)
    {
        InteractionResult result =
            new InteractionResult(
                minigameId: "Joaquin",
                interactionType: InteractionType.GuitarHit,
                outcome: InteractionOutcome.Success,
                difficulty: _currentDifficulty,
                expectedTime: hit.ExpectedTime,
                actualTime: hit.ActualTime,
                feedbackPosition: feedbackPosition
            );

        _interactionRegistered.RaiseEvent(
            result
        );
    }


    private void RegisterComboBonus(GuitarRhythmHit hit, Vector3 feedbackPosition)
    {
        if (!hit.AddedToCombo)
        {
            return;
        }

        if (hit.Multiplier <= _lastComboMultiplier)
        {
            return;
        }

        if (_scoreProfile.BonusPoints <= 0)
        {
            return;
        }

        ScoreBonus bonus =
            new ScoreBonus(
                _scoreProfile.BonusPoints,
                feedbackPosition
            );

        _scoreBonusAwarded.RaiseEvent(
            bonus
        );

        Debug.Log(
            $"[JoaquinInteractionController] " +
            $"Combo multiplier increased to x{hit.Multiplier} | " +
            $"Bonus: +{_scoreProfile.BonusPoints}",
            this
        );
    }


    private void HandleDifficultyChanged(DifficultyLevel difficulty)
    {
        _currentDifficulty = difficulty;

        _lastOffBeatHitTime =
            double.NegativeInfinity;
    }


    private bool ValidateReferences()
    {
        if (_guitarDamage == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] GuitarDamage no está asignado.",
                this
            );

            return false;
        }

        if (_interactionRegistered == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] InteractionRegistered no está asignado.",
                this
            );

            return false;
        }

        if (_difficultyChanged == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] DifficultyChanged no está asignado.",
                this
            );

            return false;
        }

        if (_scoreProfile == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] ScoreProfile no está asignado.",
                this
            );

            return false;
        }

        if (_scoreProfileChanged == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] ScoreProfileChanged no está asignado.",
                this
            );

            return false;
        }

        if (_scoreBonusAwarded == null)
        {
            Debug.LogError(
                "[JoaquinInteractionController] ScoreBonusAwarded no está asignado.",
                this
            );

            return false;
        }

        return true;
    }
}