using UnityEngine;

[CreateAssetMenu(
    fileName = "ShipiFoodDefinition",
    menuName = "Scriptable Objects/Shipi/Food Definition"
)]
public sealed class ShipiFoodDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _foodId;

    [Header("Prefabs")]
    [SerializeField] private ShipiFood _wholePrefab;
    [SerializeField] private GameObject _verticalCutPrefab;
    [SerializeField] private GameObject _horizontalCutPrefab;


    public string FoodId => _foodId;
    public ShipiFood WholePrefab => _wholePrefab;
    public GameObject VerticalCutPrefab => _verticalCutPrefab;
    public GameObject HorizontalCutPrefab => _horizontalCutPrefab;


    public GameObject GetCutPrefab(ShipiCutDirection direction)
    {
        return direction switch
        {
            ShipiCutDirection.TopToBottom =>
                _verticalCutPrefab,

            ShipiCutDirection.LeftToRight =>
                _horizontalCutPrefab,

            ShipiCutDirection.RightToLeft =>
                _horizontalCutPrefab,

            _ => null
        };
    }
}