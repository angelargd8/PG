using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PlayerBeatVFXController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Opcional. Se busca el ExperienceBeatPlayer de ExperienceCore.")]
    [SerializeField] private ExperienceBeatPlayer _beatPlayer;

    [Tooltip("Opcional. Se busca la camara con el tag MainCamera del jugador.")]
    [SerializeField] private Transform _playerHead;

    [Tooltip("Si esta vacio, controla todos los Particle Systems de este objeto y sus hijos.")]
    [SerializeField] private ParticleSystem[] _particleSystems;

    [Header("Follow Player")]
    [Tooltip("Desplazamiento en metros respecto a la cabeza, en ejes del mundo.")]
    [SerializeField] private Vector3 _positionOffset = new Vector3(0f, -0.6f, 0f);

    [Header("Beat Filter")]
    [Range(0f, 1f)]
    [SerializeField] private float _minimumIntensity = 0.7f;

    [Tooltip("Particulas por sistema al alcanzar Minimum Intensity.")]
    [Min(1)]
    [SerializeField] private int _minParticlesPerBeat = 8;

    [Tooltip("Particulas por sistema cuando la intensidad llega a 1.")]
    [Min(1)]
    [SerializeField] private int _maxParticlesPerBeat = 20;

    [Header("Debug")]
    [SerializeField] private bool _logPulses;

    private readonly List<ParticleSystem> _systems = new List<ParticleSystem>();
    private ExperienceBeatPlayer _subscribedBeatPlayer;
    private float _nextDependencySearch;
    private double _lastBeatTime = double.NegativeInfinity;
    private bool _hasEmitted;


    private void OnEnable()
    {
        PrepareParticles();
        ResolveDependencies();
        FollowPlayer();
        _nextDependencySearch = Time.unscaledTime + 0.5f;
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
    }


    private void Start()
    {
        ResolveDependencies();
        if (_systems.Count == 0)
        {
            Debug.LogWarning("[PlayerBeatVFXController] Agrega las particulas como hijas de PlayerBeatVFX o asigna Particle Systems.", this);
        }

        if (_beatPlayer == null || _playerHead == null)
        {
            Debug.LogWarning("[PlayerBeatVFXController] Falta Beat Player o Player Head. Inicia desde Bootstrap para cargar el jugador y ExperienceCore, o asigna las referencias.", this);
        }
    }


    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        Unsubscribe();
        ClearParticles();
    }


    private void OnValidate()
    {
        _minimumIntensity = Mathf.Clamp01(_minimumIntensity);
        _minParticlesPerBeat = Mathf.Max(1, _minParticlesPerBeat);
        _maxParticlesPerBeat = Mathf.Max(_minParticlesPerBeat, _maxParticlesPerBeat);
    }


    private void LateUpdate()
    {

        if (Time.unscaledTime >= _nextDependencySearch)
        {
            ResolveDependencies();
            _nextDependencySearch = Time.unscaledTime + 0.5f;
        }

        bool hasPlayer = FollowPlayer();
        if (_hasEmitted && (!hasPlayer || _subscribedBeatPlayer == null ||
            !_subscribedBeatPlayer.IsPlaying || Time.timeScale <= 0f || AudioListener.pause))
        {
            ClearParticles();
        }
    }


    private void ResolveDependencies()
    {
        if (_playerHead == null)
        {
            Camera playerCamera = Camera.main;
            if (playerCamera != null)
            {
                _playerHead = playerCamera.transform;
            }
        }

        if (_beatPlayer == null)
        {
            _beatPlayer = FindFirstObjectByType<ExperienceBeatPlayer>();
        }

        if (_subscribedBeatPlayer == _beatPlayer)
        {
            return;
        }

        Unsubscribe();
        ClearParticles();
        _subscribedBeatPlayer = _beatPlayer;
        if (_subscribedBeatPlayer != null)
        {
            _subscribedBeatPlayer.BeatReached += HandleBeat;
            _subscribedBeatPlayer.PlaybackReset += ClearParticles;
        }
    }


    private void Unsubscribe()
    {
        if (_subscribedBeatPlayer != null)
        {
            _subscribedBeatPlayer.BeatReached -= HandleBeat;
            _subscribedBeatPlayer.PlaybackReset -= ClearParticles;
        }

        _subscribedBeatPlayer = null;
    }


    private bool FollowPlayer()
    {
        if (_playerHead == null || !_playerHead.gameObject.activeInHierarchy)
        {
            return false;
        }

        transform.SetPositionAndRotation(_playerHead.position + _positionOffset, Quaternion.identity);
        return true;
    }


    private void PrepareParticles()
    {
        _systems.Clear();
        ParticleSystem[] candidates = _particleSystems != null && _particleSystems.Length > 0
            ? _particleSystems
            : GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particles in candidates)
        {
            if (particles == null || _systems.Contains(particles))
            {
                continue;
            }

            _systems.Add(particles);
            var main = particles.main;

            main.stopAction = ParticleSystemStopAction.None;
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            main.playOnAwake = false;
            main.prewarm = false;
            main.loop = true;
            main.startDelay = 0f;
            main.useUnscaledTime = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.enabled = false;
        }

        _lastBeatTime = double.NegativeInfinity;
        _hasEmitted = false;
    }


    private void HandleBeat(BeatMapSO.Beat beat, int beatIndex)
    {
        if (!isActiveAndEnabled || beat == null || _subscribedBeatPlayer == null ||
            !_subscribedBeatPlayer.IsPlaying || Time.timeScale <= 0f || AudioListener.pause ||
            float.IsNaN(beat.Intensity) || beat.Intensity < _minimumIntensity ||
            beat.Time == _lastBeatTime || !FollowPlayer())
        {
            return;
        }

        float strength = _minimumIntensity < 1f
            ? Mathf.InverseLerp(_minimumIntensity, 1f, beat.Intensity)
            : 1f;
        int minimum = Mathf.Max(1, _minParticlesPerBeat);
        int maximum = Mathf.Max(minimum, _maxParticlesPerBeat);
        int requestedCount = Mathf.RoundToInt(Mathf.Lerp(minimum, maximum, strength));
        int emittedCount = 0;

        foreach (ParticleSystem particles in _systems)
        {
            if (particles == null || !particles.gameObject.activeInHierarchy)
            {
                continue;
            }

            int count = Mathf.Min(requestedCount, particles.main.maxParticles - particles.particleCount);
            if (count <= 0)
            {
                continue;
            }


            if (!particles.isPlaying)
            {
                particles.Play(false);
            }

            particles.Emit(count);
            emittedCount += count;
        }

        _lastBeatTime = beat.Time;
        _hasEmitted |= emittedCount > 0;
        if (_logPulses && emittedCount > 0)
        {
            Debug.Log($"[PlayerBeatVFXController] Beat {beatIndex + 1} | Intensidad: {beat.Intensity:F2} | Particulas: {emittedCount}", this);
        }
    }


    private void ClearParticles()
    {
        foreach (ParticleSystem particles in _systems)
        {
            if (particles != null)
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        _lastBeatTime = double.NegativeInfinity;
        _hasEmitted = false;
    }


    private void HandleActiveSceneChanged(Scene previous, Scene current)
    {
        ClearParticles();
        ResolveDependencies();
        FollowPlayer();
    }
}
