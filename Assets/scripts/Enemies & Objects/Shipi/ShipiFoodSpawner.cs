using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiFoodSpawner :
    MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private ShipiFoodPool _foodPool;

    [Header("Directions")]
    [Range(0f, 1f)]
    [SerializeField] private float _noCutProbability = 0.2f;


    public ShipiFood Spawn(
        ShipiMovePoint spawnPoint,
        DifficultyLevel difficulty)
    {
        if (spawnPoint == null ||
            _foodPool == null)
        {
            return null;
        }

        ShipiFoodDefinitionSO definition =
            GetRandomDefinition();

        if (definition == null)
        {
            return null;
        }

        ShipiCutDirection direction =
            GetRandomDirection();

        ShipiFood food =
            _foodPool.GetFood(
                definition,
                direction,
                difficulty
            );

        if (food == null)
        {
            return null;
        }

        return food;
    }


    public void Release(
        ShipiFood food)
    {
        if (_foodPool == null)
        {
            return;
        }

        _foodPool.ReleaseFood(
            food
        );
    }


    public void ReleaseAll()
    {
        if (_foodPool != null)
        {
            _foodPool.ReleaseAllFoods();
        }
    }


    private ShipiFoodDefinitionSO
        GetRandomDefinition()
    {
        ShipiFoodDefinitionSO[] definitions =
            _foodPool.FoodDefinitions;

        if (definitions == null ||
            definitions.Length == 0)
        {
            return null;
        }

        int index =
            Random.Range(
                0,
                definitions.Length
            );

        return definitions[index];
    }


    private ShipiCutDirection
        GetRandomDirection()
    {
        if (Random.value <
            _noCutProbability)
        {
            return ShipiCutDirection.None;
        }

        int direction =
            Random.Range(
                (int) ShipiCutDirection.LeftToRight,
                (int) ShipiCutDirection.TopToBottom
            );

        return (ShipiCutDirection) direction;
    }
}