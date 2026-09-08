using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyHitEvaluator : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;


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
            expectedTime: 0.0,
            directionAccuracy: correctDirection ? 1f : 0f,
            usedCorrectHand: correctHand
        );

        if (_interactionRegistered != null)
        {
            _interactionRegistered.RaiseEvent(result);
        }

        Debug.Log(
            $"Jeremy Hit | Expected: {expectedHand} {expectedDirection} | " +
            $"Actual: {actualHand} {actualDirection} | " +
            $"Correct Hand: {correctHand} | " +
            $"Correct Direction: {correctDirection} | " +
            $"Outcome: {outcome}",
            enemy
        );
    }
}