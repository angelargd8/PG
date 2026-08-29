using UnityEngine;


[DisallowMultipleComponent]
public class EnemyDanniel : MonoBehaviour
{
    [SerializeField]
    private int maxHealth = 1;


    private int currentHealth;

    private bool isDead;

    private EnemyPool enemyPool;

    private SegmentContent segmentContent;


    // =========================
    // UNITY
    // =========================

    private void OnEnable()
    {
        currentHealth = maxHealth;

        isDead = false;
    }


    // =========================
    // POOL
    // =========================

    public void SetPool(
        EnemyPool pool
    )
    {
        enemyPool = pool;
    }


    // =========================
    // SEGMENT
    // =========================

    public void SetSegmentContent(
        SegmentContent content
    )
    {
        segmentContent = content;
    }


    public void ClearSegmentContent(
        SegmentContent content
    )
    {
        if (segmentContent == content)
        {
            segmentContent = null;
        }
    }


    // =========================
    // DAMAGE
    // =========================

    public void TakeDamage(
        int damage
    )
    {
        if (isDead ||
            damage <= 0)
        {
            return;
        }


        currentHealth -= damage;


        if (currentHealth <= 0)
        {
            Die();
        }
    }


    // =========================
    // DEATH
    // =========================

    private void Die()
    {
        if (isDead)
        {
            return;
        }


        isDead = true;


        SegmentContent previousSegment =
            segmentContent;


        segmentContent = null;


        if (previousSegment != null)
        {
            previousSegment.UnregisterEnemy(
                gameObject
            );
        }


        if (enemyPool != null)
        {
            enemyPool.ReleaseEnemy(
                gameObject
            );
        }
        else
        {
            Debug.LogWarning(
                "[EnemyDanniel] No tiene EnemyPool asignado.",
                this
            );
        }
    }
}