using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class JuanAndresTool : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private JuanAndresToolType _toolType;
    [SerializeField] private SceneWeaponEquipController.Hand _hand;
    [SerializeField] private Transform _interactionPoint;

    [Header("Gesture Detection")]
    [Min(1f)]
    [SerializeField] private float _minimumGestureAngle = 270f;

    [Min(0.001f)]
    [SerializeField] private float _minimumRadius = 0.04f;

    [Min(0f)]
    [SerializeField] private float _minimumStepAngle = 0.5f;

    [Min(1f)]
    [SerializeField] private float _maximumStepAngle = 35f;

    [Header("Debug")]
    [SerializeField] private bool _logDetectedGestures = true;

    [Header("Particles")]
    [SerializeField] private JuanAndresToolBubbleController _bubbleController;


    private JuanAndresTarget _activeTarget;
    private Vector2 _previousRadial;
    private float _accumulatedAngle;
    private bool _hasPreviousRadial;
    private bool _gestureSubmitted;
    private bool _gestureStarted;


    public JuanAndresToolType ToolType => _toolType;
    public SceneWeaponEquipController.Hand Hand => _hand;
    public Vector3 InteractionPosition =>
        _interactionPoint != null
            ? _interactionPoint.position
            : transform.position;
    public event Action<
        JuanAndresTarget,
        JuanAndresTool,
        JuanAndresActionDirection,
        Vector3> GestureDetected;
    public event Action<JuanAndresTarget, JuanAndresTool> GestureStarted;


    private void Update()
    {
        if (_activeTarget == null)
        {
            return;
        }

        if (!_activeTarget.gameObject.activeInHierarchy ||
            _activeTarget.IsResolved)
        {
            ResetTracking();
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        JuanAndresTarget target =
            other.GetComponentInParent<JuanAndresTarget>();

        if (target == null ||
            target.IsResolved ||
            _activeTarget != null)
        {
            return;
        }

        BeginTracking(target);
    }


    private void OnTriggerStay(Collider other)
    {
        JuanAndresTarget target =
            other.GetComponentInParent<
                JuanAndresTarget
            >();

        if (target == null ||
            target.IsResolved)
        {
            return;
        }

        if (_activeTarget == null)
        {
            BeginTracking(target);
        }

        if (target != _activeTarget ||
            _gestureSubmitted)
        {
            return;
        }

        TrackGesture();
    }


    private void OnTriggerExit(Collider other)
    {
        if (_activeTarget == null)
        {
            return;
        }

        JuanAndresTarget target =
            other.GetComponentInParent<JuanAndresTarget>();

        if (target != _activeTarget)
        {
            return;
        }

        ResetTracking();
    }


    private void BeginTracking(JuanAndresTarget target)
    {
        _activeTarget = target;

        _accumulatedAngle = 0f;

        _hasPreviousRadial = false;
        _gestureSubmitted = false;
        _gestureStarted = false;

        if (_bubbleController != null)
        {
            _bubbleController.BeginContact();
        }

        TrySetInitialRadial();
    }


    private void TrackGesture()
    {
        if (!TryGetRadial(
            out Vector2 currentRadial))
        {
            ResetGestureProgress();

            return;
        }

        if (!_hasPreviousRadial)
        {
            _previousRadial =
                currentRadial;

            _hasPreviousRadial = true;

            return;
        }

        float angleDelta =
            Vector2.SignedAngle(
                _previousRadial,
                currentRadial
            );

        float absoluteDelta =
            Mathf.Abs(angleDelta);

        _previousRadial =
            currentRadial;


        if (absoluteDelta <
            _minimumStepAngle)
        {
            return;
        }

        if (absoluteDelta >
            _maximumStepAngle)
        {
            ResetGestureProgress(
                currentRadial
            );

            return;
        }


        if (!_gestureStarted)
        {
            _gestureStarted = true;

            GestureStarted?.Invoke(
                _activeTarget,
                this
            );
        }


        _accumulatedAngle +=
            angleDelta;


        if (Mathf.Abs(
            _accumulatedAngle) <
            _minimumGestureAngle)
        {
            return;
        }


        SubmitGesture();
    }


    private void ResetGestureProgress()
    {
        _accumulatedAngle = 0f;

        _hasPreviousRadial = false;

        _gestureStarted = false;
    }


    private void ResetGestureProgress(Vector2 currentRadial)
    {
        _accumulatedAngle = 0f;

        _previousRadial =
            currentRadial;

        _hasPreviousRadial = true;

        _gestureStarted = false;
    }


    private void SubmitGesture()
    {
        JuanAndresActionDirection direction =
            _accumulatedAngle > 0f
                ? JuanAndresActionDirection.CounterClockwise
                : JuanAndresActionDirection.Clockwise;

        _gestureSubmitted = true;

        Vector3 feedbackPosition =
            InteractionPosition;

        if (_logDetectedGestures)
        {
            Debug.Log(
                $"[JuanAndresTool] " +
                $"{_toolType} | " +
                $"Detected: {direction} | " +
                $"Expected Tool: {_activeTarget.ExpectedTool} | " +
                $"Expected Direction: {_activeTarget.ExpectedDirection}",
                this
            );
        }

        GestureDetected?.Invoke(
            _activeTarget,
            this,
            direction,
            feedbackPosition
        );
    }


    private bool TryGetRadial(out Vector2 radial)
    {
        Vector3 offset =
            InteractionPosition -
            _activeTarget.transform.position;

        float horizontal =
            Vector3.Dot(
                offset,
                _activeTarget.transform.right
            );

        float vertical =
            Vector3.Dot(
                offset,
                _activeTarget.transform.up
            );

        radial =
            new Vector2(
                horizontal,
                vertical
            );

        if (radial.magnitude <
            _minimumRadius)
        {
            radial = Vector2.zero;

            return false;
        }

        radial.Normalize();

        return true;
    }


    private void TrySetInitialRadial()
    {
        if (!TryGetRadial(out Vector2 radial))
        {
            return;
        }

        _previousRadial = radial;
        _hasPreviousRadial = true;
    }


    private void ResetTracking()
    {
        if (_activeTarget != null &&
            _bubbleController != null)
        {
            _bubbleController.EndContact();
        }

        _activeTarget = null;

        _accumulatedAngle = 0f;

        _hasPreviousRadial = false;
        _gestureSubmitted = false;
        _gestureStarted = false;
    }
}