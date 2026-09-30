using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public sealed class ShipiFoodPool :
    MonoBehaviour,
    IExperiencePreloadable
{
    [Header("Food")]
    [SerializeField]
    private ShipiFoodDefinitionSO[] _foodDefinitions;

    [Header("Pool")]
    [Min(1)]
    [SerializeField]
    private int _maxActiveFoods = 6;

    [Min(0)]
    [SerializeField]
    private int _prewarmPerFood = 1;


    private readonly Dictionary<
        ShipiFoodDefinitionSO,
        ObjectPool<ShipiFood>
    > _pools =
        new Dictionary<
            ShipiFoodDefinitionSO,
            ObjectPool<ShipiFood>
        >();

    private readonly HashSet<ShipiFood>
        _activeFoods =
            new HashSet<ShipiFood>();

    private bool _isPrewarmed;


    public ShipiFoodDefinitionSO[] FoodDefinitions =>
        _foodDefinitions;

    public int ActiveFoodCount =>
        _activeFoods.Count;


    private void Awake()
    {
        EnsureInitialized();
    }


    private void EnsureInitialized()
    {
        if (_foodDefinitions == null)
        {
            return;
        }

        for (int i = 0;
             i < _foodDefinitions.Length;
             i++)
        {
            ShipiFoodDefinitionSO definition =
                _foodDefinitions[i];

            if (definition == null ||
                _pools.ContainsKey(definition))
            {
                continue;
            }

            ObjectPool<ShipiFood> pool =
                new ObjectPool<ShipiFood>(
                    () => CreateFood(definition),
                    OnGetFood,
                    OnReleaseFood,
                    OnDestroyFood,
                    collectionCheck: false,
                    defaultCapacity: 1,
                    maxSize: _maxActiveFoods
                );

            _pools.Add(
                definition,
                pool
            );
        }
    }


    public IEnumerator Preload()
    {
        EnsureInitialized();

        if (_isPrewarmed)
        {
            yield break;
        }

        foreach (
            KeyValuePair<
                ShipiFoodDefinitionSO,
                ObjectPool<ShipiFood>
            > pair
            in _pools)
        {
            int amount =
                Mathf.Clamp(
                    _prewarmPerFood,
                    0,
                    _maxActiveFoods
                );

            ShipiFood[] foods =
                new ShipiFood[amount];

            for (int i = 0;
                 i < amount;
                 i++)
            {
                foods[i] =
                    pair.Value.Get();

                if ((i + 1) % 2 == 0)
                {
                    yield return null;
                }
            }

            for (int i = 0;
                 i < amount;
                 i++)
            {
                if (foods[i] != null)
                {
                    pair.Value.Release(
                        foods[i]
                    );
                }
            }
        }

        _isPrewarmed = true;

        Debug.Log(
            "[ShipiFoodPool] Prewarm completado.",
            this
        );
    }


    public ShipiFood GetFood(
        ShipiFoodDefinitionSO definition,
        ShipiCutDirection direction,
        DifficultyLevel difficulty)
    {
        EnsureInitialized();

        if (definition == null)
        {
            return null;
        }

        if (_activeFoods.Count >=
            _maxActiveFoods)
        {
            Debug.LogWarning(
                "[ShipiFoodPool] Se alcanzó el máximo " +
                $"de {_maxActiveFoods} comidas activas.",
                this
            );

            return null;
        }

        if (!_pools.TryGetValue(
                definition,
                out ObjectPool<ShipiFood> pool))
        {
            Debug.LogError(
                "[ShipiFoodPool] No existe pool para " +
                $"{definition.name}.",
                this
            );

            return null;
        }

        ShipiFood food =
            pool.Get();

        if (food == null)
        {
            return null;
        }

        food.Initialize(
            definition,
            direction,
            difficulty
        );

        _activeFoods.Add(
            food
        );

        food.gameObject.SetActive(
            true
        );

        return food;
    }


    public void ReleaseFood(
        ShipiFood food)
    {
        if (food == null ||
            !_activeFoods.Contains(food))
        {
            return;
        }

        ShipiFoodDefinitionSO definition =
            food.Definition;

        if (definition == null ||
            !_pools.TryGetValue(
                definition,
                out ObjectPool<ShipiFood> pool))
        {
            Debug.LogError(
                "[ShipiFoodPool] No se pudo devolver la comida.",
                this
            );

            return;
        }

        _activeFoods.Remove(
            food
        );

        pool.Release(
            food
        );
    }


    public void ReleaseAllFoods()
    {
        if (_activeFoods.Count == 0)
        {
            return;
        }

        ShipiFood[] foods =
            new ShipiFood[
                _activeFoods.Count
            ];

        _activeFoods.CopyTo(
            foods
        );

        for (int i = 0;
             i < foods.Length;
             i++)
        {
            ReleaseFood(
                foods[i]
            );
        }
    }


    private ShipiFood CreateFood(
        ShipiFoodDefinitionSO definition)
    {
        if (definition == null ||
            definition.WholePrefab == null)
        {
            Debug.LogError(
                "[ShipiFoodPool] FoodDefinition inválida.",
                this
            );

            return null;
        }

        ShipiFood food =
            Instantiate(
                definition.WholePrefab,
                transform
            );

        food.gameObject.SetActive(
            false
        );

        return food;
    }


    private void OnGetFood(
        ShipiFood food)
    {
    }


    private void OnReleaseFood(
        ShipiFood food)
    {
        if (food == null)
        {
            return;
        }

        food.ResetFood();

        food.gameObject.SetActive(
            false
        );

        food.transform.SetParent(
            transform,
            false
        );
    }


    private void OnDestroyFood(
        ShipiFood food)
    {
        if (food != null)
        {
            Destroy(
                food.gameObject
            );
        }
    }
}