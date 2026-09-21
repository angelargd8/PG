using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(ExperienceBeatPlayer))]
public sealed class BeatLightController : MonoBehaviour
{
    [Header("Beat Filter")]
    [Tooltip("Solo cambia de color si la intensidad del beat alcanza este valor.")]
    [Range(0f, 1f)]
    [SerializeField] private float _minimumIntensity = 0.7f;

    [Header("Colors")]
    [SerializeField] private Color _baseColor = Color.white;
    [SerializeField] private Color[] _palette =
    {
        Color.green,
        Color.red,
        Color.blue,
        new Color(1f, 1f, 0f, 1f)
    };

    [Tooltip("Cuanto se mezcla el color con el blanco. Tambien se pondera por la intensidad del beat.")]
    [Range(0f, 1f)]
    [SerializeField] private float _colorStrength = 1f;

    [Header("Pulse (Seconds)")]
    [Min(0f)]
    [SerializeField] private float _holdDuration = 0.04f;
    [Min(0.01f)]
    [SerializeField] private float _returnDuration = 0.16f;

    [Header("Debug")]
    [SerializeField] private bool _logPulses;

    private readonly List<GameObject> _sceneRoots = new List<GameObject>();
    private readonly List<Light> _lights = new List<Light>();
    private ExperienceBeatPlayer _beatPlayer;
    private Light _targetLight;
    private Color _originalColor;
    private bool _originalUseColorTemperature;
    private Color _pulseColor;
    private double _pulseStartTime;
    private int _nextColorIndex;
    private bool _pulseActive;
    private bool _warnedMissingLight;

    public Light CurrentLight => _targetLight;


    private void OnEnable()
    {
        _beatPlayer = GetComponent<ExperienceBeatPlayer>();
        _beatPlayer.BeatReached += HandleBeat;
        _beatPlayer.PlaybackReset += HandlePlaybackReset;
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        _nextColorIndex = 0;
        _warnedMissingLight = false;
    }


    private void OnDisable()
    {
        if (_beatPlayer != null)
        {
            _beatPlayer.BeatReached -= HandleBeat;
            _beatPlayer.PlaybackReset -= HandlePlaybackReset;
        }

        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        ReleaseLight();
    }


    private void OnValidate()
    {
        _minimumIntensity = Mathf.Clamp01(_minimumIntensity);
        _colorStrength = Mathf.Clamp01(_colorStrength);
        _holdDuration = Mathf.Max(0f, _holdDuration);
        _returnDuration = Mathf.Max(0.01f, _returnDuration);
    }


    private void Update()
    {
        if (_beatPlayer == null || !_beatPlayer.IsPlaying)
        {
            ResetPulse();
            return;
        }

        if (_targetLight != null &&
            !IsUsableLight(_targetLight, SceneManager.GetActiveScene()))
        {
            ReleaseLight();
        }

        if (_pulseActive && _targetLight != null)
        {
            ApplyPulse(_beatPlayer.SongTime);
        }
    }


    private void HandleBeat(BeatMapSO.Beat beat, int beatIndex)
    {
        if (beat.Intensity < _minimumIntensity || _palette == null || _palette.Length == 0)
        {
            return;
        }

        if (!IsUsableLight(_targetLight, SceneManager.GetActiveScene()) && !BindActiveSceneLight())
        {
            if (!_warnedMissingLight)
            {
                Debug.LogWarning("[BeatLightController] La escena activa necesita una Directional Light habilitada en modo Realtime o Mixed.", this);
                _warnedMissingLight = true;
            }

            return;
        }

        Color color = _palette[_nextColorIndex % _palette.Length];
        _nextColorIndex = (_nextColorIndex + 1) % _palette.Length;
        _pulseColor = Color.Lerp(_baseColor, color, beat.Intensity * _colorStrength);
        _pulseStartTime = beat.Time;
        _pulseActive = true;
        ApplyPulse(_beatPlayer.SongTime);

        if (_logPulses)
        {
            Debug.Log($"[BeatLightController] Beat {beatIndex + 1} | Intensidad: {beat.Intensity:F2} | Escena: {_targetLight.gameObject.scene.name}", this);
        }
    }


    private void ApplyPulse(double songTime)
    {
        double elapsed = songTime - _pulseStartTime;
        if (elapsed < 0.0 || elapsed >= _holdDuration + _returnDuration)
        {
            ResetPulse();
            return;
        }

        float progress = Mathf.Clamp01((float)(elapsed - _holdDuration) / _returnDuration);
        _targetLight.color = Color.Lerp(_pulseColor, _baseColor, Mathf.SmoothStep(0f, 1f, progress));
    }


    private void HandlePlaybackReset()
    {
        ResetPulse();
        _nextColorIndex = 0;
    }


    private void ResetPulse()
    {
        _pulseActive = false;
        if (_targetLight != null)
        {
            _targetLight.color = _baseColor;
        }
    }


    private void HandleActiveSceneChanged(Scene previous, Scene current)
    {
        ReleaseLight();
        _nextColorIndex = 0;
        _warnedMissingLight = false;
        if (_beatPlayer != null && _beatPlayer.IsPlaying)
        {
            BindActiveSceneLight();
        }
    }


    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene == SceneManager.GetActiveScene())
        {
            HandleActiveSceneChanged(scene, scene);
        }
    }


    private bool BindActiveSceneLight()
    {
        ReleaseLight();
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return false;
        }

        Light candidate = FindDirectionalLight(scene);
        if (candidate == null)
        {
            return false;
        }

        _targetLight = candidate;
        _originalColor = candidate.color;
        _originalUseColorTemperature = candidate.useColorTemperature;

        candidate.useColorTemperature = false;
        candidate.color = _baseColor;
        _warnedMissingLight = false;
        return true;
    }


    private Light FindDirectionalLight(Scene scene)
    {
        if (IsUsableLight(RenderSettings.sun, scene))
        {
            return RenderSettings.sun;
        }

        Light candidate = null;
        _sceneRoots.Clear();
        scene.GetRootGameObjects(_sceneRoots);

        foreach (GameObject root in _sceneRoots)
        {
            _lights.Clear();
            root.GetComponentsInChildren(false, _lights);
            foreach (Light light in _lights)
            {
                if (!IsUsableLight(light, scene))
                {
                    continue;
                }

                if (light.gameObject.name == "Directional Light")
                {
                    return light;
                }

                if (candidate == null || light.intensity > candidate.intensity)
                {
                    candidate = light;
                }
            }
        }

        return candidate;
    }


    private static bool IsUsableLight(Light light, Scene scene)
    {
        if (light == null || !light.isActiveAndEnabled ||
            light.gameObject.scene != scene || light.type != LightType.Directional)
        {
            return false;
        }

#if UNITY_EDITOR
        return light.lightmapBakeType != LightmapBakeType.Baked;
#else
        return light.bakingOutput.lightmapBakeType != LightmapBakeType.Baked;
#endif
    }


    private void ReleaseLight()
    {
        _pulseActive = false;
        if (_targetLight != null)
        {
            _targetLight.color = _originalColor;
            _targetLight.useColorTemperature = _originalUseColorTemperature;
        }

        _targetLight = null;
    }
}
