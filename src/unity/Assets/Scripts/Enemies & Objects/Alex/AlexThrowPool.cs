using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public sealed class AlexThrowPool : MonoBehaviour, IExperiencePreloadable
{
    [Serializable]
    public sealed class Entry
    {
        public AlexThrowKind Kind;
        public GameObject Prefab;
        [Min(0)] public int PrewarmCount = 8;
        [Min(1)] public int MaxSize = 24;
    }

    [Tooltip("Un pool independiente para Dollar, Pumpkin A, F y G.")]
    [SerializeField] private Entry[] _entries = new Entry[4];

    private readonly Dictionary<AlexThrowKind, ObjectPool<AlexThrownObject>> _pools = new();
    private readonly HashSet<AlexThrownObject> _leased = new();
    private readonly List<AlexThrownObject> _releaseBuffer = new();
    private bool _prewarmed;

    private void Awake() => EnsurePools();

    private void EnsurePools()
    {
        if (_pools.Count > 0) return;
        foreach (Entry entry in _entries)
        {
            if (entry == null || entry.Prefab == null || _pools.ContainsKey(entry.Kind))
            {
                Debug.LogError("[AlexThrowPool] Asigna cuatro prefabs con tipos distintos.", this);
                continue;
            }

            Entry captured = entry;
            _pools.Add(entry.Kind, new ObjectPool<AlexThrownObject>(
                () => Create(captured), null,
                item => { item.ResetForPool(); item.gameObject.SetActive(false); },
                item => { if (item != null) Destroy(item.gameObject); },
                true, Mathf.Max(1, entry.PrewarmCount), Mathf.Max(1, entry.MaxSize)));
        }
    }

    private AlexThrownObject Create(Entry entry)
    {
        GameObject clone = Instantiate(entry.Prefab, transform);
        clone.SetActive(false);
        // These variants include static flags intended for scenery.
        foreach (Transform child in clone.GetComponentsInChildren<Transform>(true))
            child.gameObject.isStatic = false;
        AlexThrownObject item = clone.GetComponent<AlexThrownObject>();
        if (item == null) item = clone.AddComponent<AlexThrownObject>();
        item.ConfigurePhysics();
        return item;
    }

    public IEnumerator Preload()
    {
        if (_prewarmed) yield break;
        EnsurePools();
        foreach (Entry entry in _entries)
        {
            if (entry == null || !_pools.TryGetValue(entry.Kind, out var pool)) continue;
            var items = new List<AlexThrownObject>();
            try
            {
                int count = Mathf.Clamp(entry.PrewarmCount, 0, entry.MaxSize);
                for (int i = 0; i < count; i++)
                {
                    items.Add(pool.Get());
                    if (i % 2 == 1) yield return null;
                }
            }
            finally
            {
                foreach (var item in items) pool.Release(item);
            }
        }
        _prewarmed = true;
    }

    public bool Launch(AlexThrowKind kind, AlexThrowDirector director, Vector3 origin,
        Quaternion rotation, Vector3 target, double launchTime, double hitTime)
    {
        EnsurePools();
        if (!_pools.TryGetValue(kind, out var pool)) return false;
        AlexThrownObject item = pool.Get();
        _leased.Add(item);
        item.Launch(this, kind, director, origin, rotation, target, launchTime, hitTime);
        item.gameObject.SetActive(true);
        return true;
    }

    public void Release(AlexThrownObject item)
    {
        if (item == null || !_leased.Remove(item)) return;
        _pools[item.Kind].Release(item);
    }

    public void ReleaseAll()
    {
        _releaseBuffer.Clear();
        _releaseBuffer.AddRange(_leased);
        foreach (var item in _releaseBuffer) Release(item);
        _releaseBuffer.Clear();
    }

    private void OnDisable() => ReleaseAll();

    private void OnDestroy()
    {
        ReleaseAll();
        foreach (var pool in _pools.Values) pool.Clear();
        _pools.Clear();
    }
}
