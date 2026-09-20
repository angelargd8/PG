using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public sealed class GuitarDamage : MonoBehaviour
{
    [Header("Damage")]
    [Min(1)]
    [SerializeField] private int damage = 1;

    [Tooltip("Tiempo minimo entre dos contactos nuevos contra el mismo enemigo.")]
    [Min(0f)]
    [SerializeField] private float repeatHitCooldown = 0.15f;

    [Header("Rhythm")]
    [Tooltip("Si se deja vacio, se busca el ExperienceBeatPlayer de ExperienceCore.")]
    [SerializeField] private ExperienceBeatPlayer beatPlayer;

    [Tooltip("Tolerancia antes o despues del beat, en segundos.")]
    [Min(0f)]
    [SerializeField] private float perfectWindowSeconds = 0.08f;

    [Min(0f)]
    [SerializeField] private float goodWindowSeconds = 0.15f;

    [Header("Musical Combo")]
    [Min(1)]
    [SerializeField] private int hitsPerMultiplier = 4;

    [Min(1)]
    [SerializeField] private int maxMultiplier = 4;

    [Min(0)]
    [SerializeField] private int perfectPoints = 100;

    [Min(0)]
    [SerializeField] private int goodPoints = 50;

    [Header("Combo Knockback")]
    [SerializeField] private bool enableComboKnockback = true;

    [Tooltip("Empuja al enemigo golpeado en los combos 4, 8, 12... con el valor inicial de 4.")]
    [Min(1)]
    [SerializeField] private int knockbackEveryHits = 4;

    [Tooltip("Velocidad inicial de retroceso en metros por segundo.")]
    [Min(0f)]
    [SerializeField] private float knockbackSpeed = 6f;

    [Tooltip("Segundos de retroceso antes de frenar. Las colisiones pueden acortarlo.")]
    [Min(0.01f)]
    [SerializeField] private float knockbackDuration = 0.25f;

    [Tooltip("Pausa adicional antes de reanudar la persecucion.")]
    [Min(0f)]
    [SerializeField] private float knockbackRecoveryDuration = 0.2f;

    [Header("Metrics (Optional)")]
    [Tooltip("Si se deja vacio, los resultados se registran directamente en MetricsSystem de ExperienceCore.")]
    [SerializeField] private InteractionResultEventChannelSO interactionRegistered;
    [SerializeField] private DifficultyLevel difficulty = DifficultyLevel.Normal;

    [Header("Rhythm Feedback (Optional)")]
    [Tooltip("Texto TMP para mostrar juicio, combo, multiplicador y puntos. En VR, usa un Canvas World Space.")]
    [SerializeField] private TMP_Text rhythmFeedbackText;
    [SerializeField] private ParticleSystem onBeatParticles;
    [SerializeField] private bool logRhythmHits = true;

    [Header("Haptic Feedback")]
    [SerializeField] private XRNode hapticHand = XRNode.RightHand;

    [Range(0f, 1f)]
    [SerializeField] private float hapticAmplitude = 0.6f;

    [Min(0.01f)]
    [SerializeField] private float hapticDuration = 0.08f;

    [Tooltip("Evita varias vibraciones seguidas por multiples colliders.")]
    [Min(0f)]
    [SerializeField] private float hapticCooldown = 0.1f;

    private sealed class EnemyContact
    {
        public uint SpawnVersion;
        public double LastHitTime = double.NegativeInfinity;
        public readonly HashSet<Collider> Colliders = new HashSet<Collider>();
    }

    private readonly GuitarRhythmCombo rhythmCombo = new GuitarRhythmCombo();
    private readonly Dictionary<EnemyController, EnemyContact> contacts =
        new Dictionary<EnemyController, EnemyContact>();
    private readonly List<EnemyController> expiredContacts = new List<EnemyController>();
    private MetricsSystem metricsSystem;
    private BeatMapSO observedBeatMap;
    private double lastSongTime;
    private bool hasSongTime;
    private bool warnedMissingMusic;
    private bool warnedMissingHaptics;
    private float nextHapticTime;

    public int Combo => rhythmCombo.Combo;
    public int BestCombo => rhythmCombo.BestCombo;
    public int Multiplier => rhythmCombo.Multiplier;
    public int Score => rhythmCombo.Score;
    public event Action<GuitarRhythmHit> RhythmHitEvaluated;

    private void Start()
    {
        ResolveDependencies();
        ShowFeedback("LISTO", 0);
    }

    private void OnValidate()
    {
        damage = Mathf.Max(1, damage);
        repeatHitCooldown = Mathf.Max(0f, repeatHitCooldown);
        perfectWindowSeconds = Mathf.Max(0f, perfectWindowSeconds);
        goodWindowSeconds = Mathf.Max(perfectWindowSeconds, goodWindowSeconds);
        hitsPerMultiplier = Mathf.Max(1, hitsPerMultiplier);
        maxMultiplier = Mathf.Max(1, maxMultiplier);
        perfectPoints = Mathf.Max(0, perfectPoints);
        goodPoints = Mathf.Max(0, goodPoints);
        knockbackEveryHits = Mathf.Max(1, knockbackEveryHits);
        knockbackSpeed = Mathf.Max(0f, knockbackSpeed);
        knockbackDuration = Mathf.Max(0.01f, knockbackDuration);
        knockbackRecoveryDuration = Mathf.Max(0f, knockbackRecoveryDuration);
    }

    private void Update()
    {
        ObserveTimeline();

        // Pool returns do not always produce Exit callbacks; discard the old life.
        expiredContacts.Clear();
        foreach (KeyValuePair<EnemyController, EnemyContact> entry in contacts)
        {
            if (entry.Key == null || !entry.Key.IsAlive || entry.Key.SpawnVersion != entry.Value.SpawnVersion)
            {
                expiredContacts.Add(entry.Key);
            }
        }

        foreach (EnemyController enemy in expiredContacts)
        {
            contacts.Remove(enemy);
        }
    }

    private void OnDisable()
    {
        contacts.Clear();
        nextHapticTime = 0f;
        // Hiding the weapon during a pause must not erase the score or combo.
    }

    private void ResolveDependencies()
    {
        if (beatPlayer == null)
        {
            beatPlayer = FindFirstObjectByType<ExperienceBeatPlayer>();
        }

        if (interactionRegistered == null && metricsSystem == null)
        {
            metricsSystem = FindFirstObjectByType<MetricsSystem>();
        }
    }

    private void ObserveTimeline()
    {
        if (beatPlayer == null || !beatPlayer.IsPlaying)
        {
            return;
        }

        double songTime = beatPlayer.SongTime;
        if (observedBeatMap != beatPlayer.BeatMap || (hasSongTime && songTime < lastSongTime - 0.001))
        {
            ResetRhythmSession();
            observedBeatMap = beatPlayer.BeatMap;
        }

        lastSongTime = songTime;
        hasSongTime = true;
    }

    public void ResetRhythmSession()
    {
        rhythmCombo.Reset();
        hasSongTime = false;
        ShowFeedback("LISTO", 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void OnTriggerExit(Collider other)
    {
        ReleaseContact(other);
    }

    private void OnCollisionExit(Collision collision)
    {
        ReleaseContact(collision.collider);
    }

    private void ReleaseContact(Collider other)
    {
        if (other == null)
        {
            return;
        }

        EnemyController enemy = other.GetComponentInParent<EnemyController>();
        if (enemy != null && contacts.TryGetValue(enemy, out EnemyContact contact))
        {
            contact.Colliders.Remove(other);
        }
    }

    private void HandleHit(Collider other)
    {
        if (!isActiveAndEnabled || other == null || Time.timeScale <= 0f || AudioListener.pause)
        {
            return;
        }

        EnemyController enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || !enemy.IsAlive)
        {
            return;
        }

        ResolveDependencies();
        if (beatPlayer != null && !beatPlayer.IsPlaying)
        {
            return;
        }

        if (!contacts.TryGetValue(enemy, out EnemyContact contact) || contact.SpawnVersion != enemy.SpawnVersion)
        {
            contact = new EnemyContact { SpawnVersion = enemy.SpawnVersion };
            contacts[enemy] = contact;
        }

        contact.Colliders.RemoveWhere(collider =>
            collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy);
        bool alreadyTouching = contact.Colliders.Count > 0;
        if (!contact.Colliders.Add(other) || alreadyTouching ||
            Time.timeAsDouble - contact.LastHitTime < repeatHitCooldown)
        {
            return;
        }

        contact.LastHitTime = Time.timeAsDouble;
        ObserveTimeline();

        GuitarRhythmHit hit = default;
        bool evaluated = beatPlayer != null && rhythmCombo.TryEvaluate(
            beatPlayer.BeatMap, beatPlayer.SongTime, perfectWindowSeconds, goodWindowSeconds,
            perfectPoints, goodPoints, hitsPerMultiplier, maxMultiplier, out hit);

        // Resolve damage first, so a finisher never pushes an enemy already returned to its pool.
        enemy.TakeDamage(damage);
        bool knockedBack = evaluated && TryApplyComboKnockback(enemy, hit);
        PlayHaptic(evaluated && hit.AddedToCombo ? hit.Grade : GuitarHitGrade.OffBeat);

        if (!evaluated)
        {
            if (!warnedMissingMusic)
            {
                Debug.LogWarning("[GuitarDamage] No se encontro el reloj/BeatMap de ExperienceCore. Este golpe solo aplica dano normal.", this);
                warnedMissingMusic = true;
            }
            return;
        }

        string label = hit.Grade == GuitarHitGrade.Perfect ? "PERFECTO" :
            hit.Grade == GuitarHitGrade.Good ? "BUENO" : "FUERA DE TIEMPO";
        if (hit.IsOnBeat && !hit.AddedToCombo)
        {
            label = "BEAT YA CONTADO";
        }
        else if (knockedBack)
        {
            label += " + EMPUJE";
        }

        if (hit.AddedToCombo && onBeatParticles != null)
        {
            onBeatParticles.Play(true);
        }

        // Multiple enemies hit on one beat must not inflate the musical success metrics.
        if (hit.AddedToCombo || !hit.IsOnBeat)
        {
            RegisterMetrics(hit);
        }

        ShowFeedback(label, hit.PointsAwarded);
        if (logRhythmHits)
        {
            Debug.Log($"[GuitarDamage] {label} | Beat {hit.BeatIndex + 1} | Desfase: {hit.TimingOffset * 1000.0:F0} ms | Combo: {Combo} | x{Multiplier} | +{hit.PointsAwarded} | Puntos: {Score}", this);
        }

        RhythmHitEvaluated?.Invoke(hit);
    }

    private bool TryApplyComboKnockback(EnemyController enemy, GuitarRhythmHit hit)
    {
        if (!enableComboKnockback || !hit.AddedToCombo || hit.Combo <= 0 ||
            hit.Combo % Mathf.Max(1, knockbackEveryHits) != 0 || !enemy.IsAlive)
        {
            return false;
        }

        EnemyMeleeAI meleeAI = enemy.GetComponent<EnemyMeleeAI>();
        return meleeAI != null && meleeAI.TryApplyKnockback(
            transform.position, knockbackSpeed, knockbackDuration, knockbackRecoveryDuration);
    }

    private void RegisterMetrics(GuitarRhythmHit hit)
    {
        InteractionResult result = new InteractionResult(
            minigameId: "Joaquin",
            interactionType: InteractionType.GuitarHit,
            outcome: hit.IsOnBeat ? InteractionOutcome.Success : InteractionOutcome.Failed,
            difficulty: difficulty,
            expectedTime: hit.ExpectedTime,
            actualTime: hit.ActualTime);

        if (interactionRegistered != null)
        {
            interactionRegistered.RaiseEvent(result);
        }
        else if (metricsSystem != null)
        {
            metricsSystem.RegisterInteraction(result);
        }
    }

    private void ShowFeedback(string label, int points)
    {
        if (rhythmFeedbackText != null)
        {
            rhythmFeedbackText.text = $"{label}  +{points}\nCombo {Combo}  x{Multiplier}\nPuntos {Score}";
        }
    }

    private void PlayHaptic(GuitarHitGrade grade)
    {
        if (Time.time < nextHapticTime)
        {
            return;
        }

        nextHapticTime = Time.time + hapticCooldown;
        XRHapticFeedback haptics = XRHapticFeedback.Instance;
        if (haptics == null)
        {
            if (!warnedMissingHaptics)
            {
                Debug.LogWarning("[GuitarDamage] No se encontro XRHapticFeedback.", this);
                warnedMissingHaptics = true;
            }
            return;
        }

        float strength = grade == GuitarHitGrade.Perfect ? 1.6f : grade == GuitarHitGrade.Good ? 1.25f : 1f;
        haptics.Pulse(hapticHand, Mathf.Clamp01(hapticAmplitude * strength), hapticDuration);
    }
}
