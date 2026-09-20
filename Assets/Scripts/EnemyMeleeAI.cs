using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class EnemyMeleeAI : MonoBehaviour
{

    [Header("Movement")]

    [SerializeField]
    private float moveSpeed = 2f;

    [SerializeField]
    private float rotationSpeed = 8f;


    [Header("Attack")]

    [SerializeField]
    private float attackRange = 1.5f;

    [SerializeField]
    private float attackCooldown = 1.5f;


    [Header("Animation")]

    [SerializeField]
    private Animator animator;



    private Rigidbody rb;

    private Transform target;


    private float attackTimer;

    private bool shouldMove;

    private Vector3 moveDirection;

    private bool knockbackActive;
    private bool knockbackImpulsePending;
    private bool knockbackPaused;
    private Vector3 knockbackDirection;
    private Vector3 pausedKnockbackVelocity;
    private float knockbackSpeed;
    private float knockbackTimeRemaining;
    private float knockbackRecoveryRemaining;

    public bool IsKnockedBack => knockbackActive;


    private static readonly int IsMovingHash =
        Animator.StringToHash(
            "IsMoving"
        );

    private static readonly int AttackHash =
        Animator.StringToHash(
            "Attack"
        );


    private void Awake()
    {
        rb =
            GetComponent<Rigidbody>();
        KeepUpright();
    }


    private void OnEnable()
    {
        ClearKnockback();
        KeepUpright();
        attackTimer = 0f;

        shouldMove = false;

        moveDirection =
            Vector3.zero;
    }


    private void OnDisable()
    {
        ClearKnockback();
        target = null;

        attackTimer = 0f;

        shouldMove = false;

        moveDirection =
            Vector3.zero;


        if (rb != null && !rb.isKinematic)
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
        if (Time.timeScale > 0f && !AudioListener.pause)
        {
            KeepUpright();
        }

        if (knockbackActive)
        {
            ApplyKnockbackMovement();
            return;
        }

        if (Time.timeScale <= 0f || AudioListener.pause)
        {
            return;
        }

        ApplyMovement();
    }

    /// Temporarily yields pursuit to an impulse away from the player
    public bool TryApplyKnockback(Vector3 hitSource, float speed, float duration, float recoveryDuration)
    {
        if (!isActiveAndEnabled || rb == null || speed <= 0f || duration <= 0f ||
            Time.timeScale <= 0f || AudioListener.pause)
        {
            return false;
        }

        KeepUpright();
        Vector3 origin = target != null ? target.position : hitSource;
        Vector3 direction = rb.position - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = -transform.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        knockbackDirection = direction.normalized;
        knockbackSpeed = speed;
        knockbackTimeRemaining = duration;
        knockbackRecoveryRemaining = Mathf.Max(0f, recoveryDuration);
        knockbackActive = true;
        knockbackImpulsePending = true;
        knockbackPaused = false;
        shouldMove = false;
        moveDirection = Vector3.zero;
        attackTimer = Mathf.Max(attackTimer, attackCooldown);
        SetMovingAnimation(false);
        if (animator != null)
        {
            animator.ResetTrigger(AttackHash);
        }

        return true;
    }

    private void ApplyKnockbackMovement()
    {
        if (rb == null)
        {
            ClearKnockback();
            return;
        }

        if (Time.timeScale <= 0f || AudioListener.pause)
        {
            if (!knockbackPaused && !rb.isKinematic)
            {
                pausedKnockbackVelocity = rb.linearVelocity;
                StopHorizontalVelocity();
            }
            knockbackPaused = true;
            return;
        }

        if (knockbackPaused && !rb.isKinematic && !knockbackImpulsePending)
        {
            rb.linearVelocity = new Vector3(pausedKnockbackVelocity.x, rb.linearVelocity.y, pausedKnockbackVelocity.z);
        }
        knockbackPaused = false;

        if (knockbackTimeRemaining > 0f)
        {
            if (rb.isKinematic)
            {
                float step = Mathf.Min(Time.fixedDeltaTime, knockbackTimeRemaining);
                rb.MovePosition(rb.position + knockbackDirection * knockbackSpeed * step);
            }
            else if (knockbackImpulsePending)
            {
                // Apply once: collisions can stop the retreat instead of fighting a new force every frame.
                StopHorizontalVelocity();
                rb.AddForce(knockbackDirection * knockbackSpeed, ForceMode.VelocityChange);
            }

            knockbackImpulsePending = false;
            knockbackTimeRemaining = Mathf.Max(0f, knockbackTimeRemaining - Time.fixedDeltaTime);
            return;
        }

        StopHorizontalVelocity();
        knockbackRecoveryRemaining = Mathf.Max(0f, knockbackRecoveryRemaining - Time.fixedDeltaTime);
        if (knockbackRecoveryRemaining <= 0f)
        {
            ClearKnockback();
        }
    }

    private void KeepUpright()
    {
        if (rb == null)
        {
            return;
        }

        // These enemies turn through the AI. Contacts must not tip their physical body.
        const RigidbodyConstraints uprightConstraints =
            RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if ((rb.constraints & uprightConstraints) != uprightConstraints)
        {
            rb.constraints |= uprightConstraints;
        }

        if (!rb.isKinematic)
        {
            rb.angularVelocity = Vector3.zero;
        }

        if (Vector3.Dot(rb.rotation * Vector3.up, Vector3.up) >= 0.99999f)
        {
            return;
        }

        // Constraints prevent new tipping, but do not straighten an already fallen body.
        Vector3 forward = rb.rotation * Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            // Looking straight up/down: use the right axis to recover a stable heading.
            forward = Vector3.Cross(rb.rotation * Vector3.right, Vector3.up);
            forward.y = 0f;
        }

        if (forward.sqrMagnitude > 0.001f)
        {
            rb.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }

    private void StopHorizontalVelocity()
    {
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    private void ClearKnockback()
    {
        knockbackActive = false;
        knockbackImpulsePending = false;
        knockbackPaused = false;
        knockbackTimeRemaining = 0f;
        knockbackRecoveryRemaining = 0f;
        knockbackDirection = Vector3.zero;
        pausedKnockbackVelocity = Vector3.zero;
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



    // AI

    private void UpdateAI()
    {
        if (knockbackActive || Time.timeScale <= 0f || AudioListener.pause)
        {
            shouldMove = false;
            SetMovingAnimation(false);
            return;
        }

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
