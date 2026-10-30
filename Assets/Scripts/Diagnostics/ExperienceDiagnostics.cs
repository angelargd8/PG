using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public enum ExperienceDiagnosticLevel
{
    Off = 0,
    Flow = 1,
    Detailed = 2
}

/// <summary>Opt-in, structured diagnostics for reconstructing an experience run from Logcat.</summary>
public static class ExperienceDiagnostics
{
    private const string ConfigFileName = "experience-diagnostics.txt";
    private static readonly long ProcessStartTicks = Stopwatch.GetTimestamp();
    private static int recordSequence;
    private static long eventSequence;
    private static long operationSequence;
    private static ExperienceDiagnosticLevel level;
    private static bool initialized;
    private static string currentRunId;
    private static string currentExperience;
    private static string currentMode;
    private static int currentSegmentIndex = -1;
    private static ExperienceMusicClock musicClock;
    private static readonly Dictionary<string, FrequentCounter> frequentCounters = new Dictionary<string, FrequentCounter>();
    [ThreadStatic] private static string currentEventId;
    private static readonly ExperienceDiagnosticOperation DisabledOperation =
        new ExperienceDiagnosticOperation(null, null, null, null, -1, null, null, 0.0) { IsFinished = true };

    public static ExperienceDiagnosticLevel Level
    {
        get { EnsureInitialized(); return level; }
    }

    public static string CurrentRunId => currentRunId;
    public static string CurrentEventId => currentEventId;
    public static int CurrentSegmentIndex => currentSegmentIndex;
    public static string Field(string key, string value) => key + "=" + Escape(value);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeAtStartup()
    {
        EnsureInitialized();
        WriteHeader("startup", null, null);
    }

