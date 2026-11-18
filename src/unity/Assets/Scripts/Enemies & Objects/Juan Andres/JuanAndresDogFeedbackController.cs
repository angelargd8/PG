using UnityEngine;

[DisallowMultipleComponent]
public sealed class JuanAndresDogFeedbackController :
    MonoBehaviour,
    IExperienceRuntime
{
    private const string MinigameId = "JuanAndres";


    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private ScoreProfileSO _scoreProfile;

    [Header("Events")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;

    [Header("Animator")]
    [SerializeField] private string _animationIdParameter = "AnimationID";
    [SerializeField] private string _idleStateName = "Base Layer.Breathing";

    [Header("Perfect")]
    [SerializeField] private int _perfectAnimationId = 1;
    [SerializeField] private string _perfectStateName = "Base Layer.WigglingTail";

    [Header("Failed Reaction")]
    [SerializeField] private bool _useFailedReaction = true;
    [SerializeField] private int _failedAnimationId = 6;
    [SerializeField] private string _failedStateName = "Base Layer.AngryCycle";


    private int _animationIdHash;
    private int _idleStateHash;
    private int _activeStateHash;

    private bool _isRunning;
    private bool _isPlayingReaction;
    private bool _hasEnteredActiveState;
    private bool _returnRequested;


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

        _animationIdHash =
            Animator.StringToHash(
                _animationIdParameter
            );

        _idleStateHash =
            Animator.StringToHash(
                _idleStateName
            );

        _interactionRegistered.Raised +=
            HandleInteraction;

        ResetAnimator();

        _isRunning = true;
    }


    public void EndExperience()
    {
        if (!_isRunning)
        {
            return;
        }

        _interactionRegistered.Raised -=
            HandleInteraction;

        ResetAnimator();

        _isRunning = false;
    }


    private void Update()
    {
        if (!_isRunning ||
            !_isPlayingReaction ||
            _animator == null ||
            !_animator.isActiveAndEnabled)
        {
            return;
        }

        AnimatorStateInfo state =
            _animator.GetCurrentAnimatorStateInfo(0);

        if (!_hasEnteredActiveState)
        {
            if (state.fullPathHash ==
                _activeStateHash)
            {
                _hasEnteredActiveState = true;
            }

            return;
        }

        if (!_returnRequested)
        {
            if (state.fullPathHash ==
                    _activeStateHash &&
                state.normalizedTime >= 1f)
            {
                _animator.SetInteger(
                    _animationIdHash,
                    0
                );

                _returnRequested = true;
            }

            return;
        }

        if (state.fullPathHash ==
                _idleStateHash &&
            !_animator.IsInTransition(0))
        {
            FinishReaction();
        }
    }


    private void HandleInteraction(InteractionResult result)
    {
        if (!_isRunning ||
            _isPlayingReaction ||
            result.MinigameId != MinigameId)
        {
            return;
        }

        if (result.Outcome == InteractionOutcome.Failed)
        {
            if (_useFailedReaction)
            {
                StartReaction(
                    _failedAnimationId,
                    _failedStateName
                );
            }

            return;
        }

        if (result.Outcome !=
            InteractionOutcome.Success)
        {
            return;
        }

        ScoreEvaluation evaluation =
            _scoreProfile.Evaluate(
                result
            );

        if (
            evaluation.TimingJudgement == TimingJudgement.Perfect || 
            evaluation.TimingJudgement == TimingJudgement.Nice || 
            evaluation.TimingJudgement == TimingJudgement.Good
        )
        {
            StartReaction(
                _perfectAnimationId,
                _perfectStateName
            );
        }
    }


    private void StartReaction(int animationId, string stateName)
    {
        if (_isPlayingReaction)
        {
            return;
        }

        _activeStateHash =
            Animator.StringToHash(
                stateName
            );

        _hasEnteredActiveState = false;
        _returnRequested = false;
        _isPlayingReaction = true;

        _animator.SetInteger(
            _animationIdHash,
            animationId
        );
    }


    private void FinishReaction()
    {
        _isPlayingReaction = false;
        _hasEnteredActiveState = false;
        _returnRequested = false;
        _activeStateHash = 0;
    }


    private void ResetAnimator()
    {
        if (_animator != null)
        {
            _animator.SetInteger(
                _animationIdHash,
                0
            );
        }

        FinishReaction();
    }


    private bool ValidateReferences()
    {
        if (_animator == null)
        {
            Debug.LogError(
                "[JuanAndresDogFeedbackController] Animator no está asignado.",
                this
            );

            return false;
        }

        if (_scoreProfile == null)
        {
            Debug.LogError(
                "[JuanAndresDogFeedbackController] ScoreProfile no está asignado.",
                this
            );

            return false;
        }

        if (_interactionRegistered == null)
        {
            Debug.LogError(
                "[JuanAndresDogFeedbackController] InteractionRegistered no está asignado.",
                this
            );

            return false;
        }

        return true;
    }
}