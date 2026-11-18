using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField]
    private int maxHealth = 1;


    private int currentHealth;
    private int? spawnHealthOverride;

    private bool isDead;

    public bool IsAlive => isActiveAndEnabled && !isDead;
    public uint SpawnVersion { get; private set; }

    private EnemyPool enemyPool;

    // Solo se utiliza en escenas con segmentos
    private SegmentContent segmentContent;


    private void OnEnable()
    {
        SpawnVersion++;
        currentHealth = Mathf.Max(1, spawnHealthOverride ?? maxHealth);

        isDead = false;

        segmentContent = null;
    }

    public void SetPool(
        EnemyPool pool
    )
    {
        enemyPool = pool;
    }

    public void SetSpawnHealth(int? health)
    {
        spawnHealthOverride = health.HasValue ? Mathf.Max(1, health.Value) : (int?)null;
    }



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
                "[EnemyController] No tiene EnemyPool asignado.",
                this
            );

            gameObject.SetActive(false);
        }
    }
}
