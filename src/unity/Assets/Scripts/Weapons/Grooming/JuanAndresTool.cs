using System;
using System.Collections.Generic;
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

    [Header("Target Selection")]
    [Tooltip("Distance from InteractionPoint to a target center. 0 = no extra limit beyond the collider overlap.")]
    [Min(0f)]
    [SerializeField] private float _maximumSelectionDistance = 0f;

    [Tooltip("A new target must be this much closer (in meters) to replace the current candidate.")]
    [Min(0f)]
    [SerializeField] private float _switchHysteresis = 0.05f;

    [Tooltip("Lock the selected target after this much intentional circular motion.")]
    [Min(1f)]
    [SerializeField] private float _lockGestureAngle = 15f;

    [Header("Contact Tolerance")]
    [Min(0f)]
    [SerializeField] private float _contactGraceSeconds = 0.2f;

    [Header("Rearm After a Gesture")]
    [Min(0f)]
    [SerializeField] private float _rearmDelaySeconds = 0.15f;

    [Min(0f)]
    [SerializeField] private float _rearmTravelDistance = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool _logDetectedGestures = true;

    [Header("Particles")]
    [SerializeField] private JuanAndresToolBubbleController _bubbleController;

    // Register every target currently overlapping this tool's collider.
    private readonly HashSet<JuanAndresTarget> _overlappingTargets =
        new HashSet<JuanAndresTarget>();
    private readonly List<JuanAndresTarget> _staleTargets =
        new List<JuanAndresTarget>();

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

    private float _nextAllowedSelectionTime;
    private Vector3 _lastSubmissionPosition;
    private bool _requiresRearmMovement;

    public JuanAndresToolType ToolType => _toolType;
    public SceneWeaponEquipController.Hand Hand => _hand;
    public Vector3 InteractionPosition =>
        _interactionPoint != null ? _interactionPoint.position : transform.position;

    public event Action<
        JuanAndresTarget,
        JuanAndresTool,
        JuanAndresActionDirection,
        Vector3> GestureDetected;

    public event Action<JuanAndresTarget, JuanAndresTool> GestureStarted;

    private void Update()
    {
        RemoveInvalidContacts();

        if (_activeTarget != null && !IsValidTarget(_activeTarget))
        {
            ResetTracking();
        }

        if (Time.timeScale <= 0f || AudioListener.pause)
            return;

        if (IsRearming())
            return;

        // Before locking a gesture, choose the nearest overlapping center.
        if (!_gestureStarted)
        {
            SelectClosestTarget();
        }

        if (_activeTarget == null)
            return;

        bool touching = _overlappingTargets.Contains(_activeTarget);
        if (touching)
        {
            if (!_isTouchingTarget)
            {
                // Avoid a large angle jump after briefly losing contact.
                _hasPreviousRadial = false;
            }

            _isTouchingTarget = true;

            if (!_gestureSubmitted)
                TrackGesture();
        }
        else
        {
            if (_isTouchingTarget)
            {
                _isTouchingTarget = false;
                _lastExitTime = Time.time;
                _hasPreviousRadial = false;
            }

            if (Time.time - _lastExitTime >= _contactGraceSeconds)
                ResetTracking();
        }
    }

    private void OnDisable()
    {
        ResetTracking();
        _overlappingTargets.Clear();
        _staleTargets.Clear();
        _nextAllowedSelectionTime = 0f;
        _requiresRearmMovement = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Recover overlaps that were present before the tool was enabled.
        RegisterContact(other);
    }

    private void OnTriggerExit(Collider other)
    {
        JuanAndresTarget target = other.GetComponentInParent<JuanAndresTarget>();
        if (target == null)
            return;

        _overlappingTargets.Remove(target);

        if (target == _activeTarget && _isTouchingTarget)
        {
            _isTouchingTarget = false;
            _lastExitTime = Time.time;
            _hasPreviousRadial = false;
        }
    }

    private void RegisterContact(Collider other)
    {
        JuanAndresTarget target = other.GetComponentInParent<JuanAndresTarget>();
        if (IsValidTarget(target))
            _overlappingTargets.Add(target);
    }

    private static bool IsValidTarget(JuanAndresTarget target)
    {
        return target != null &&
               target.gameObject.activeInHierarchy &&
               !target.IsResolved;
    }

    private void RemoveInvalidContacts()
    {
        _staleTargets.Clear();

        foreach (JuanAndresTarget target in _overlappingTargets)
        {
            if (!IsValidTarget(target))
                _staleTargets.Add(target);
        }

        foreach (JuanAndresTarget target in _staleTargets)
            _overlappingTargets.Remove(target);

        _staleTargets.Clear();
    }

    private void SelectClosestTarget()
    {
        JuanAndresTarget closest = null;
        float closestSqrDistance = float.PositiveInfinity;
        Vector3 point = InteractionPosition;
        float maxDistanceSqr = _maximumSelectionDistance * _maximumSelectionDistance;

        foreach (JuanAndresTarget candidate in _overlappingTargets)
        {
            if (!IsValidTarget(candidate))
                continue;

            float sqrDistance = (point - candidate.transform.position).sqrMagnitude;

            if (_maximumSelectionDistance > 0f && sqrDistance > maxDistanceSqr)
                continue;

            if (sqrDistance < closestSqrDistance)
            {
                closest = candidate;
                closestSqrDistance = sqrDistance;
            }
        }

        // Prevent flickering between two almost equally close targets.
        if (closest != null &&
            _activeTarget != null &&
            _activeTarget != closest &&
            _overlappingTargets.Contains(_activeTarget) &&
            IsValidTarget(_activeTarget))
        {
            float currentDistance = Vector3.Distance(
                point, _activeTarget.transform.position);
            float closestDistance = Mathf.Sqrt(closestSqrDistance);

            if (currentDistance <= closestDistance + _switchHysteresis)
                closest = _activeTarget;
        }

        if (closest == _activeTarget)
            return;

        ResetTracking();

        if (closest != null)
            BeginTracking(closest);
    }

    private bool IsRearming()
    {
        if (Time.time < _nextAllowedSelectionTime)
            return true;

        if (!_requiresRearmMovement)
            return false;

        float travelSqr = (InteractionPosition - _lastSubmissionPosition).sqrMagnitude;
        float requiredSqr = _rearmTravelDistance * _rearmTravelDistance;

        if (travelSqr < requiredSqr)
            return true;

        _requiresRearmMovement = false;
        return false;
    }

    private void BeginRearm()
    {
        _nextAllowedSelectionTime = Time.time + _rearmDelaySeconds;
        _lastSubmissionPosition = InteractionPosition;
        _requiresRearmMovement = _rearmTravelDistance > 0f;
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
            _bubbleController.BeginContact();

        TrySetInitialRadial();
    }

    private void TrackGesture()
    {
        if (!TryGetRadial(out Vector2 currentRadial))
        {
            // Keep accumulated progress when passing briefly near the center.
            _hasPreviousRadial = false;
            return;
        }

        if (!_hasPreviousRadial)
        {
            _previousRadial = currentRadial;
            _hasPreviousRadial = true;
            return;
        }

        float angleDelta = Vector2.SignedAngle(_previousRadial, currentRadial);
        float absoluteDelta = Mathf.Abs(angleDelta);
        _previousRadial = currentRadial;

        if (absoluteDelta < _minimumStepAngle)
            return;

        if (absoluteDelta > _maximumStepAngle)
            return;

        _accumulatedAngle += angleDelta;

        // Lock only after a meaningful movement, not a single noisy sample.
        if (!_gestureStarted &&
            Mathf.Abs(_accumulatedAngle) >=
            Mathf.Min(_lockGestureAngle, _minimumGestureAngle))
        {
            _gestureStarted = true;
            GestureStarted?.Invoke(_activeTarget, this);
        }

        if (Mathf.Abs(_accumulatedAngle) >= _minimumGestureAngle)
            SubmitGesture();
    }

    private void SubmitGesture()
    {
        JuanAndresTarget target = _activeTarget;
        if (target == null)
            return;

        JuanAndresActionDirection direction = _accumulatedAngle > 0f
            ? JuanAndresActionDirection.CounterClockwise
            : JuanAndresActionDirection.Clockwise;

        _gestureSubmitted = true;
        Vector3 feedbackPosition = InteractionPosition;

        if (_logDetectedGestures)
        {
            Debug.Log(
                $"[JuanAndresTool] {_toolType} | Detected: {direction} | " +
                $"Expected Tool: {target.ExpectedTool} | " +
                $"Expected Direction: {target.ExpectedDirection}",
                this);
        }

        GestureDetected?.Invoke(target, this, direction, feedbackPosition);

        if (_activeTarget != target)
            return;

        if (!IsValidTarget(target))
        {
            // One gesture cannot immediately resolve a neighboring target.
            BeginRearm();
            ResetTracking();
        }
        else
        {
            // The evaluator did not consume the gesture: allow a new attempt.
            _gestureSubmitted = false;
            ResetGestureProgress();
        }
    }

    private bool TryGetRadial(out Vector2 radial)
    {
        Vector3 offset = InteractionPosition - _activeTarget.transform.position;

        float horizontal = Vector3.Dot(offset, _gestureRight);
        float vertical = Vector3.Dot(offset, _gestureUp);
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

    private void ResetGestureProgress()
    {
        _accumulatedAngle = 0f;
        _hasPreviousRadial = false;
        _gestureStarted = false;
    }

    private void ResetTracking()
    {
        if (_activeTarget != null && _bubbleController != null)
            _bubbleController.EndContact();

        _activeTarget = null;
        _accumulatedAngle = 0f;
        _hasPreviousRadial = false;
        _gestureSubmitted = false;
        _gestureStarted = false;
        _isTouchingTarget = false;
        _lastExitTime = -1f;
    }
}
