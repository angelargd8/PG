using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public sealed class JuanAndresTargetPool :
    MonoBehaviour,
    IExperiencePreloadable
{
    [Header("Pool")]
    [SerializeField] private JuanAndresTarget _targetPrefab;

    [Min(1)]
    [SerializeField] private int _defaultCapacity = 8;

    [Min(1)]
    [SerializeField] private int _maxSize = 16;

    [Min(0)]
    [SerializeField] private int _prewarmCount = 8;


    private ObjectPool<JuanAndresTarget> _pool;
    private bool _isPrewarmed;


    private void Awake()
    {
        EnsureInitialized();
    }


    private void EnsureInitialized()
    {
        if (_pool != null)
        {
            return;
        }

        _pool =
            new ObjectPool<JuanAndresTarget>(
                CreateTarget,
                OnGetTarget,
                OnReleaseTarget,
                OnDestroyTarget,
                collectionCheck: false,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxSize
            );
    }


    public IEnumerator Preload()
    {
        EnsureInitialized();

        if (_isPrewarmed)
        {
            yield break;
        }

        int amount =
            Mathf.Clamp(
                _prewarmCount,
                0,
                _maxSize
            );

        JuanAndresTarget[] targets =
            new JuanAndresTarget[amount];

        for (int i = 0; i < amount; i++)
        {
            targets[i] =
                _pool.Get();

            if ((i + 1) % 2 == 0)
            {
                yield return null;
            }
        }

        for (int i = 0; i < amount; i++)
        {
            if (targets[i] != null)
            {
                _pool.Release(
                    targets[i]
                );
            }
        }

        _isPrewarmed = true;

        Debug.Log(
            $"[JuanAndresTargetPool] " +
            $"Prewarm completado: {amount} targets.",
            this
        );
    }


    public JuanAndresTarget GetTarget()
    {
        EnsureInitialized();

        return _pool.Get();
    }


    public void ReleaseTarget(JuanAndresTarget target)
    {
        if (target == null)
        {
            return;
        }

        _pool.Release(
            target
        );
    }


    private JuanAndresTarget CreateTarget()
    {
        if (_targetPrefab == null)
        {
            Debug.LogError(
                "[JuanAndresTargetPool] Target Prefab no está asignado.",
                this
            );

            return null;
        }

        JuanAndresTarget target =
            Instantiate(
                _targetPrefab,
                transform
            );

        target.gameObject.SetActive(false);

        return target;
    }


    private void OnGetTarget(JuanAndresTarget target)
    {
    }


    private void OnReleaseTarget(JuanAndresTarget target)
    {
        if (target == null)
        {
            return;
        }

        target.ResetTarget();

        target.gameObject.SetActive(false);

        target.transform.SetParent(
            transform,
            false
        );
    }


    private void OnDestroyTarget(JuanAndresTarget target)
    {
        if (target != null)
        {
            Destroy(
                target.gameObject
            );
        }
    }
}