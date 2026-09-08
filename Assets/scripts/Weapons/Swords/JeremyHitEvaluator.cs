using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyHitEvaluator : MonoBehaviour
{
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
            enemy.ExpectedHand == JeremyHand.Any ||
            enemy.ExpectedHand == actualHand;

        bool correctHit = correctDirection && correctHand;

        Debug.Log(
            $"Jeremy Hit | Expected: {expectedHand} {expectedDirection} | " +
            $"Actual: {actualHand} {actualDirection} | " +
            $"Correct Hand: {correctHand} |" +
            $"Correct Direction: {correctDirection} |" +
            $"Correct Hit: {correctHit}",
            enemy
        );

        enemy.TryResolveHit();
    }
}