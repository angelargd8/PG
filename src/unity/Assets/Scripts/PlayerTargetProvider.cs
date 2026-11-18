using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerTargetProvider : MonoBehaviour
{
    public static PlayerTargetProvider Instance
    {
        get;
        private set;
    }


    [Header("Target")]

    [SerializeField]
    private Transform enemyTarget;


    public Transform EnemyTarget =>
        enemyTarget;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "[PlayerTargetProvider] Ya existe una instancia.",
                this
            );

            return;
        }

        Instance = this;


        if (enemyTarget == null)
        {
            Debug.LogError(
                "[PlayerTargetProvider] " +
                "No se asignó Enemy Target.",
                this
            );
        }
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}