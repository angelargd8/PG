using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyHitEvaluator : MonoBehaviour
{
    public void EvaluateHit(JeremyEnemy enemy, JeremyCutDirection actualDirection)
    {
        if (enemy == null)
        {
            return;
        }

        bool correctDirection = enemy.ExpectedDirection == actualDirection;

        Debug.Log(
            $"Jeremy Hit | Expected: {enemy.ExpectedDirection} | " +
            $"Actual: {actualDirection} | " +
            $"Correct: {correctDirection}",
            enemy
        );
    }
}