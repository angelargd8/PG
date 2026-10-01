using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiHitEvaluator :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform _directionReference;


    private void Awake()
    {
        ResolveDirectionReference();
    }


    public void EvaluateCut(
        ShipiFood food,
        Vector3 cutVelocity)
    {
        if (food == null ||
            food.IsResolved)
        {
            return;
        }

        if (food.CurrentPoint == null ||
            food.CurrentPoint.Type !=
            ShipiMovePointType.Cutting)
        {
            return;
        }

        if (cutVelocity.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        ShipiFoodDefinitionSO definition =
            food.Definition;

        if (definition == null)
        {
            return;
        }

        bool isHorizontal =
            IsHorizontalCut(
                cutVelocity
            );

        GameObject cutPrefab =
            isHorizontal
                ? definition.HorizontalCutPrefab
                : definition.VerticalCutPrefab;

        if (cutPrefab == null)
        {
            Debug.LogWarning(
                "[ShipiHitEvaluator] " +
                "No hay prefab para este tipo de corte.",
                this
            );

            return;
        }

        food.MarkResolved();

        food.ShowCutVisual(
            cutPrefab
        );
    }


    private bool IsHorizontalCut(
        Vector3 velocity)
    {
        Vector3 normalizedVelocity =
            velocity.normalized;

        Vector3 right =
            _directionReference != null
                ? _directionReference.right
                : Vector3.right;

        Vector3 up =
            _directionReference != null
                ? _directionReference.up
                : Vector3.up;

        float horizontal =
            Vector3.Dot(
                normalizedVelocity,
                right
            );

        float vertical =
            Vector3.Dot(
                normalizedVelocity,
                up
            );

        return
            Mathf.Abs(horizontal) >
            Mathf.Abs(vertical);
    }


    private void ResolveDirectionReference()
    {
        if (_directionReference != null)
        {
            return;
        }

        Camera mainCamera =
            Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "[ShipiHitEvaluator] " +
                "No se encontró Main Camera.",
                this
            );

            return;
        }

        _directionReference =
            mainCamera.transform;
    }
}