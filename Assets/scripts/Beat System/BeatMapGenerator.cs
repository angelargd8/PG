using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BeatMapGenerator
{
    private const int WindowSize = 1024;
    private const float MinimumBpm = 70f;
    private const float MaximumBpm = 180f;
    private const float PreferredMinimumBpm = 100f;
    private const float PreferredMaximumBpm = 180f;


    [MenuItem("Tools/Music/Generate Beat Map")]
    public static void GenerateBeatMap()
    {
        AudioClip audioClip = Selection.activeObject as AudioClip;

        if (audioClip == null)
        {
            Debug.LogError("[BeatMapGenerator] Selecciona un AudioClip en el Project.");
            return;
        }

        float[] samples = ReadMonoSamples(audioClip);
        float[] energyEnvelope = CalculateEnergyEnvelope(samples);

        float bpm = EstimateBpm(energyEnvelope, audioClip.frequency);
        double firstBeatTime = EstimateFirstBeatTime(
            energyEnvelope,
            audioClip.frequency,
            bpm
        );

        List<double> beatTimes = GenerateBeatTimes(
            firstBeatTime,
            bpm,
            audioClip.length
        );

        CreateBeatMapAsset(audioClip, bpm, beatTimes);
    }


    private static float[] ReadMonoSamples(AudioClip audioClip)
    {
        int channels = audioClip.channels;

        float[] interleavedSamples =
            new float[audioClip.samples * channels];

        audioClip.GetData(interleavedSamples, 0);

        float[] monoSamples = new float[audioClip.samples];

        for (int i = 0; i < audioClip.samples; i++)
        {
            float sum = 0f;

            for (int channel = 0; channel < channels; channel++)
            {
                sum += interleavedSamples[i * channels + channel];
            }

            monoSamples[i] = sum / channels;
        }

        return monoSamples;
    }


    private static float[] CalculateEnergyEnvelope(float[] samples)
    {
        int windowCount = samples.Length / WindowSize;

        float[] envelope = new float[windowCount];

        for (int window = 0; window < windowCount; window++)
        {
            float energy = 0f;
            int start = window * WindowSize;

            for (int i = 0; i < WindowSize; i++)
            {
                float sample = samples[start + i];
                energy += sample * sample;
            }

            envelope[window] = energy / WindowSize;
        }

        return envelope;
    }


    private static float EstimateBpm(float[] envelope, int sampleRate)
    {
        float envelopeRate = sampleRate / (float)WindowSize;

        int minimumLag = Mathf.RoundToInt(
            envelopeRate * 60f / MaximumBpm
        );

        int maximumLag = Mathf.RoundToInt(
            envelopeRate * 60f / MinimumBpm
        );

        float bestCorrelation = float.MinValue;
        int bestLag = minimumLag;

        for (int lag = minimumLag; lag <= maximumLag; lag++)
        {
            float correlation = 0f;

            for (int i = lag; i < envelope.Length; i++)
            {
                correlation += envelope[i] * envelope[i - lag];
            }

            if (correlation > bestCorrelation)
            {
                bestCorrelation = correlation;
                bestLag = lag;
            }
        }

        float bpm = 60f * envelopeRate / bestLag;

        while (bpm < PreferredMinimumBpm)
        {
            bpm *= 2f;
        }

        while (bpm > PreferredMaximumBpm)
        {
            bpm *= 0.5f;
        }

        return bpm;
    }


    private static double EstimateFirstBeatTime(
        float[] envelope,
        int sampleRate,
        float bpm)
    {
        float envelopeRate = sampleRate / (float)WindowSize;

        int beatLag = Mathf.RoundToInt(
            envelopeRate * 60f / bpm
        );

        float bestEnergy = float.MinValue;
        int bestOffset = 0;

        for (int offset = 0; offset < beatLag; offset++)
        {
            float energy = 0f;

            for (int i = offset; i < envelope.Length; i += beatLag)
            {
                energy += envelope[i];
            }

            if (energy > bestEnergy)
            {
                bestEnergy = energy;
                bestOffset = offset;
            }
        }

        return bestOffset / envelopeRate;
    }


    private static List<double> GenerateBeatTimes(
        double firstBeatTime,
        float bpm,
        float songLength)
    {
        List<double> beatTimes = new List<double>();

        double beatDuration = 60.0 / bpm;
        double currentTime = firstBeatTime;

        while (currentTime <= songLength)
        {
            beatTimes.Add(currentTime);
            currentTime += beatDuration;
        }

        return beatTimes;
    }


    private static void CreateBeatMapAsset(
        AudioClip audioClip,
        float bpm,
        List<double> beatTimes)
    {
        BeatMapSO beatMap = ScriptableObject.CreateInstance<BeatMapSO>();

        beatMap.SetData(audioClip, bpm, beatTimes);

        string clipPath = AssetDatabase.GetAssetPath(audioClip);

        string directory =
            System.IO.Path.GetDirectoryName(clipPath);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{directory}/{audioClip.name}_BeatMap.asset"
        );

        AssetDatabase.CreateAsset(beatMap, assetPath);
        AssetDatabase.SaveAssets();

        Selection.activeObject = beatMap;

        Debug.Log(
            $"[BeatMapGenerator] BeatMap generado | " +
            $"BPM: {bpm:F2} | " +
            $"Beats: {beatTimes.Count}",
            beatMap
        );
    }
}