    private static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        level = ExperienceDiagnosticLevel.Off;
        try
        {
            string path = ConfigPath;
            if (!File.Exists(path)) return;
            string value = File.ReadAllText(path, Encoding.UTF8).Trim();
            if (Enum.TryParse(value, true, out ExperienceDiagnosticLevel parsed) &&
                parsed >= ExperienceDiagnosticLevel.Off && parsed <= ExperienceDiagnosticLevel.Detailed)
                level = parsed;
        }
        catch
        {
            // Configuration errors must not affect application startup.
        }
    }

    private static string ConfigPath => Path.Combine(Application.persistentDataPath, ConfigFileName);

    /// <summary>Sets and persists the diagnostic level. This works in Release builds too.</summary>
    public static void SetLevel(ExperienceDiagnosticLevel value)
    {
        EnsureInitialized();
        if (value < ExperienceDiagnosticLevel.Off || value > ExperienceDiagnosticLevel.Detailed)
            value = ExperienceDiagnosticLevel.Off;
        level = value;
        if (value == ExperienceDiagnosticLevel.Off)
            EndRun("diagnostics_disabled");
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(ConfigPath, value.ToString().ToLowerInvariant(), new UTF8Encoding(false));
        }
        catch
        {
            // Logging configuration must never interrupt the experience.
        }
        Log(ExperienceDiagnosticLevel.Flow, "CONFIG", "level_changed", null, null,
            "level=" + value.ToString().ToLowerInvariant());
        WriteHeader("level_changed", currentExperience, currentMode);
    }

    public static string BeginRun(string experience, bool fullSequence)
    {
        if (!IsEnabled(ExperienceDiagnosticLevel.Flow)) return null;
        frequentCounters.Clear();
        currentRunId = Guid.NewGuid().ToString("N");
        currentExperience = experience ?? "unknown";
        currentMode = fullSequence ? "full" : "individual";
        currentSegmentIndex = -1;
        WriteHeader("run", currentExperience, currentMode);
        Log(ExperienceDiagnosticLevel.Flow, "RUN", "started", null, null,
            "run_id=" + currentRunId + " experience=" + Escape(currentExperience) + " mode=" + currentMode);
        return currentRunId;
    }

    public static void EndRun(string outcome)
    {
        if (string.IsNullOrEmpty(currentRunId)) return;
        FlushFrequentCounters("run_end", null);
        Log(ExperienceDiagnosticLevel.Flow, "RUN", "ended", null, null,
            "outcome=" + (outcome ?? "unknown"));
        currentRunId = null;
        currentExperience = null;
        currentMode = null;
        currentSegmentIndex = -1;
    }

    public static string NewOperationId() => "op-" + System.Threading.Interlocked.Increment(ref operationSequence).ToString(CultureInfo.InvariantCulture);

    public static string NewEventId() => "evt-" + System.Threading.Interlocked.Increment(ref eventSequence).ToString(CultureInfo.InvariantCulture);

    public static double MonotonicSeconds => (Stopwatch.GetTimestamp() - ProcessStartTicks) / (double)Stopwatch.Frequency;

    public static ExperienceDiagnosticOperation BeginOperation(string name, UnityEngine.Object component, string details = null)
    {
        if (!IsEnabled(ExperienceDiagnosticLevel.Flow)) return DisabledOperation;
        var operation = new ExperienceDiagnosticOperation(NewOperationId(), currentRunId, currentExperience,
            currentMode, currentSegmentIndex, name, component, MonotonicSeconds);
        Log(ExperienceDiagnosticLevel.Flow, "OP", name, component, operation.OperationId,
            "phase=start" + (string.IsNullOrEmpty(details) ? string.Empty : " " + details), operation.RunId,
            experienceOverride: operation.Experience, modeOverride: operation.Mode, segmentIndexOverride: operation.SegmentIndex);
        return operation;
    }

    public static void EndOperation(ExperienceDiagnosticOperation operation, string result, string details = null)
    {
        if (operation == null || operation == DisabledOperation || operation.IsFinished) return;
        operation.IsFinished = true;
        double duration = Math.Max(0.0, MonotonicSeconds - operation.StartedAt);
        Log(ExperienceDiagnosticLevel.Flow, "OP", operation.Name, operation.Component,
            operation.OperationId, "phase=finish result=" + Escape(result) +
            " duration_s=" + duration.ToString("F6", CultureInfo.InvariantCulture) +
            (string.IsNullOrEmpty(details) ? string.Empty : " " + details), operation.RunId,
            experienceOverride: operation.Experience, modeOverride: operation.Mode, segmentIndexOverride: operation.SegmentIndex);
    }

    public static void RegisterMusicClock(ExperienceMusicClock clock) => musicClock = clock;
    public static void SetSegmentIndex(int index) => currentSegmentIndex = index;

    public static void RecordFrequentEvent(string eventName, string outcome, UnityEngine.Object component)
        => RecordFrequentEvent(eventName, outcome, 1, component);

    public static void RecordFrequentEvent(string eventName, string outcome, int amount, UnityEngine.Object component)
    {
        if (Level != ExperienceDiagnosticLevel.Flow) return;
        int segmentIndex = currentSegmentIndex;
        string key = eventName + ":" + outcome + ":" + segmentIndex.ToString(CultureInfo.InvariantCulture);
        if (!frequentCounters.TryGetValue(key, out FrequentCounter counter))
        {
            counter = new FrequentCounter(eventName, outcome, segmentIndex);
            frequentCounters.Add(key, counter);
        }
        counter.Total += amount;
        counter.Pending += amount;
        if (counter.Pending >= 32) FlushFrequentCounter(key, counter, "interval", component);
    }

    public static void FlushFrequentCounters(string reason, UnityEngine.Object component)
    {
        if (!IsEnabled(ExperienceDiagnosticLevel.Flow)) return;
        foreach (var pair in frequentCounters)
            if (pair.Value.Pending > 0) FlushFrequentCounter(pair.Key, pair.Value, reason, component);
    }

    private static void FlushFrequentCounter(string key, FrequentCounter counter, string reason, UnityEngine.Object component)
    {
        int pending = counter.Pending;
        counter.Pending = 0;
        Log(ExperienceDiagnosticLevel.Flow, "SUMMARY", "frequent_event_summary", component, null,
            "event=" + Escape(counter.EventName) + " outcome=" + Escape(counter.Outcome) +
            " interval_count=" + pending + " total_count=" + counter.Total +
            " flush_reason=" + Escape(reason), segmentIndexOverride: counter.SegmentIndex);
    }

    private sealed class FrequentCounter
    {
        public FrequentCounter(string eventName, string outcome, int segmentIndex)
        { EventName = eventName; Outcome = outcome; SegmentIndex = segmentIndex; }
        public string EventName { get; }
        public string Outcome { get; }
        public int SegmentIndex { get; }
        public int Total;
        public int Pending;
    }
    public static void UnregisterMusicClock(ExperienceMusicClock clock)
    {
        if (musicClock == clock) musicClock = null;
    }

    public static bool IsEnabled(ExperienceDiagnosticLevel required)
    {
        EnsureInitialized();
        return level >= required && required != ExperienceDiagnosticLevel.Off;
    }

    public static void Log(ExperienceDiagnosticLevel required, string category, string action,
        UnityEngine.Object component, string operationId, string details = null,
        string runIdOverride = null, string eventId = null, string experienceOverride = null,
        string modeOverride = null, int? segmentIndexOverride = null)
    {
        if (!IsEnabled(required)) return;
        string runId = runIdOverride ?? currentRunId;
        string componentName = component != null ? component.GetType().Name : "none";
        string instanceId = component != null ? component.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "none";
        string scene = component is Component unityComponent ? unityComponent.gameObject.scene.name :
            component is GameObject gameObject ? gameObject.scene.name : "none";
        string musicalTime = musicClock != null ? musicClock.SongTime.ToString("F6", CultureInfo.InvariantCulture) : "unavailable";
        var line = new StringBuilder(256);
        line.Append("[PGDIAG][").Append(category).Append("] record_seq=")
            .Append(System.Threading.Interlocked.Increment(ref recordSequence).ToString(CultureInfo.InvariantCulture))
            .Append(" mono_s=").Append(MonotonicSeconds.ToString("F6", CultureInfo.InvariantCulture))
            .Append(" run_id=").Append(Escape(runId ?? "none"))
            .Append(" event_id=").Append(Escape(eventId ?? currentEventId ?? "none"))
            .Append(" operation_id=").Append(Escape(operationId ?? "none"))
            .Append(" action=").Append(Escape(action ?? "none"))
            .Append(" component=").Append(Escape(componentName))
            .Append(" instance_id=").Append(instanceId)
            .Append(" scene=").Append(Escape(scene))
            .Append(" experience=").Append(Escape(experienceOverride ?? currentExperience ?? "none"))
            .Append(" mode=").Append(Escape(modeOverride ?? currentMode ?? "none"))
            .Append(" segment_index=").Append((segmentIndexOverride ?? currentSegmentIndex).ToString(CultureInfo.InvariantCulture))
            .Append(" musical_time_s=").Append(musicalTime);
        if (!string.IsNullOrEmpty(details)) line.Append(' ').Append(details);
        UnityEngine.Debug.Log(line.ToString(), component);
    }

    public static void LogHeader(string reason = "manual") => WriteHeader(reason, currentExperience, currentMode);

    private static void WriteHeader(string reason, string experience, string mode)
    {
        if (!IsEnabled(ExperienceDiagnosticLevel.Flow)) return;
        string line = "[PGDIAG][HEADER] record_seq=" +
            System.Threading.Interlocked.Increment(ref recordSequence).ToString(CultureInfo.InvariantCulture) +
            " mono_s=" + MonotonicSeconds.ToString("F6", CultureInfo.InvariantCulture) +
            " reason=" + Escape(reason) +
            " app_version=" + Escape(Application.version) +
            " app_id=" + Escape(Application.identifier) +
            " unity_version=" + Escape(Application.unityVersion) +
            " build_type=" + (UnityEngine.Debug.isDebugBuild ? "development" : "release") +
            " platform=" + Escape(Application.platform.ToString()) +
            " experience=" + Escape(experience ?? "pending") +
            " mode=" + Escape(mode ?? "pending") +
            " log_level=" + level.ToString().ToLowerInvariant() +
            " config_path=" + Escape(ConfigPath);
        UnityEngine.Debug.Log(line);
    }

    public static void LogSubscription(string action, UnityEngine.Object subscriber, UnityEngine.Object channel)
    {
        if (!IsEnabled(ExperienceDiagnosticLevel.Detailed)) return;
        Log(ExperienceDiagnosticLevel.Detailed, "EDA", "subscription_" + action, subscriber, null,
            "channel=" + Escape(channel != null ? channel.name : "none") +
            " channel_id=" + (channel != null ? channel.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "none"));
    }

    public static void PublishVoid(UnityEngine.Object channel, Delegate handlers, UnityEngine.Object publisher, string payload = null)
    {
        string eventId = PrepareEvent(channel, publisher, payload, handlers, out string details);
        using (PushEvent(eventId))
        {
            if (IsEnabled(ExperienceDiagnosticLevel.Detailed) && handlers != null)
            {
                foreach (Delegate receiver in handlers.GetInvocationList())
                {
                    LogReceipt(eventId, channel, receiver, payload);
                    InvokeReceiver(eventId, channel, receiver, payload, () => ((Action)receiver)());
                }
            }
            else ((Action)handlers)?.Invoke();
        }
        LogEventCompletion(channel, publisher, eventId, details, handlers);
    }

    public static void Publish<T>(UnityEngine.Object channel, Delegate handlers, T value,
        UnityEngine.Object publisher, string payload = null)
    {
        if (Level == ExperienceDiagnosticLevel.Flow && IsFrequentChannel(channel))
        {
            RecordFrequentEvent(channel.GetType().Name, "published", publisher);
            ((Action<T>)handlers)?.Invoke(value);
            return;
        }
        string eventId = PrepareEvent(channel, publisher, payload, handlers, out string details);
        using (PushEvent(eventId))
        {
            if (IsEnabled(ExperienceDiagnosticLevel.Detailed) && handlers != null)
            {
                foreach (Delegate receiver in handlers.GetInvocationList())
                {
                    LogReceipt(eventId, channel, receiver, payload);
                    InvokeReceiver(eventId, channel, receiver, payload, () => ((Action<T>)receiver)(value));
                }
            }
            else ((Action<T>)handlers)?.Invoke(value);
        }
        LogEventCompletion(channel, publisher, eventId, details, handlers);
    }

    public static bool IsFrequentChannel(UnityEngine.Object channel)
    {
        if (channel == null) return false;
        string type = channel.GetType().Name;
        return type == "InteractionResultEventChannelSO" || type == "ScoreChangedEventChannelSO";
    }

    public static void Publish<T1, T2>(UnityEngine.Object channel, Delegate handlers, T1 first, T2 second,
        UnityEngine.Object publisher, string payload = null)
    {
        string eventId = PrepareEvent(channel, publisher, payload, handlers, out string details);
        using (PushEvent(eventId))
        {
            if (IsEnabled(ExperienceDiagnosticLevel.Detailed) && handlers != null)
            {
                foreach (Delegate receiver in handlers.GetInvocationList())
                {
                    LogReceipt(eventId, channel, receiver, payload);
                    InvokeReceiver(eventId, channel, receiver, payload, () => ((Action<T1, T2>)receiver)(first, second));
                }
            }
            else ((Action<T1, T2>)handlers)?.Invoke(first, second);
        }
        LogEventCompletion(channel, publisher, eventId, details, handlers);
    }

    public static void PublishBeat(ExperienceBeatPlayer player, BeatMapSO.Beat beat, int index,
        Delegate handlers, string payload)
    {
        if (Level == ExperienceDiagnosticLevel.Flow)
        {
            RecordFrequentEvent("BeatReached", "published", player);
            ((Action<BeatMapSO.Beat, int>)handlers)?.Invoke(beat, index);
            return;
        }
        string eventId = PrepareEvent(player, player, payload, handlers, out string details, "BeatReached");
        using (PushEvent(eventId))
        {
            if (IsEnabled(ExperienceDiagnosticLevel.Detailed) && handlers != null)
            {
                foreach (Delegate receiver in handlers.GetInvocationList())
                {
                    LogReceipt(eventId, player, receiver, payload, "BeatReached");
                    InvokeReceiver(eventId, player, receiver, payload, () => ((Action<BeatMapSO.Beat, int>)receiver)(beat, index), "BeatReached");
                }
            }
            else ((Action<BeatMapSO.Beat, int>)handlers)?.Invoke(beat, index);
        }
        LogEventCompletion(player, player, eventId, details, handlers, "BeatReached");
    }

    public static void PublishPlaybackReset(ExperienceBeatPlayer player, Delegate handlers, string condition, string times)
    {
        string payload = "condition=" + Escape(condition) + " " + times;
        string eventId = PrepareEvent(player, player, payload, handlers, out string details, "PlaybackReset");
        using (PushEvent(eventId))
        {
            if (IsEnabled(ExperienceDiagnosticLevel.Detailed) && handlers != null)
            {
                foreach (Delegate receiver in handlers.GetInvocationList())
                {
                    LogReceipt(eventId, player, receiver, payload, "PlaybackReset");
                    InvokeReceiver(eventId, player, receiver, payload, () => ((Action)receiver)(), "PlaybackReset");
                }
            }
            else ((Action)handlers)?.Invoke();
        }
        LogEventCompletion(player, player, eventId, details, handlers, "PlaybackReset");
    }

    private static string PrepareEvent(UnityEngine.Object channel, UnityEngine.Object publisher, string payload,
        Delegate handlers, out string details, string eventName = null)
    {
        string eventId = NewEventId();
        int receiverCount = handlers?.GetInvocationList().Length ?? 0;
        string channelName = channel != null ? channel.name : "none";
        string channelId = channel != null ? channel.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "none";
        string name = eventName ?? channelName;
        details = "event=" + Escape(name) + " phase=publish channel=" + Escape(channelName) +
            " channel_type=" + Escape(channel != null ? channel.GetType().Name : "none") +
            " channel_id=" + channelId + " receiver_count=" + receiverCount +
            (string.IsNullOrEmpty(payload) ? string.Empty : " data=" + Escape(payload));
        Log(ExperienceDiagnosticLevel.Flow, "EDA", "publish", publisher, null, details, eventId: eventId);
        return eventId;
    }

    private static void LogReceipt(string eventId, UnityEngine.Object channel, Delegate receiver, string payload, string eventName = null)
    {
        UnityEngine.Object target = receiver.Target as UnityEngine.Object;
        string receiverName = target != null ? target.GetType().Name :
            receiver.Target != null ? receiver.Target.GetType().Name : "static";
        string channelName = channel != null ? channel.name : "none";
        string channelId = channel != null ? channel.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "none";
        string details = "event=" + Escape(eventName ?? channelName) + " phase=receive channel=" + Escape(channelName) +
            " channel_type=" + Escape(channel != null ? channel.GetType().Name : "none") +
            " channel_id=" + channelId + " receiver=" + Escape(receiverName) +
            " receiver_method=" + Escape(receiver.Method.Name) +
            " receiver_instance_id=" + (target != null ? target.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "none") +
            (string.IsNullOrEmpty(payload) ? string.Empty : " data=" + Escape(payload));
        Log(ExperienceDiagnosticLevel.Detailed, "EDA", "receive", target, null, details, eventId: eventId);
    }

    private static void LogEventCompletion(UnityEngine.Object channel, UnityEngine.Object publisher,
        string eventId, string details, Delegate handlers, string eventName = null)
    {
        Log(ExperienceDiagnosticLevel.Detailed, "EDA", "handler_dispatch_returned", publisher, null,
            "event=" + Escape(eventName ?? (channel != null ? channel.name : "none")) +
            " receiver_count=" + (handlers?.GetInvocationList().Length ?? 0), eventId: eventId);
    }

    private static void InvokeReceiver(string eventId, UnityEngine.Object channel, Delegate receiver,
        string payload, Action invoke, string eventName = null)
    {
        try
        {
            invoke();
            if (Level == ExperienceDiagnosticLevel.Detailed)
            {
                UnityEngine.Object target = receiver.Target as UnityEngine.Object;
                Log(ExperienceDiagnosticLevel.Detailed, "EDA", "handler_returned", target, null,
                    "event=" + Escape(eventName ?? (channel != null ? channel.name : "none")) +
                    " receiver_method=" + Escape(receiver.Method.Name) +
                    (string.IsNullOrEmpty(payload) ? string.Empty : " data=" + Escape(payload)), eventId: eventId);
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Object target = receiver.Target as UnityEngine.Object;
            Log(ExperienceDiagnosticLevel.Flow, "EDA", "receiver_exception", target, null,
                "event=" + Escape(eventName ?? (channel != null ? channel.name : "none")) +
                " receiver_method=" + Escape(receiver.Method.Name) +
                " exception_type=" + Escape(exception.GetType().Name) +
                " exception_message=" + Escape(exception.Message), eventId: eventId);
            throw;
        }
    }

    private static IDisposable PushEvent(string eventId)
    {
        string previous = currentEventId;
        currentEventId = eventId;
        return new EventScope(previous);
    }

    private sealed class EventScope : IDisposable
    {
        private readonly string previous;
        private bool disposed;
        public EventScope(string previousEventId) => previous = previousEventId;
        public void Dispose()
        {
            if (disposed) return;
            currentEventId = previous;
            disposed = true;
        }
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "none";
        return Uri.EscapeDataString(value.Replace("\r", " ").Replace("\n", " "));
    }
}

public sealed class ExperienceDiagnosticOperation
{
    internal ExperienceDiagnosticOperation(string operationId, string runId, string experience,
        string mode, int segmentIndex, string name, UnityEngine.Object component, double startedAt)
    {
        OperationId = operationId;
        RunId = runId;
        Experience = experience;
        Mode = mode;
        SegmentIndex = segmentIndex;
        Name = name;
        Component = component;
        StartedAt = startedAt;
    }

    public string OperationId { get; }
    public string RunId { get; }
    public string Experience { get; }
    public string Mode { get; }
    public int SegmentIndex { get; }
    public string Name { get; }
    public UnityEngine.Object Component { get; }
    public double StartedAt { get; }
    public bool IsFinished { get; internal set; }
}
