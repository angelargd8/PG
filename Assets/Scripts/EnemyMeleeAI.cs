using UnityEngine;

[DisallowMultipleComponent]
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
    // RUNTIME
    // =========================

    private Transform target;

    private float attackTimer;


    private static readonly int IsMovingHash =
        Animator.StringToHash(
            "IsMoving"
        );

    private static readonly int AttackHash =
        Animator.StringToHash(
            "Attack"
        );


    // =========================
    // TARGET
    // =========================

    public void SetTarget(
        Transform newTarget
    )
    {
        target = newTarget;
    }


    // =========================
    // UNITY
    // =========================

    private void OnEnable()
    {
        attackTimer = 0f;
    }


    private void OnDisable()
    {
        target = null;

        attackTimer = 0f;
    }


    private void Update()
    {
        if (target == null)
        {
            SetMovingAnimation(false);

            return;
        }


        attackTimer -=
            Time.deltaTime;


        Vector3 direction =
            target.position -
            transform.position;


        // No queremos que el enemigo
        // intente subir/bajar hacia
        // la altura de la cámara.
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
            StopAndAttack(
                direction
            );

            return;
        }


        // =========================
        // CHASE
        // =========================

        ChaseTarget(
            direction
        );
    }


    // =========================
    // CHASE
    // =========================

    private void ChaseTarget(
        Vector3 direction
    )
    {
        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            SetMovingAnimation(false);

            return;
        }


        direction.Normalize();


        // Movimiento
        transform.position +=
            direction *
            moveSpeed *
            Time.deltaTime;


        // Rotación hacia jugador
        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );


        SetMovingAnimation(true);
    }


    // =========================
    // ATTACK
    // =========================

    private void StopAndAttack(
        Vector3 direction
    )
    {
        SetMovingAnimation(false);


        // Aunque esté atacando,
        // sigue mirando al jugador.
        if (
            direction.sqrMagnitude >
            0.001f
        )
        {
            direction.Normalize();


            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction
                );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }


        if (attackTimer > 0f)
        {
            return;
        }


        attackTimer =
            attackCooldown;


        PerformAttack();
    }


    // =========================
    // PERFORM ATTACK
    // =========================

    private void PerformAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger(
                AttackHash
            );
        }


        // Más adelante aquí NO recomiendo
        // aplicar directamente el daño.
        //
        // Lo ideal será que la animación
        // llame un Animation Event justo
        // cuando la guitarra/mano/arma
        // golpee al jugador.
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