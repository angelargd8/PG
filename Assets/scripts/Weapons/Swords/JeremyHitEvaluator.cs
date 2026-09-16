using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyHitEvaluator : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;


    [Header("Haptic Feedback")]
    [Range(0f, 1f)]
    [SerializeField] private float _successHapticAmplitude = 0.4f;

    [Min(0.01f)]
    [SerializeField] private float _successHapticDuration = 0.06f;

    [Range(0f, 1f)]
    [SerializeField] private float _failureHapticAmplitude = 0.6f;

    [Min(0.01f)]
    [SerializeField] private float _failureHapticDuration = 0.1f;


    private ExperienceMusicClock _musicClock;


    private void Awake()
    {
        _musicClock = FindFirstObjectByType<ExperienceMusicClock>();

        if (_musicClock == null)
        {
            Debug.LogError(
                "[JeremyHitEvaluator] No se encontró ExperienceMusicClock.",
                this
            );
        }
    }

    public void EvaluateHit(JeremyEnemy enemy, JeremyCutDirection actualDirection, JeremyHand actualHand)
    {
        if (enemy == null)
        {
            return;
        }

        JeremyCutDirection expectedDirection = enemy.ExpectedDirection;
        JeremyHand expectedHand = enemy.ExpectedHand;

        bool correctDirection = expectedDirection == actualDirection;

        bool correctHand =
            expectedHand == JeremyHand.Any ||
            expectedHand == actualHand;

        bool correctHit = correctDirection && correctHand;

        double actualHitTime = _musicClock.SongTime;

        if (!enemy.TryResolveHit())
        {
            return;
        }

        InteractionOutcome outcome = correctHit
            ? InteractionOutcome.Success
            : InteractionOutcome.Failed;

        PlayHapticFeedback(outcome, actualHand);

        InteractionResult result = new InteractionResult(
            minigameId: "Jeremy",
            interactionType: InteractionType.SwordCut,
            outcome: outcome,
            difficulty: enemy.Difficulty,
            expectedTime: enemy.ExpectedHitTime,
            actualTime: actualHitTime,
            // reactionTime: actualHitTime - enemy.ExpectedHitTime,
            directionAccuracy: correctDirection ? 1f : 0f,
            usedCorrectHand: correctHand
        );

        if (_interactionRegistered != null)
        {
            _interactionRegistered.RaiseEvent(result);
        }

        // Debug.Log(
        //     $"Jeremy Hit | Expected: {expectedHand} {expectedDirection} | " +
        //     $"Actual: {actualHand} {actualDirection} | " +
        //     $"Correct Hand: {correctHand} | " +
        //     $"Correct Direction: {correctDirection} | " +
        //     $"Outcome: {outcome}",
        //     enemy
        // );

        Debug.Log(
            $"[JeremyHitEvaluator] Hit | " +
            $"Expected: {enemy.ExpectedHitTime:F3} | " +
            $"Actual: {actualHitTime:F3} | " +
            $"Offset: {actualHitTime - enemy.ExpectedHitTime:F3}",
            this
        );
    }

    private void PlayHapticFeedback(InteractionOutcome outcome, JeremyHand actualHand)
    {
        XRHapticFeedback haptics = XRHapticFeedback.Instance;

        if (haptics == null)
        {
            return;
        }

        if (outcome == InteractionOutcome.Success)
        {
            if (actualHand == JeremyHand.Left)
            {
                haptics.PulseLeft(
                    _successHapticAmplitude,
                    _successHapticDuration
                );
            }
            else if (actualHand == JeremyHand.Right)
            {
                haptics.PulseRight(
                    _successHapticAmplitude,
                    _successHapticDuration
                );
            }

            return;
        }

        if (outcome == InteractionOutcome.Failed)
        {
            haptics.PulseLeft(
                _failureHapticAmplitude,
                _failureHapticDuration
            );

            haptics.PulseRight(
                _failureHapticAmplitude,
                _failureHapticDuration
            );
        }
    }
}