using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyHitEvaluator : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;


    [Header("References")]
    [SerializeField] private JeremyBeatClock _beatClock;


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

        double actualHitTime = _beatClock.SongTime;

        if (!enemy.TryResolveHit())
        {
            return;
        }

        InteractionOutcome outcome = correctHit
            ? InteractionOutcome.Success
            : InteractionOutcome.Failed;

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
            $"Actual: {_beatClock.SongTime:F3} | " +
            $"Offset: {_beatClock.SongTime - enemy.ExpectedHitTime:F3}",
            this
        );
    }
}