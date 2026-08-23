using UnityEngine;

public class EnemyDanniel : MonoBehaviour
{
    [SerializeField] private int maxHealth = 1;

    private int currentHealth;

    private EnemyPool enemyPool;

    private SegmentContent segmentContent;


    private void OnEnable()
    {
        currentHealth = maxHealth;
    }


    public void SetPool(EnemyPool pool)
    {
        enemyPool = pool;
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
        // Solo limpiar si realmente seguimos
        // perteneciendo a ese segmento
        if (segmentContent == content)
        {
            segmentContent = null;
        }
    }


    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    private void Die()
    {
        // Guardar referencia temporal
        SegmentContent previousSegment =
            segmentContent;

        segmentContent = null;


        // Quitarnos inmediatamente del segmento
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
    }
}