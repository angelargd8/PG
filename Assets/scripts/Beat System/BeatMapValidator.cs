using UnityEngine;

[DisallowMultipleComponent]
public sealed class BeatMapValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BeatMapSO _beatMap;
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _clickSource;
    [SerializeField] private AudioClip _clickClip;

    private int _nextBeatIndex;
    private bool _isRunning;


    private void Start()
    {
        StartValidation();
    }

    private void Update()
    {
        if (!_isRunning || _beatMap == null || _musicSource == null)
        {
            return;
        }

        if (_nextBeatIndex >= _beatMap.BeatTimes.Count)
        {
            return;
        }

        double songTime = _musicSource.time;
        double nextBeatTime = _beatMap.BeatTimes[_nextBeatIndex];

        if (songTime >= nextBeatTime)
        {
            TriggerBeat(_nextBeatIndex, nextBeatTime);
            _nextBeatIndex++;
        }
    }


    public void StartValidation()
    {
        if (_beatMap == null || _beatMap.AudioClip == null)
        {
            Debug.LogError("[BeatMapValidator] No se asignó un BeatMap válido.", this);
            return;
        }

        if (_musicSource == null)
        {
            Debug.LogError("[BeatMapValidator] No se asignó Music Source.", this);
            return;
        }

        _nextBeatIndex = 0;
        _isRunning = true;

        _musicSource.clip = _beatMap.AudioClip;
        _musicSource.time = 0f;
        _musicSource.Play();
    }


    public void StopValidation()
    {
        _isRunning = false;

        if (_musicSource != null)
        {
            _musicSource.Stop();
        }

        _nextBeatIndex = 0;
    }


    private void TriggerBeat(int beatIndex, double beatTime)
    {
        if (_clickSource != null && _clickClip != null)
        {
            _clickSource.PlayOneShot(_clickClip);
        }

        Debug.Log(
            $"[BeatMapValidator] Beat {beatIndex} | Time: {beatTime:F3}",
            this
        );
    }
}