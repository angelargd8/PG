using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiHitEvaluator :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("References")]
    [SerializeField]
    private ShipiConveyDirector _conveyDirector;

    [SerializeField]
    private Transform _directionReference;


    [Header("Metrics")]
    [SerializeField]
    private InteractionResultEventChannelSO
        _interactionRegistered;


    private ExperienceMusicClock _musicClock;
    private bool _isRunning;


    public void BeginExperience()
    {
        if (_isRunning)
        {
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        _musicClock =
            FindFirstObjectByType<
                ExperienceMusicClock
            >();

        if (_musicClock == null)
        {
            Debug.LogError(
                "[ShipiHitEvaluator] " +
                "No se encontró ExperienceMusicClock.",
                this
            );

            return;
        }

        ResolveDirectionReference();

        _conveyDirector.FoodLeftCuttingPoint +=
            HandleFoodLeftCuttingPoint;

        _isRunning = true;
    }


    public void EndExperience()
    {
        if (!_isRunning)
        {
            return;
        }

        if (_conveyDirector != null)
        {
            _conveyDirector.FoodLeftCuttingPoint -=
                HandleFoodLeftCuttingPoint;
        }

        _isRunning = false;
    }


    public void EvaluateCut(
        ShipiFood food,
        Vector3 cutVelocity,
        Vector3? feedbackPosition = null)
    {
        if (!_isRunning ||
            food == null ||
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
                "No hay prefab para este corte.",
                this
            );

            return;
        }


        ShipiCutDirection expectedDirection =
            food.ExpectedDirection;

        bool shouldNotCut =
            expectedDirection ==
            ShipiCutDirection.None;

        bool correctDirection =
            !shouldNotCut &&
            IsCorrectDirection(
                expectedDirection,
                cutVelocity
            );

        InteractionOutcome outcome =
            correctDirection
                ? InteractionOutcome.Success
                : InteractionOutcome.Failed;


        double expectedTime =
            food.ExpectedCutTime;

        double actualTime =
            _musicClock.SongTime;

        double reactionTime =
            System.Math.Max(
                0d,
                actualTime - expectedTime
            );


        food.MarkResolved();

        food.ShowCutVisual(
            cutPrefab
        );


        float? directionAccuracy =
            shouldNotCut
                ? null
                : correctDirection
                    ? 1f
                    : 0f;

        InteractionResult result =
            new InteractionResult(
                minigameId: "Shipi",
                interactionType:
                    InteractionType.FoodCut,
                outcome: outcome,
                difficulty:
                    food.Difficulty,
                expectedTime:
                    expectedTime,

                // Una X no tiene un momento
                // correcto de corte.
                actualTime:
                    shouldNotCut
                        ? null
                        : actualTime,

                reactionTime:
                    shouldNotCut
                        ? null
                        : reactionTime,

                directionAccuracy:
                    directionAccuracy,

                feedbackPosition:
                    feedbackPosition
            );

        _interactionRegistered.RaiseEvent(
            result
        );
    }


    private void HandleFoodLeftCuttingPoint(
        ShipiFood food)
    {
        if (!_isRunning ||
            food == null ||
            food.IsResolved)
        {
            return;
        }

        bool shouldNotCut =
            food.ExpectedDirection ==
            ShipiCutDirection.None;

        InteractionType interactionType =
            shouldNotCut
                ? InteractionType.FoodClear
                : InteractionType.FoodCut;

        InteractionOutcome outcome =
            shouldNotCut
                ? InteractionOutcome.Success
                : InteractionOutcome.Missed;


        food.MarkResolved();


        InteractionResult result =
            new InteractionResult(
                minigameId: "Shipi",
                interactionType:
                    interactionType,
                outcome:
                    outcome,
                difficulty:
                    food.Difficulty,
                expectedTime:
                    food.ExpectedCutTime
            );

        _interactionRegistered.RaiseEvent(
            result
        );
    }


    private bool IsCorrectDirection(
        ShipiCutDirection expectedDirection,
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

        bool horizontalDominant =
            Mathf.Abs(horizontal) >
            Mathf.Abs(vertical);

        return expectedDirection switch
        {
            ShipiCutDirection.LeftToRight =>
                horizontalDominant &&
                horizontal > 0f,

            ShipiCutDirection.RightToLeft =>
                horizontalDominant &&
                horizontal < 0f,

            ShipiCutDirection.TopToBottom =>
                !horizontalDominant &&
                vertical < 0f,

            _ => false
        };
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


    private bool ValidateReferences()
    {
        if (_conveyDirector == null)
        {
            Debug.LogError(
                "[ShipiHitEvaluator] " +
                "ConveyDirector no está asignado.",
                this
            );

            return false;
        }

        if (_interactionRegistered == null)
        {
            Debug.LogError(
                "[ShipiHitEvaluator] " +
                "InteractionRegistered no está asignado.",
                this
            );

            return false;
        }

        return true;
    }
}