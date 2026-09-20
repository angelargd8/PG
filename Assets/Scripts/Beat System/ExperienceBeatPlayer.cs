using System;
using UnityEngine;

/// <summary>
/// Publishes musical beats for shared effects while the experience Timeline is playing.
/// </summary>
[DisallowMultipleComponent]
public sealed class ExperienceBeatPlayer : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private BeatMapSO _beatMap;

    [Tooltip("Reloj de ExperienceCore. Si se deja vacio, se busca al iniciar.")]
    [SerializeField] private ExperienceMusicClock _musicClock;

    [Tooltip("Retraso maximo para emitir un beat. Los beats antiguos se omiten al saltar el Timeline.")]
    [Min(0f)]
    [SerializeField] private float _maxBeatLateness = 0.1f;

    public event Action<BeatMapSO.Beat, int> BeatReached;
    public event Action PlaybackReset;

    public BeatMapSO BeatMap => _beatMap;
    public double SongTime => _musicClock != null ? _musicClock.SongTime : 0.0;
    public bool IsPlaying =>
        isActiveAndEnabled && _beatMap != null && _beatMap.Beats.Count > 0 &&
        _musicClock != null && _musicClock.IsPlaying &&
        Time.timeScale > 0f && !AudioListener.pause;

    private BeatMapSO _observedBeatMap;
    private int _nextBeatIndex;
    private double _lastSongTime;
    private bool _hasSongTime;
    private bool _wasPlaying;


    private void Awake()
    {
        if (_musicClock == null)
        {
            _musicClock = FindFirstObjectByType<ExperienceMusicClock>();
        }
    }


    private void OnEnable()
    {
        _observedBeatMap = _beatMap;
        _nextBeatIndex = 0;
        _hasSongTime = false;
        _wasPlaying = false;
    }


    private void Start()
    {
        if (_beatMap == null || _beatMap.Beats.Count == 0)
        {
            Debug.LogError("[ExperienceBeatPlayer] Asigna un Beat Map con beats.", this);
        }

        if (_musicClock == null)
        {
            Debug.LogError("[ExperienceBeatPlayer] Falta ExperienceMusicClock. Coloca este componente en ExperienceCore y asigna su reloj.", this);
        }
    }


    private void OnDisable()
    {
        _wasPlaying = false;
        PlaybackReset?.Invoke();
    }


    private void OnValidate()
    {
        _maxBeatLateness = Mathf.Max(0f, _maxBeatLateness);
    }


    private void Update()
    {
        if (_observedBeatMap != _beatMap)
        {
            _observedBeatMap = _beatMap;
            _nextBeatIndex = 0;
            _hasSongTime = false;
            PlaybackReset?.Invoke();
        }

        if (!IsPlaying)
        {
            if (_wasPlaying)
            {
                _wasPlaying = false;
                PlaybackReset?.Invoke();
            }

            // Keep the cursor so resuming a pause does not publish the same beat twice.
            return;
        }

        _wasPlaying = true;
        double songTime = SongTime;
        int nextIndex = FindFirstBeatAfter(songTime);

        if (_hasSongTime && songTime < _lastSongTime)
        {
            _nextBeatIndex = nextIndex;
            PlaybackReset?.Invoke();
        }

        _lastSongTime = songTime;
        _hasSongTime = true;

        if (nextIndex <= _nextBeatIndex)
        {
            return;
        }

        if (nextIndex - _nextBeatIndex > 1)
        {
            PlaybackReset?.Invoke();
        }

        _nextBeatIndex = nextIndex;
        int beatIndex = nextIndex - 1;
        BeatMapSO.Beat beat = _beatMap.Beats[beatIndex];

        // Only publish the latest beat, never a burst of historical events after a seek.
        if (songTime - beat.Time <= _maxBeatLateness)
        {
            BeatReached?.Invoke(beat, beatIndex);
        }
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
}
