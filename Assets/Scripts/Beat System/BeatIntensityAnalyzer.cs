using System;
using System.Collections.Generic;

/// <summary>
/// Estimates relative beat strength from RMS energy in a 150 ms window.
/// This measures the audio mix near a beat, not individual instruments or accents.
/// </summary>
public static class BeatIntensityAnalyzer
{
    private const double WindowRadiusSeconds = 0.075;
    private const double SilenceThreshold = 0.00001;

    public static float[] Analyze(
        float[] interleavedSamples,
        int channels,
        int sampleRate,
        IReadOnlyList<double> beatTimes)
    {
        if (interleavedSamples == null)
        {
            throw new ArgumentNullException(nameof(interleavedSamples));
        }

        if (channels <= 0 || interleavedSamples.Length % channels != 0)
        {
            throw new ArgumentException("Samples must contain complete audio frames.", nameof(channels));
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (beatTimes == null)
        {
            throw new ArgumentNullException(nameof(beatTimes));
        }

        int frameCount = interleavedSamples.Length / channels;
        double duration = frameCount / (double)sampleRate;
        double[] levels = new double[beatTimes.Count];

        for (int beatIndex = 0; beatIndex < beatTimes.Count; beatIndex++)
        {
            double time = beatTimes[beatIndex];
            if (double.IsNaN(time) || double.IsInfinity(time) || time < 0.0)
            {
                throw new ArgumentException("Beat times must be finite and non-negative.", nameof(beatTimes));
            }

            if (frameCount == 0 || time > duration)
            {
                continue;
            }

            int startFrame = (int)Math.Floor(Math.Max(0.0, time - WindowRadiusSeconds) * sampleRate);
            int endFrame = (int)Math.Ceiling(Math.Min(duration, time + WindowRadiusSeconds) * sampleRate);
            endFrame = Math.Min(frameCount, endFrame);

            double sumSquares = 0.0;
            int startSample = startFrame * channels;
            int endSample = endFrame * channels;

            for (int sampleIndex = startSample; sampleIndex < endSample; sampleIndex++)
            {
                double sample = interleavedSamples[sampleIndex];
                if (double.IsNaN(sample) || double.IsInfinity(sample))
                {
                    throw new ArgumentException("Audio samples must be finite.", nameof(interleavedSamples));
                }

                // Square each channel before combining, so opposite stereo phases do not cancel.
                sumSquares += sample * sample;
            }

            int sampleCount = endSample - startSample;
            if (sampleCount > 0)
            {
                levels[beatIndex] = Math.Sqrt(sumSquares / sampleCount);
            }
        }

        return Normalize(levels);
    }


    private static float[] Normalize(double[] levels)
    {
        float[] intensities = new float[levels.Length];
        if (levels.Length == 0)
        {
            return intensities;
        }

        double[] sortedLevels = (double[])levels.Clone();
        Array.Sort(sortedLevels);

        double maximum = sortedLevels[sortedLevels.Length - 1];
        if (maximum <= SilenceThreshold)
        {
            return intensities;
        }

        // Use the 5th and 95th percentiles so isolated peaks do not flatten the whole song.
        double low = Percentile(sortedLevels, 0.05);
        double high = Percentile(sortedLevels, 0.95);

        // A flat passage must not turn numerical noise into strong/weak beat differences.
        // Falling back to the maximum also handles sparse sound surrounded by silence.
        if (high - low <= Math.Max(SilenceThreshold, high * 0.01))
        {
            low = 0.0;
            high = maximum;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] <= SilenceThreshold)
            {
                continue;
            }

            double normalized = (levels[i] - low) / (high - low);
            intensities[i] = (float)Math.Max(0.0, Math.Min(1.0, normalized));
        }

        return intensities;
    }


    private static double Percentile(double[] sortedValues, double percentile)
    {
        double position = (sortedValues.Length - 1) * percentile;
        int lowerIndex = (int)Math.Floor(position);
        int upperIndex = Math.Min(lowerIndex + 1, sortedValues.Length - 1);
        double fraction = position - lowerIndex;

        return sortedValues[lowerIndex] +
            (sortedValues[upperIndex] - sortedValues[lowerIndex]) * fraction;
    }
}
