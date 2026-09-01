using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyFollowTarget : MonoBehaviour
{
    [SerializeField]
    private float speed = 2f;

    private Transform target;


    public void SetTarget(
        Transform newTarget
    )
    {
        target = newTarget;
    }


    private void OnDisable()
    {
        target = null;
    }


    private void Update()
    {
        if (target == null)
        {
            return;
        }


        Vector3 direction =
            target.position -
            transform.position;


        direction.y = 0f;


        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }


        direction.Normalize();


        transform.position +=
            direction *
            speed *
            Time.deltaTime;
    }
}