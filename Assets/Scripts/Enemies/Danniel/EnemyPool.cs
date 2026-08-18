using UnityEngine;
using UnityEngine.Pool;

public class EnemyPool : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxSize = 30;

    private ObjectPool<GameObject> pool;


    private void Awake()
    {
        pool = new ObjectPool<GameObject>(
            CreateEnemy,
            OnGetEnemy,
            OnReleaseEnemy,
            OnDestroyEnemy,
            true,
            defaultCapacity,
            maxSize
        );
    }


    private GameObject CreateEnemy()
    {
        GameObject enemy =
            Instantiate(enemyPrefab, transform);

        EnemyDanniel enemyScript =
            enemy.GetComponent<EnemyDanniel>();

        if (enemyScript != null)
        {
            enemyScript.SetPool(this);
        }

        return enemy;
    }


    private void OnGetEnemy(GameObject enemy)
    {
        enemy.SetActive(true);
    }


    private void OnReleaseEnemy(GameObject enemy)
    {
        enemy.SetActive(false);

        enemy.transform.SetParent(transform);
    }


    private void OnDestroyEnemy(GameObject enemy)
    {
        Destroy(enemy);
    }


    public GameObject GetEnemy()
    {
        return pool.Get();
    }


    public void ReleaseEnemy(GameObject enemy)
    {
        if (enemy != null)
        {
            pool.Release(enemy);
        }
    }
}