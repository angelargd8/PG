using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BeatMap",
    menuName = "Scriptable Objects/Music/Beat Map"
)]
public sealed class BeatMapSO : ScriptableObject
{
    public enum SectionType
    {
        Introduction,
        Verse,
        Chorus,
        Break
    }


    [Serializable]
    public sealed class Beat
    {
        [Tooltip("Tiempo desde el inicio de la cancion, en segundos.")]
        [Min(0f)]
        [SerializeField] private double _time;

        [Tooltip("Fuerza relativa entre 0 y 1, estimada desde el audio. Puedes corregirla manualmente; solo se reemplaza al recalcular intensidades.")]
        [Range(0f, 1f)]
        [SerializeField] private float _intensity = 1f;

        [Tooltip("Marca este beat como un acento musical.")]
        [SerializeField] private bool _isAccent;

        [Tooltip("Numero de compas, empezando en 1. Usa 0 si no esta definido.")]
        [Min(0)]
        [SerializeField] private int _bar;

        [Tooltip("Pulso dentro del compas, empezando en 1. Usa 0 si no esta definido.")]
        [Min(0)]
        [SerializeField] private int _beatInBar;

        [Tooltip("Divisiones del pulso: 1 = entero, 2 = mitades, 3 = tresillo, 4 = cuartos.")]
        [Min(1)]
        [SerializeField] private int _subdivisionsPerBeat = 1;

        [Tooltip("Posicion dentro de las subdivisiones, empezando en 1.")]
        [Min(1)]
        [SerializeField] private int _subdivisionIndex = 1;


        public double Time => _time;
        public float Intensity => _intensity;
        public bool IsAccent => _isAccent;
        public int Bar => _bar;
        public int BeatInBar => _beatInBar;
        public int SubdivisionsPerBeat => _subdivisionsPerBeat;
        public int SubdivisionIndex => _subdivisionIndex;


        public Beat()
        {
        }

        public Beat(double time)
        {
            _time = time;
        }

        internal void SetIntensity(float intensity)
        {
            _intensity = Mathf.Clamp01(intensity);
        }


        internal void Validate()
        {
            _time = Math.Max(0.0, _time);
            _intensity = Mathf.Clamp01(_intensity);
            _bar = Mathf.Max(0, _bar);
            _beatInBar = Mathf.Max(0, _beatInBar);
            _subdivisionsPerBeat = Mathf.Max(1, _subdivisionsPerBeat);
            _subdivisionIndex = Mathf.Clamp(_subdivisionIndex, 1, _subdivisionsPerBeat);
        }
    }


    [Serializable]
    public sealed class Section
    {
        [SerializeField] private SectionType _type;

        [Tooltip("Nombre opcional, por ejemplo: Coro 2.")]
        [SerializeField] private string _label;

        [Tooltip("Inicio incluido, en segundos desde el inicio de la cancion.")]
        [Min(0f)]
        [SerializeField] private double _startTime;

        [Tooltip("Fin excluido, en segundos desde el inicio de la cancion.")]
        [Min(0f)]
        [SerializeField] private double _endTime;


        public SectionType Type => _type;
        public string Label => _label;
        public double StartTime => _startTime;
        public double EndTime => _endTime;


        internal void Validate()
        {
            _startTime = Math.Max(0.0, _startTime);
            _endTime = Math.Max(_startTime, _endTime);
        }
    }


    [Header("Music")]
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private float _estimatedBpm;

    [Header("Time Signature ")]
    [Tooltip("Pulsos por compas. 4/4 es el valor inicial, no una deteccion automatica.")]
    [Min(1)]
    [SerializeField] private int _beatsPerBar = 4;

    [Tooltip("Figura del pulso: 4 = negra, 8 = corchea, etc.")]
    [Min(1)]
    [SerializeField] private int _beatUnit = 4;

    [Header("Beats")]
    [SerializeField] private List<Beat> _beats = new List<Beat>();

    [Header("Sections")]
    [SerializeField] private List<Section> _sections = new List<Section>();

    // mantener la lista de tiempos de beat para compatibilidad con versiones anteriores xd
    [HideInInspector]
    [SerializeField] private List<double> _beatTimes = new List<double>();

    private BeatTimeView _beatTimeView;


    public AudioClip AudioClip => _audioClip;
    public float EstimatedBpm => _estimatedBpm;
    public int BeatsPerBar => _beatsPerBar;
    public int BeatUnit => _beatUnit;
    public IReadOnlyList<Beat> Beats => _beats;
    public IReadOnlyList<Section> Sections => _sections;

    //sLos consumidores existentes leen los mismos tiempos sin necesidad de mantener una segunda lista editable
    public IReadOnlyList<double> BeatTimes =>
        _beatTimeView ?? (_beatTimeView = new BeatTimeView(this));


    private void OnEnable()
    {
        MigrateLegacyBeatTimes();
    }


    private void OnValidate()
    {
        MigrateLegacyBeatTimes();

        _beatsPerBar = Mathf.Max(1, _beatsPerBar);
        _beatUnit = Mathf.Max(1, _beatUnit);

        _beats.RemoveAll(beat => beat == null);
        foreach (Beat beat in _beats)
        {
            beat.Validate();
        }

        // El validador y el director de reproducción consumen los beats en orden cronologico
        _beats.Sort((left, right) => left.Time.CompareTo(right.Time));

        _sections.RemoveAll(section => section == null);
        foreach (Section section in _sections)
        {
            section.Validate();
        }

        _sections.Sort((left, right) => left.StartTime.CompareTo(right.StartTime));
    }


    public void SetData(AudioClip audioClip, float estimatedBpm, List<double> beatTimes)
    {
        _audioClip = audioClip;
        _estimatedBpm = estimatedBpm;
        _beats.Clear();
        _beatTimes.Clear();
        _sections.Clear();

        if (beatTimes == null)
        {
            return;
        }

        foreach (double time in beatTimes)
        {
            _beats.Add(new Beat(time));
        }
    }


    /// <summary>
    /// Updates only intensities, preserving beat times and all manually authored metadata.
    /// </summary>
    public void SetIntensities(IReadOnlyList<float> intensities)
    {
        if (intensities == null)
        {
            throw new ArgumentNullException(nameof(intensities));
        }

        if (intensities.Count != _beats.Count)
        {
            throw new ArgumentException("Provide one intensity per beat.", nameof(intensities));
        }

        // Validate all values before changing the asset, so invalid input cannot partially overwrite it.
        for (int i = 0; i < intensities.Count; i++)
        {
            if (float.IsNaN(intensities[i]) || float.IsInfinity(intensities[i]))
            {
                throw new ArgumentException("Intensities must be finite.", nameof(intensities));
            }
        }

        for (int i = 0; i < intensities.Count; i++)
        {
            _beats[i].SetIntensity(intensities[i]);
        }
    }


    private void MigrateLegacyBeatTimes()
    {
        if (_beatTimes == null || _beatTimes.Count == 0)
        {
            return;
        }

        if (_beats.Count == 0)
        {
            foreach (double time in _beatTimes)
            {
                _beats.Add(new Beat(time));
            }
        }

        _beatTimes.Clear();
    }


    private sealed class BeatTimeView : IReadOnlyList<double>
    {
        private readonly BeatMapSO _owner;

        public int Count => _owner._beats.Count;
        public double this[int index] => _owner._beats[index].Time;


        public BeatTimeView(BeatMapSO owner)
        {
            _owner = owner;
        }


        public IEnumerator<double> GetEnumerator()
        {
            foreach (Beat beat in _owner._beats)
            {
                yield return beat.Time;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
