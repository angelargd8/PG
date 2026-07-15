using UnityEngine;

public sealed class MainMenuEffectsController : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField]
    private VoidEventChannelSO mainMenuEntered;


    [Header("Particle Effects")]
    [SerializeField]
    private ParticleSystem[] particleSystems;


    [Header("Animated Objects")]
    [SerializeField]
    private Animator[] animators;



    [Header("Objects To Activate")]
    [SerializeField]
    private GameObject[] objectsToActivate;

    private static readonly int ShowTrigger =
        Animator.StringToHash("Show");

    private void OnEnable()
    {
        if (mainMenuEntered == null)
        {
            Debug.LogError(
                "MainMenuEntered no está asignado",
                this);

            return;
        }

        mainMenuEntered.Raised += HandleMenuEntered;

        Debug.Log(
            $"MainMenuEffectsController suscrito a '{mainMenuEntered.name}' ",
            this);
    }

    private void OnDisable()
    {
        if (mainMenuEntered == null)
        {
            return;
        }

        mainMenuEntered.Raised -= HandleMenuEntered;
    }

    private void HandleMenuEntered()
    {
        Debug.Log(
            "MainMenuEffectsController recibió MainMenuEntered",
            this);

        ActivateObjects();
        PlayParticles();
        PlayAnimations();
    }

    private void ActivateObjects()
    {
        foreach (GameObject target in objectsToActivate)
        {
            if (target == null)
            {
                continue;
            }

            target.SetActive(true);

            Debug.Log(
                $"Objeto activado: {target.name} ",
                target);
        }
    }

    private void PlayParticles()
    {
        foreach (ParticleSystem particles in particleSystems)
        {
            if (particles == null)
            {
                continue;
            }

            if (!particles.gameObject.activeInHierarchy)
            {
                Debug.LogWarning(
                    $"El sistema '{particles.name}' está dentro de un objeto inactivo ",
                    particles);

                continue;
            }

            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            particles.Play(true);

            Debug.Log(
                $"Partículas reproducidas: {particles.name} ",
                particles);
        }
    }

    private void PlayAnimations()
    {
        foreach (Animator animator in animators)
        {
            if (animator == null)
            {
                continue;
            }

            animator.SetTrigger(ShowTrigger);

            Debug.Log(
                $"Animación activada: {animator.name} ",
                animator);
        }
    }
}