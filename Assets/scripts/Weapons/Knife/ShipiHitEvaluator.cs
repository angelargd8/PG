using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiHitEvaluator :
    MonoBehaviour,
    IExperienceRuntime
{
    private enum DetectedCutDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }


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


    public void EvaluateCut(ShipiFood food, Vector3 cutVelocity, Vector3? feedbackPosition = null)
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

        DetectedCutDirection actualDirection =
            GetActualDirection(
                cutVelocity
            );

        GameObject cutPrefab =
            GetCutPrefab(
                definition,
                actualDirection
            );

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
                actualDirection
            );

        InteractionOutcome outcome =
            correctDirection
                ? InteractionOutcome.Success
                : InteractionOutcome.Failed;


        string foodName =
            definition.FoodId;

        Debug.Log(
            $"[ShipiCut] " +
            $"Food: {foodName} | " +
            $"Expected: {expectedDirection} | " +
            $"Received: {actualDirection} | " +
            $"Result: {outcome}",
            this
        );


        double expectedTime =
            food.ExpectedCutTime;

        double actualTime =
            _musicClock.SongTime;

        double reactionTime =
            System.Math.Max(
                0d,
                actualTime - food.CueTime
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
                outcome:
                    outcome,
                difficulty:
                    food.Difficulty,
                expectedTime:
                    expectedTime,

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

        string foodName =
            food.Definition != null
                ? food.Definition.FoodId
                : food.name;

        Debug.Log(
            $"[ShipiCut] " +
            $"Food: {foodName} | " +
            $"Expected: {food.ExpectedDirection} | " +
            $"Received: None | " +
            $"Result: {outcome}",
            this
        );


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


    private bool IsCorrectDirection(ShipiCutDirection expectedDirection, DetectedCutDirection actualDirection)
    {
        return expectedDirection switch
        {
            ShipiCutDirection.LeftToRight =>
                actualDirection ==
                DetectedCutDirection.LeftToRight,

            ShipiCutDirection.RightToLeft =>
                actualDirection ==
                DetectedCutDirection.RightToLeft,

            ShipiCutDirection.TopToBottom =>
                actualDirection ==
                DetectedCutDirection.TopToBottom,

            _ => false
        };
    }


    private DetectedCutDirection GetActualDirection(Vector3 velocity)
    {
        Vector3 normalizedVelocity = velocity.normalized;

          Vector3 right = _directionReference.right;

        Vector3 up = _directionReference.up;

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

        if (horizontalDominant)
        {
            return horizontal > 0f
                ? DetectedCutDirection.LeftToRight
                : DetectedCutDirection.RightToLeft;
        }

        return vertical < 0f
            ? DetectedCutDirection.TopToBottom
            : DetectedCutDirection.BottomToTop;
    }


    private GameObject GetCutPrefab(ShipiFoodDefinitionSO definition, DetectedCutDirection actualDirection)
    {
        bool horizontal =
            actualDirection ==
                DetectedCutDirection.LeftToRight ||
            actualDirection ==
                DetectedCutDirection.RightToLeft;

        return horizontal
            ? definition.HorizontalCutPrefab
            : definition.VerticalCutPrefab;
    }


    private void ResolveDirectionReference()
    {
        if (_directionReference != null)
        {
            return;
        }

        Debug.LogError(
            "[ShipiHitEvaluator] " +
            "DirectionReference no está asignado. " +
            "Asigna Frontal Objects.",
            this
        );
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