using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DannielRhythmDirector : MonoBehaviour, IExperienceRuntime
{
    [Header("Music")]
    [SerializeField] private BeatMapSO _beatMap;

    [Tooltip("Opcional. Se busca en ExperienceCore al iniciar la experiencia.")]
    [SerializeField] private ExperienceMusicClock _musicClock;

    [Header("Attack Pattern")]
    [Tooltip("Numero del primer beat de ataque, empezando en 1. El aviso ocurre un beat antes.")]
    [Min(2)]
    [SerializeField] private int _firstAttackBeat = 2;

    [Tooltip("Intervalo entre ataques. 2 produce: aviso, disparo, aviso, disparo.")]
    [Min(2)]
    [SerializeField] private int _attackEveryNBeats = 2;

    [Min(1)]
    [SerializeField] private int _maxAttackersPerBeat = 1;

    [Tooltip("Beats con mas retraso se omiten para evitar rafagas tras un salto del Timeline.")]
    [Min(0f)]
    [SerializeField] private float _maxBeatLateness = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool _logBeats;

    private readonly List<EnemyShooter> _shooters = new List<EnemyShooter>();
    private readonly List<EnemyShooter> _pendingAttackers = new List<EnemyShooter>();
    private int _nextBeatIndex;
    private int _nextShooterIndex;
    private double _lastSongTime;
    private bool _hasSongTime;

    public bool IsRunning { get; private set; }


    public void BeginExperience()
    {
        if (IsRunning || !isActiveAndEnabled)
        {
            return;
        }

        if (_beatMap == null || _beatMap.Beats.Count == 0)
        {
            Debug.LogError("[DannielRhythmDirector] Asigna un Beat Map con beats.", this);
            return;
        }

        if (_musicClock == null)
        {
            _musicClock = FindFirstObjectByType<ExperienceMusicClock>();
        }

        if (_musicClock == null)
        {
            Debug.LogError("[DannielRhythmDirector] No se encontro ExperienceMusicClock. Inicia desde el flujo que carga ExperienceCore.", this);
            return;
        }

        ClearPendingAttackers();
        _nextBeatIndex = 0;
        _nextShooterIndex = 0;
        _hasSongTime = false;
        IsRunning = true;
    }


    public void EndExperience()
    {
        IsRunning = false;
        _hasSongTime = false;
        ClearPendingAttackers();
    }


    private void OnDisable()
    {
        EndExperience();
    }


    private void OnValidate()
    {
        _firstAttackBeat = Mathf.Max(2, _firstAttackBeat);
        _attackEveryNBeats = Mathf.Max(2, _attackEveryNBeats);
        _maxAttackersPerBeat = Mathf.Max(1, _maxAttackersPerBeat);
        _maxBeatLateness = Mathf.Max(0f, _maxBeatLateness);
    }


    public void RegisterShooter(EnemyShooter shooter)
    {
        if (shooter != null && !_shooters.Contains(shooter))
        {
            _shooters.Add(shooter);
        }
    }


    public void UnregisterShooter(EnemyShooter shooter)
    {
        int index = _shooters.IndexOf(shooter);
        if (index >= 0)
        {
            _shooters.RemoveAt(index);
            if (index < _nextShooterIndex)
            {
                _nextShooterIndex--;
            }
        }

        _pendingAttackers.Remove(shooter);
        if (shooter != null)
        {
            shooter.ClearRhythmCue();
        }
    }


    private void Update()
    {
        if (!IsRunning || _beatMap == null || _musicClock == null ||
            !_musicClock.IsPlaying || Time.timeScale <= 0f)
        {
            return;
        }

        double songTime = _musicClock.SongTime;
        int nextIndex = FindFirstBeatAfter(songTime);

        if (_hasSongTime && songTime < _lastSongTime)
        {
            // Al retroceder, descartar avisos y continuar desde la nueva posicion.
            ClearPendingAttackers();
            _nextBeatIndex = nextIndex;
        }

        _lastSongTime = songTime;
        _hasSongTime = true;

        if (nextIndex <= _nextBeatIndex)
        {
            return;
        }

        int beatIndex = nextIndex - 1;
        if (nextIndex - _nextBeatIndex > 1)
        {
            ClearPendingAttackers();
        }

        _nextBeatIndex = nextIndex;

        // Procesar solo el beat mas reciente; no recuperar ataques antiguos en rafaga.
        if (songTime - _beatMap.Beats[beatIndex].Time > _maxBeatLateness)
        {
            ClearPendingAttackers();
            return;
        }

        if (IsAttackBeat(beatIndex))
        {
            FirePendingAttackers(beatIndex);
        }
        else if (IsAttackBeat(beatIndex + 1))
        {
            PrepareAttackers(beatIndex);
        }
    }


    private bool IsAttackBeat(int beatIndex)
    {
        int firstIndex = _firstAttackBeat - 1;
        return beatIndex >= firstIndex &&
            (beatIndex - firstIndex) % _attackEveryNBeats == 0;
    }


    private int FindFirstBeatAfter(double songTime)
    {
        int low = 0;
        int high = _beatMap.Beats.Count;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (_beatMap.Beats[middle].Time <= songTime)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }


    private void PrepareAttackers(int beatIndex)
    {
        ClearPendingAttackers();

        // No anunciar un ataque cuando ya no queda un beat para ejecutarlo.
        if (beatIndex + 1 >= _beatMap.Beats.Count)
        {
            return;
        }

        int checkedShooters = 0;
        while (checkedShooters < _shooters.Count &&
            _pendingAttackers.Count < _maxAttackersPerBeat)
        {
            _nextShooterIndex %= _shooters.Count;
            EnemyShooter shooter = _shooters[_nextShooterIndex];
            _nextShooterIndex++;
            checkedShooters++;

            if (shooter == null || !shooter.CanShootOnBeat)
            {
                continue;
            }

            _pendingAttackers.Add(shooter);
            shooter.ShowRhythmCue();
        }

        if (_logBeats)
        {
            Debug.Log($"[DannielRhythmDirector] Beat {beatIndex + 1}: aviso para {_pendingAttackers.Count} enemigo(s).", this);
        }
    }


    private void FirePendingAttackers(int beatIndex)
    {
        int shots = 0;
        foreach (EnemyShooter shooter in _pendingAttackers)
        {
            if (shooter != null && shooter.TryShootOnBeat())
            {
                shots++;
            }
        }

        ClearPendingAttackers();

        if (_logBeats)
        {
            Debug.Log($"[DannielRhythmDirector] Beat {beatIndex + 1}: {shots} disparo(s).", this);
        }
    }


    private void ClearPendingAttackers()
    {
        foreach (EnemyShooter shooter in _pendingAttackers)
        {
            if (shooter != null)
            {
                shooter.ClearRhythmCue();
            }
        }

        _pendingAttackers.Clear();
    }
}
