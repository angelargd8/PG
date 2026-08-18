using UnityEngine;

public class EnemyDanniel : MonoBehaviour
{
    [SerializeField] private int maxHealth = 1;

    private int currentHealth;

    private EnemyPool enemyPool;

    private void OnEnable()
    {
        currentHealth = maxHealth;
    }

    public void SetPool(EnemyPool pool)
    {
        enemyPool = pool;
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
        enemyPool.ReleaseEnemy(gameObject);
    }
}
