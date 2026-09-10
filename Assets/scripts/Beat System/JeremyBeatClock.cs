using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class JeremyBeatClock :
    MonoBehaviour,
    IExperienceRuntime
{
    private PlayableDirector _playableDirector;
    private bool _isRunning;


    public double SongTime
    {
        get
        {
            if (!_isRunning || _playableDirector == null)
            {
                return 0.0;
            }

            return _playableDirector.time;
        }
    }

    public bool IsRunning => _isRunning;


    public void BeginExperience()
    {
        if (_isRunning)
        {
            return;
        }

        _playableDirector = FindFirstObjectByType<PlayableDirector>();

        if (_playableDirector == null)
        {
            Debug.LogError(
                "[JeremyBeatClock] No se encontró PlayableDirector.",
                this
            );

            return;
        }

        _isRunning = true;

        Debug.Log(
            $"[JeremyBeatClock] PlayableDirector encontrado: {_playableDirector.gameObject.name}",
            this
        );
    }


    public void EndExperience()
    {
        _isRunning = false;
        _playableDirector = null;
    }


    // private void Update()
    // {
    //     if (!_isRunning || _playableDirector == null)
    //     {
    //         return;
    //     }

    //     Debug.Log(
    //         $"[JeremyBeatClock] SongTime: {SongTime:F3} | " +
    //         $"DirectorState: {_playableDirector.state}",
    //         this
    //     );
    // }
}