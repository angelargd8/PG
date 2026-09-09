using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BeatMap",
    menuName = "Scriptable Objects/Music/Beat Map"
)]
public sealed class BeatMapSO : ScriptableObject
{
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private float _estimatedBpm;
    [SerializeField] private List<double> _beatTimes = new List<double>();


    public AudioClip AudioClip => _audioClip;
    public float EstimatedBpm => _estimatedBpm;
    public IReadOnlyList<double> BeatTimes => _beatTimes;


    public void SetData(AudioClip audioClip, float estimatedBpm, List<double> beatTimes)
    {
        _audioClip = audioClip;
        _estimatedBpm = estimatedBpm;
        _beatTimes = beatTimes;
    }
}