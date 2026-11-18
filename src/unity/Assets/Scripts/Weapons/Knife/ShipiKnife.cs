using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiKnife :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipiHitEvaluator _hitEvaluator;
    [SerializeField] private Collider _cutCollider;

    [Header("Cut Detection")]
    [Min(0f)]
    [SerializeField] private float _minimumCutSpeed = 0.5f;

 
    private Vector3 _previousCutPosition;
    private Vector3 _velocity;


    private void OnEnable()
    {
        _previousCutPosition = GetCutPosition();

        _velocity = Vector3.zero;
    }


    private void Update()
    {
        if (Time.deltaTime <= 0f)
        {
            return;
        }

        Vector3 currentPosition = GetCutPosition();

        _velocity =
            (currentPosition -
             _previousCutPosition) /
            Time.deltaTime;

        _previousCutPosition = currentPosition;
    }


    private Vector3 GetCutPosition()
    {
        if (_cutCollider != null)
        {
            return _cutCollider.bounds.center;
        }

        return transform.position;
    }


    private void OnTriggerEnter(
        Collider other)
    {
        if (_hitEvaluator == null)
        {
            return;
        }

        ShipiFood food =
            other.GetComponentInParent<
                ShipiFood
            >();

        if (food == null)
        {
            return;
        }

        if (_velocity.magnitude <
            _minimumCutSpeed)
        {
            return;
        }

        Vector3 feedbackPosition =
            other.ClosestPoint(
                transform.position
            );

        _hitEvaluator.EvaluateCut(
            food,
            _velocity,
            feedbackPosition
        );
    }
}