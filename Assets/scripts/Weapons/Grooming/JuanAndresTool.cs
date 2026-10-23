
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
    [SerializeField] private float _minimumGestureAngle = 100f;

    [Min(0.001f)]
    [SerializeField] private float _minimumRadius = 0.04f;

    [Min(0f)]
    [SerializeField] private float _minimumStepAngle = 1f;

    [Min(1f)]
    [SerializeField] private float _maximumStepAngle = 35f;

    [Header("Contact Tolerance")]
    [Min(0f)]
    [SerializeField] private float _contactGraceSeconds = 0.2f;

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

    private Vector3 _gestureRight;
    private Vector3 _gestureUp;

    private bool _isTouchingTarget;
    private float _lastExitTime;

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
            return;

        if (!_activeTarget.gameObject.activeInHierarchy ||
            _activeTarget.IsResolved)
        {
            ResetTracking();
            return;
        }

        if (Time.timeScale <= 0f || AudioListener.pause)
            return;

        if (!_isTouchingTarget)
        {
            if (Time.time - _lastExitTime >= _contactGraceSeconds)
            {
                ResetTracking();
            }

            return;
        }

        if (!_gestureSubmitted)
        {
            TrackGesture();
        }
    }

    private void OnDisable()
    {
        ResetTracking();
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleTargetContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        HandleTargetContact(other);
    }

    private void HandleTargetContact(Collider other)
    {
        JuanAndresTarget target =
            other.GetComponentInParent<JuanAndresTarget>();

        if (target == null ||
            target.IsResolved ||
            !target.gameObject.activeInHierarchy)
        {
            return;
        }

        if (_activeTarget == null)
        {
            BeginTracking(target);
        }

        if (target != _activeTarget)
            return;

        if (!_isTouchingTarget)
        {
            _hasPreviousRadial = false;
        }

        _isTouchingTarget = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (_activeTarget == null)
            return;

        JuanAndresTarget target =
            other.GetComponentInParent<JuanAndresTarget>();

        if (target != _activeTarget)
            return;

        _isTouchingTarget = false;
        _lastExitTime = Time.time;
        _hasPreviousRadial = false;
    }

    private void BeginTracking(JuanAndresTarget target)
    {
        _activeTarget = target;

        _gestureRight = target.transform.right.normalized;
        _gestureUp = target.transform.up.normalized;

        _accumulatedAngle = 0f;
        _hasPreviousRadial = false;
        _gestureSubmitted = false;
        _gestureStarted = false;

        _isTouchingTarget = true;
        _lastExitTime = -1f;

        if (_bubbleController != null)
        {
            _bubbleController.BeginContact();
        }

        TrySetInitialRadial();
    }

    private void TrackGesture()
    {
        if (!TryGetRadial(out Vector2 currentRadial))
        {
            _hasPreviousRadial = false;
            return;
        }

        if (!_hasPreviousRadial)
        {
            _previousRadial = currentRadial;
            _hasPreviousRadial = true;
            return;
        }

        float angleDelta = Vector2.SignedAngle(
            _previousRadial,
            currentRadial
        );

        float absoluteDelta = Mathf.Abs(angleDelta);

        _previousRadial = currentRadial;

        if (absoluteDelta < _minimumStepAngle)
            return;

        if (absoluteDelta > _maximumStepAngle)
        {
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

        _accumulatedAngle += angleDelta;

        if (Mathf.Abs(_accumulatedAngle) < _minimumGestureAngle)
            return;

        SubmitGesture();
    }

    private void ResetGestureProgress()
    {
        _accumulatedAngle = 0f;
        _hasPreviousRadial = false;
        _gestureStarted = false;
    }

    private void SubmitGesture()
    {
        JuanAndresTarget target = _activeTarget;

        if (target == null)
            return;

        JuanAndresActionDirection direction =
            _accumulatedAngle > 0f
                ? JuanAndresActionDirection.CounterClockwise
                : JuanAndresActionDirection.Clockwise;

        _gestureSubmitted = true;

        Vector3 feedbackPosition = InteractionPosition;

        if (_logDetectedGestures)
        {
            Debug.Log(
                $"[JuanAndresTool] " +
                $"{_toolType} | " +
                $"Detected: {direction} | " +
                $"Expected Tool: {target.ExpectedTool} | " +
                $"Expected Direction: {target.ExpectedDirection}",
                this
            );
        }

        GestureDetected?.Invoke(
            target,
            this,
            direction,
            feedbackPosition
        );

        if (_activeTarget != target)
            return;

        if (target.IsResolved ||
            !target.gameObject.activeInHierarchy)
        {
            ResetTracking();
        }
        else
        {
            _gestureSubmitted = false;
            ResetGestureProgress();
        }
    }

    private bool TryGetRadial(out Vector2 radial)
    {
        Vector3 offset =
            InteractionPosition -
            _activeTarget.transform.position;

        float horizontal =
            Vector3.Dot(offset, _gestureRight);

        float vertical =
            Vector3.Dot(offset, _gestureUp);

        radial = new Vector2(horizontal, vertical);

        if (radial.magnitude < _minimumRadius)
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
            return;

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

        _isTouchingTarget = false;
        _lastExitTime = -1f;
    }
}
