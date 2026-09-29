using UnityEngine;

[DisallowMultipleComponent]
public sealed class JuanAndresHitEvaluator :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("References")]
    [SerializeField] private JuanAndresTargetSpawner _targetSpawner;
    [SerializeField] private JuanAndresTool[] _tools;

    [Header("Metrics")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;

    [Header("Score")]
    [SerializeField] private ScoreProfileSO _scoreProfile;
    [SerializeField] private ScoreProfileEventChannelSO _scoreProfileChanged;

    [Header("Haptic Feedback")]
    [Range(0f, 1f)]
    [SerializeField] private float _successHapticAmplitude = 0.4f;

    [Min(0.01f)]
    [SerializeField] private float _successHapticDuration = 0.06f;

    [Range(0f, 1f)]
    [SerializeField] private float _failureHapticAmplitude = 0.6f;

    [Min(0.01f)]
    [SerializeField] private float _failureHapticDuration = 0.1f;


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
            FindFirstObjectByType<ExperienceMusicClock>();

        if (_musicClock == null)
        {
            Debug.LogError(
                "[JuanAndresHitEvaluator] No se encontró ExperienceMusicClock.",
                this
            );

            return;
        }

        _targetSpawner.TargetActivated +=
            HandleTargetActivated;

        for (int i = 0; i < _tools.Length; i++)
        {
            _tools[i].GestureDetected +=
                HandleGestureDetected;
        }

        _scoreProfileChanged.RaiseEvent(
            _scoreProfile
        );

        _isRunning = true;
    }


    public void EndExperience()
    {
        if (!_isRunning)
        {
            return;
        }

        _targetSpawner.TargetActivated -=
            HandleTargetActivated;

        for (int i = 0; i < _tools.Length; i++)
        {
            if (_tools[i] != null)
            {
                _tools[i].GestureDetected -=
                    HandleGestureDetected;
            }
        }

        _isRunning = false;
    }


    private void HandleTargetActivated(JuanAndresTarget target)
    {
        if (target == null)
        {
            return;
        }

        target.Expired +=
            HandleTargetExpired;
    }


    private void HandleGestureDetected(
        JuanAndresTarget target,
        JuanAndresTool tool,
        JuanAndresActionDirection actualDirection,
        Vector3 feedbackPosition)
    {
        if (!_isRunning ||
            target == null ||
            tool == null ||
            target.IsResolved)
        {
            return;
        }

        bool correctDirection =
            target.ExpectedDirection ==
            actualDirection;

        SceneWeaponEquipController.Hand expectedHand =
            GetExpectedHand(
                target.ExpectedTool
            );

        bool correctHand =
            tool.Hand ==
            expectedHand;

        bool correctTool =
            tool.ToolType ==
            target.ExpectedTool;

        bool correctInteraction =
            correctDirection &&
            correctHand &&
            correctTool;

        double expectedTime =
            target.ExpectedTime;

        DifficultyLevel difficulty =
            target.Difficulty;

        double actualTime =
            _musicClock.SongTime;

        if (!target.TryResolve())
        {
            return;
        }

        InteractionOutcome outcome =
            correctInteraction
                ? InteractionOutcome.Success
                : InteractionOutcome.Failed;

        PlayHapticFeedback(
            outcome,
            tool.Hand
        );

        InteractionResult result =
            new InteractionResult(
                minigameId: "JuanAndres",
                interactionType: InteractionType.GroomStroke,
                outcome: outcome,
                difficulty: difficulty,
                expectedTime: expectedTime,
                actualTime: actualTime,
                directionAccuracy:
                    correctDirection ? 1f : 0f,
                usedCorrectHand:
                    correctHand,
                feedbackPosition:
                    feedbackPosition
            );

        _interactionRegistered.RaiseEvent(
            result
        );
    }


    private void HandleTargetExpired(JuanAndresTarget target)
    {
        if (!_isRunning ||
            target == null)
        {
            return;
        }

        InteractionResult result =
            new InteractionResult(
                minigameId: "JuanAndres",
                interactionType: InteractionType.GroomStroke,
                outcome: InteractionOutcome.Missed,
                difficulty: target.Difficulty,
                expectedTime: target.ExpectedTime
            );

        _interactionRegistered.RaiseEvent(
            result
        );
    }


    private SceneWeaponEquipController.Hand GetExpectedHand(JuanAndresToolType tool)
    {
        return tool ==
            JuanAndresToolType.Soap
                ? SceneWeaponEquipController.Hand.Left
                : SceneWeaponEquipController.Hand.Right;
    }


    private void PlayHapticFeedback(
        InteractionOutcome outcome,
        SceneWeaponEquipController.Hand hand)
    {
        XRHapticFeedback haptics =
            XRHapticFeedback.Instance;

        if (haptics == null)
        {
            return;
        }

        float amplitude =
            outcome ==
            InteractionOutcome.Success
                ? _successHapticAmplitude
                : _failureHapticAmplitude;

        float duration =
            outcome ==
            InteractionOutcome.Success
                ? _successHapticDuration
                : _failureHapticDuration;

        if (hand ==
            SceneWeaponEquipController.Hand.Left)
        {
            haptics.PulseLeft(
                amplitude,
                duration
            );
        }
        else
        {
            haptics.PulseRight(
                amplitude,
                duration
            );
        }
    }


    private bool ValidateReferences()
    {
        if (_targetSpawner == null)
        {
            Debug.LogError(
                "[JuanAndresHitEvaluator] TargetSpawner no está asignado.",
                this
            );

            return false;
        }

        if (_tools == null ||
            _tools.Length == 0)
        {
            Debug.LogError(
                "[JuanAndresHitEvaluator] No hay Tools asignadas.",
                this
            );

            return false;
        }

        for (int i = 0; i < _tools.Length; i++)
        {
            if (_tools[i] == null)
            {
                Debug.LogError(
                    $"[JuanAndresHitEvaluator] Tool {i} es null.",
                    this
                );

                return false;
            }
        }

        if (_interactionRegistered == null)
        {
            Debug.LogError(
                "[JuanAndresHitEvaluator] InteractionRegistered no está asignado.",
                this
            );

            return false;
        }

        if (_scoreProfile == null ||
            _scoreProfileChanged == null)
        {
            Debug.LogError(
                "[JuanAndresHitEvaluator] Configuración de Score incompleta.",
                this
            );

            return false;
        }

        return true;
    }
}