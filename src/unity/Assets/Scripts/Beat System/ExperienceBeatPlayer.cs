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
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "beat_player_enabled", this, null,
            "beat_map=" + ExperienceDiagnostics.Field("name", _beatMap != null ? _beatMap.name : "missing") +
            " beat_count=" + (_beatMap != null ? _beatMap.Beats.Count : 0) +
            " estimated_bpm=" + (_beatMap != null ? _beatMap.EstimatedBpm : 0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
            " max_beat_lateness_s=" + _maxBeatLateness.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
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
        PublishReset("component_disabled", _lastSongTime, SongTime);
    }


    private void OnValidate()
    {
        _maxBeatLateness = Mathf.Max(0f, _maxBeatLateness);
    }


    private void Update()
    {
        if (_observedBeatMap != _beatMap)
        {
            string previousMap = _observedBeatMap != null ? _observedBeatMap.name : "none";
            _observedBeatMap = _beatMap;
            _nextBeatIndex = 0;
            _hasSongTime = false;
            PublishReset("beat_map_changed:" + previousMap, _lastSongTime, SongTime);
        }

        if (!IsPlaying)
        {
            if (_wasPlaying)
            {
                _wasPlaying = false;
                string condition = Time.timeScale <= 0f ? "time_scale_paused" :
                    AudioListener.pause ? "audio_listener_paused" :
                    _musicClock == null || !_musicClock.IsPlaying ? "timeline_not_playing" : "beat_map_unavailable";
                PublishReset(condition, _lastSongTime, SongTime);
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
            PublishReset("timeline_time_reversed", _lastSongTime, songTime);
        }

        double previousSongTime = _lastSongTime;
        _lastSongTime = songTime;
        _hasSongTime = true;

        if (nextIndex <= _nextBeatIndex)
        {
            return;
        }

        if (nextIndex - _nextBeatIndex > 1)
        {
            int skippedCount = nextIndex - _nextBeatIndex - 1;
            ExperienceDiagnostics.RecordFrequentEvent("BeatReached", "historical_skipped", skippedCount, this);
            if (ExperienceDiagnostics.Level == ExperienceDiagnosticLevel.Detailed)
                ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Detailed, "BEAT", "beats_skipped_by_player_rule", this, null,
                    "reason=timeline_advanced_past_multiple_beats first_skipped_index=" + _nextBeatIndex +
                    " last_skipped_index=" + (nextIndex - 2) + " skipped_count=" + skippedCount +
                    " previous_time_s=" + previousSongTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                    " current_time_s=" + songTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            PublishReset("multiple_beats_skipped", previousSongTime, songTime,
                "skipped_count=" + skippedCount);
        }

        _nextBeatIndex = nextIndex;
        int beatIndex = nextIndex - 1;
        BeatMapSO.Beat beat = _beatMap.Beats[beatIndex];

        // Only publish the latest beat, never a burst of historical events after a seek.
        if (songTime - beat.Time <= _maxBeatLateness)
        {
            if (ExperienceDiagnostics.Level == ExperienceDiagnosticLevel.Detailed)
            {
                string payload = "beat_map=" + ExperienceDiagnostics.Field("name", _beatMap.name) +
                    " beat_index=" + beatIndex +
                    " scheduled_time_s=" + beat.Time.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                    " observed_time_s=" + songTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                    " publication_delay_s=" + (songTime - beat.Time).ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                    " max_beat_lateness_s=" + _maxBeatLateness.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                    " intensity=" + beat.Intensity.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) +
                    " active_scene=" + ExperienceDiagnostics.Field("name", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                ExperienceDiagnostics.PublishBeat(this, beat, beatIndex, BeatReached, payload);
            }
            else if (ExperienceDiagnostics.Level == ExperienceDiagnosticLevel.Flow)
                ExperienceDiagnostics.PublishBeat(this, beat, beatIndex, BeatReached, null);
            else BeatReached?.Invoke(beat, beatIndex);
        }
        else
        {
            ExperienceDiagnostics.RecordFrequentEvent("BeatReached", "omitted_late", this);
            if (ExperienceDiagnostics.Level == ExperienceDiagnosticLevel.Detailed)
                ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Detailed, "BEAT", "beat_omitted", this, null,
                    "reason=exceeded_max_lateness beat_map=" + ExperienceDiagnostics.Field("name", _beatMap.name) +
                    " beat_index=" + beatIndex +
                    " scheduled_time_s=" + beat.Time.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                    " observed_time_s=" + songTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                    " publication_delay_s=" + (songTime - beat.Time).ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                    " max_beat_lateness_s=" + _maxBeatLateness.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
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

    private void PublishReset(string condition, double previousTime, double currentTime, string extra = null)
    {
        if (!ExperienceDiagnostics.IsEnabled(ExperienceDiagnosticLevel.Flow))
        {
            PlaybackReset?.Invoke();
            return;
        }
        string times = "previous_time_s=" + previousTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
            " current_time_s=" + currentTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
            " segment_index=" + ExperienceDiagnostics.CurrentSegmentIndex +
            (string.IsNullOrEmpty(extra) ? string.Empty : " " + extra);
        ExperienceDiagnostics.PublishPlaybackReset(this, PlaybackReset, condition, times);
    }
}
