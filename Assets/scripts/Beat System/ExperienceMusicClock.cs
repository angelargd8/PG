using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class ExperienceMusicClock : MonoBehaviour
{
    [SerializeField] private PlayableDirector _playableDirector;


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