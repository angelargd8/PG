using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class EnemyMeleeAI : MonoBehaviour
{
    // =========================
    // MOVEMENT
    // =========================

    [Header("Movement")]

    [SerializeField]
    private float moveSpeed = 2f;

    [SerializeField]
    private float rotationSpeed = 8f;


    // =========================
    // ATTACK
    // =========================

    [Header("Attack")]

    [SerializeField]
    private float attackRange = 1.5f;

    [SerializeField]
    private float attackCooldown = 1.5f;


    // =========================
    // ANIMATION
    // =========================

    [Header("Animation")]

    [SerializeField]
    private Animator animator;


    // =========================
    // REFERENCES
    // =========================

    private Rigidbody rb;

    private Transform target;


    // =========================
    // RUNTIME
    // =========================

    private float attackTimer;

    private bool shouldMove;

    private Vector3 moveDirection;


    // =========================
    // ANIMATOR HASHES
    // =========================

    private static readonly int IsMovingHash =
        Animator.StringToHash(
            "IsMoving"
        );

    private static readonly int AttackHash =
        Animator.StringToHash(
            "Attack"
        );


    // =========================
    // UNITY
    // =========================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody>();
    }


    private void OnEnable()
    {
        attackTimer = 0f;

        shouldMove = false;

        moveDirection =
            Vector3.zero;
    }


    private void OnDisable()
    {
        target = null;

        attackTimer = 0f;

        shouldMove = false;

        moveDirection =
            Vector3.zero;


        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }
    }


    private void Update()
    {
        UpdateAI();
    }


    private void FixedUpdate()
    {
        ApplyMovement();
    }


    // =========================
    // TARGET
    // =========================

    public void SetTarget(
        Transform newTarget
    )
    {
        target =
            newTarget;
    }


    // =========================
    // AI
    // =========================

    private void UpdateAI()
    {
        if (target == null)
        {
            shouldMove = false;

            SetMovingAnimation(
                false
            );

            return;
        }


        if (attackTimer > 0f)
        {
            attackTimer -=
                Time.deltaTime;
        }


        Vector3 direction =
            target.position -
            transform.position;


        direction.y = 0f;


        float distanceSquared =
            direction.sqrMagnitude;


        float attackRangeSquared =
            attackRange *
            attackRange;


        // =========================
        // ATTACK
        // =========================

        if (
            distanceSquared <=
            attackRangeSquared
        )
        {
            shouldMove = false;

            moveDirection =
                Vector3.zero;


            SetMovingAnimation(
                false
            );


            FaceTarget(
                direction
            );


            if (attackTimer <= 0f)
            {
                attackTimer =
                    attackCooldown;


                PerformAttack();
            }


            return;
        }


        // =========================
        // CHASE
        // =========================

        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            shouldMove = false;

            moveDirection =
                Vector3.zero;


            SetMovingAnimation(
                false
            );

            return;
        }


        moveDirection =
            direction.normalized;


        shouldMove = true;


        SetMovingAnimation(
            true
        );
    }


    // =========================
    // PHYSICS MOVEMENT
    // =========================

    private void ApplyMovement()
    {
        if (
            rb == null ||
            !shouldMove
        )
        {
            return;
        }


        Vector3 movement =
            moveDirection *
            moveSpeed *
            Time.fixedDeltaTime;


        rb.MovePosition(
            rb.position +
            movement
        );


        Quaternion targetRotation =
            Quaternion.LookRotation(
                moveDirection
            );


        Quaternion newRotation =
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed *
                Time.fixedDeltaTime
            );


        rb.MoveRotation(
            newRotation
        );
    }


    // =========================
    // FACE TARGET
    // =========================

    private void FaceTarget(
        Vector3 direction
    )
    {
        if (
            rb == null ||
            direction.sqrMagnitude <=
            0.001f
        )
        {
            return;
        }


        direction.Normalize();


        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );


        Quaternion newRotation =
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );


        rb.MoveRotation(
            newRotation
        );
    }


    // =========================
    // ATTACK
    // =========================

    private void PerformAttack()
    {
        if (animator == null)
        {
            return;
        }


        animator.SetTrigger(
            AttackHash
        );
    }


    // =========================
    // ANIMATION
    // =========================

    private void SetMovingAnimation(
        bool isMoving
    )
    {
        if (animator == null)
        {
            return;
        }


        animator.SetBool(
            IsMovingHash,
            isMoving
        );
    }
}