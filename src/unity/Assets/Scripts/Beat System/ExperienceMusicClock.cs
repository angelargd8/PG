using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class ExperienceMusicClock : MonoBehaviour
{
    [SerializeField] private PlayableDirector _playableDirector;

    private void Awake()
    {
        ExperienceDiagnostics.RegisterMusicClock(this);
        ExperienceDiagnostics.Log(ExperienceDiagnosticLevel.Flow, "FLOW", "music_clock_ready", this, null,
            "playable_director=" + ExperienceDiagnostics.Field("name", _playableDirector != null ? _playableDirector.name : "missing"));
    }

    private void OnDestroy() => ExperienceDiagnostics.UnregisterMusicClock(this);


    public double SongTime
    {
        get
        {
            if (_playableDirector == null)
            {
                return 0.0;
            }

            return _playableDirector.time;
        }
    }

    public bool IsPlaying =>
        _playableDirector != null &&
        _playableDirector.state == PlayState.Playing;
}
