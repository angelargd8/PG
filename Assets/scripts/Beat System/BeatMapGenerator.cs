#if UNITY_EDITOR
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

        if (!TryReadSamples(audioClip, out float[] interleavedSamples))
        {
            return;
        }

        float[] samples = ReadMonoSamples(interleavedSamples, audioClip.channels);
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

        float[] intensities = BeatIntensityAnalyzer.Analyze(
            interleavedSamples, audioClip.channels, audioClip.frequency, beatTimes);

        CreateBeatMapAsset(audioClip, bpm, beatTimes, intensities);
    }


    [MenuItem("Tools/Music/Recalculate Beat Intensities")]
    public static void RecalculateBeatIntensities()
    {
        BeatMapSO beatMap = Selection.activeObject as BeatMapSO;
        if (beatMap == null || beatMap.AudioClip == null || beatMap.Beats.Count == 0)
        {
            Debug.LogError("[BeatMapGenerator] Selecciona un BeatMap con audio y beats en el Project.");
            return;
        }

        AudioClip audioClip = beatMap.AudioClip;
        if (!TryReadSamples(audioClip, out float[] interleavedSamples))
        {
            return;
        }

        float[] intensities = BeatIntensityAnalyzer.Analyze(
            interleavedSamples, audioClip.channels, audioClip.frequency, beatMap.BeatTimes);

        Undo.RecordObject(beatMap, "Recalculate Beat Intensities");
        beatMap.SetIntensities(intensities);
        EditorUtility.SetDirty(beatMap);
        AssetDatabase.SaveAssetIfDirty(beatMap);

        Debug.Log($"[BeatMapGenerator] Intensidades recalculadas para {intensities.Length} beats. Los tiempos y demas datos se conservaron.", beatMap);
    }


    [MenuItem("Tools/Music/Recalculate Beat Intensities", true)]
    private static bool CanRecalculateBeatIntensities()
    {
        return Selection.activeObject is BeatMapSO beatMap &&
            beatMap.AudioClip != null && beatMap.Beats.Count > 0;
    }


    private static bool TryReadSamples(AudioClip audioClip, out float[] samples)
    {
        samples = null;

        if (audioClip.samples <= 0 || audioClip.channels <= 0 || audioClip.frequency <= 0)
        {
            Debug.LogError("[BeatMapGenerator] El audio no contiene muestras validas.", audioClip);
            return false;
        }

        if (audioClip.loadState != AudioDataLoadState.Loaded)
        {
            audioClip.LoadAudioData();
            if (audioClip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogError("[BeatMapGenerator] El audio aun no esta cargado. Espera a que termine de cargar y vuelve a intentarlo.", audioClip);
                return false;
            }
        }

        samples = new float[audioClip.samples * audioClip.channels];
        if (!audioClip.GetData(samples, 0))
        {
            samples = null;
            Debug.LogError("[BeatMapGenerator] No se pudo leer el audio. Configura Load Type como Decompress On Load en el importador y pulsa Apply.", audioClip);
            return false;
        }

        return true;
    }


    private static float[] ReadMonoSamples(float[] interleavedSamples, int channels)
    {
        float[] monoSamples = new float[interleavedSamples.Length / channels];

        for (int i = 0; i < monoSamples.Length; i++)
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
        List<double> beatTimes,
        IReadOnlyList<float> intensities)
    {
        BeatMapSO beatMap = ScriptableObject.CreateInstance<BeatMapSO>();

        beatMap.SetData(audioClip, bpm, beatTimes);
        beatMap.SetIntensities(intensities);

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
#endif
