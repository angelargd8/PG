using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public sealed class JeremyEnemyPool :
    MonoBehaviour,
    IExperiencePreloadable
{
    [Header("Pool")]
    [SerializeField] private JeremyEnemy _enemyPrefab;
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private int _maxSize = 20;
    [SerializeField] private int _prewarmCount = 10;


    private ObjectPool<JeremyEnemy> _pool;
    private bool _isPrewarmed;


    private void Awake()
    {
        _pool = new ObjectPool<JeremyEnemy>(
            CreateEnemy,
            OnGetEnemy,
            OnReleaseEnemy,
            OnDestroyEnemy,
            collectionCheck: false,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize
        );
    }


    public IEnumerator Preload()
    {
        if (_isPrewarmed)
        {
            yield break;
        }

        int amount = Mathf.Clamp(_prewarmCount, 0, _maxSize);

        JeremyEnemy[] enemies = new JeremyEnemy[amount];

        for (int i = 0; i < amount; i++)
        {
            enemies[i] = _pool.Get();

            if ((i + 1) % 2 == 0)
            {
                yield return null;
            }
        }

        for (int i = 0; i < amount; i++)
        {
            if (enemies[i] == null)
            {
                continue;
            }

            _pool.Release(enemies[i]);
        }

        _isPrewarmed = true;

        Debug.Log(
            $"[JeremyEnemyPool] Prewarm completado: {amount} enemigos.",
            this
        );
    }


    public JeremyEnemy GetEnemy(Vector3 position, Quaternion rotation, Vector3 targetPosition)
    {
        JeremyEnemy enemy = _pool.Get();

        if (enemy == null)
        {
            Debug.LogError(
                "[JeremyEnemyPool] El pool devolvió null.",
                this
            );

            return null;
        }

        enemy.transform.SetPositionAndRotation(position, rotation);
        enemy.Initialize(this, targetPosition);
        enemy.gameObject.SetActive(true);

        return enemy;
    }


    public void ReleaseEnemy(JeremyEnemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        _pool.Release(enemy);
    }


    private JeremyEnemy CreateEnemy()
    {
        if (_enemyPrefab == null)
        {
            Debug.LogError(
                "[JeremyEnemyPool] No se asignó Enemy Prefab.",
                this
            );

            return null;
        }

        JeremyEnemy enemy = Instantiate(_enemyPrefab, transform);

        enemy.gameObject.SetActive(false);

        return enemy;
    }


    private void OnGetEnemy(JeremyEnemy enemy)
    {
        // La activación se hace en GetEnemy()
        // después de configurar posición y destino.
    }


    private void OnReleaseEnemy(JeremyEnemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.ResetEnemy();
        enemy.gameObject.SetActive(false);
        enemy.transform.SetParent(transform, false);
    }


    private void OnDestroyEnemy(JeremyEnemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        Destroy(enemy.gameObject);
    }
}