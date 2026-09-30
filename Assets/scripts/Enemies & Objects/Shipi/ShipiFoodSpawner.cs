using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiFoodSpawner : MonoBehaviour
{
    [Header("Food")]
    [SerializeField] private ShipiFoodDefinitionSO[] _foodDefinitions;

    [Header("Directions")]
    [Range(0f, 1f)]
    [SerializeField] private float _noCutProbability = 0.2f;

    [Header("Hierarchy")]
    [SerializeField] private Transform _foodRoot;


    public ShipiFood Spawn(ShipiMovePoint spawnPoint, DifficultyLevel difficulty)
    {
        if (spawnPoint == null ||
            _foodDefinitions == null ||
            _foodDefinitions.Length == 0)
        {
            return null;
        }

        ShipiFoodDefinitionSO definition =
            GetRandomDefinition();

        if (definition == null ||
            definition.WholePrefab == null)
        {
            return null;
        }

        ShipiFood food =
            Instantiate(
                definition.WholePrefab,
                _foodRoot
            );

        ShipiCutDirection direction =
            GetRandomDirection();

        food.Initialize(
            definition,
            direction,
            difficulty
        );

        food.transform.position =
            spawnPoint.Position;

        return food;
    }


    private ShipiFoodDefinitionSO GetRandomDefinition()
    {
        int index =
            Random.Range(
                0,
                _foodDefinitions.Length
            );

        return _foodDefinitions[index];
    }


    private ShipiCutDirection GetRandomDirection()
    {
        if (Random.value <
            _noCutProbability)
        {
            return ShipiCutDirection.None;
        }

        int direction =
            Random.Range(
                (int)ShipiCutDirection.LeftToRight,
                (int)ShipiCutDirection.BottomToTop + 1
            );

        return
            (ShipiCutDirection)direction;
    }
}