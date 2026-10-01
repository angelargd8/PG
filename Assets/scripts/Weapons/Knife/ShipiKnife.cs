using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiKnife :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ShipiHitEvaluator _hitEvaluator;

    [Header("Cut Detection")]
    [Min(0f)]
    [SerializeField]
    private float _minimumCutSpeed = 0.5f;


    private Vector3 _previousPosition;
    private Vector3 _velocity;


    private void OnEnable()
    {
        _previousPosition =
            transform.position;

        _velocity =
            Vector3.zero;
    }


    private void Update()
    {
        if (Time.deltaTime <= 0f)
        {
            return;
        }

        _velocity =
            (transform.position -
             _previousPosition) /
            Time.deltaTime;

        _previousPosition =
            transform.position;
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

        _hitEvaluator.EvaluateCut(
            food,
            _velocity
        );
    }
}