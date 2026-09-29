using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class JuanAndresSpawnDirector :
    MonoBehaviour,
    IExperienceRuntime
{
    private sealed class ScheduledSpawn
    {
        public int BeatIndex;
        public double ExpectedTime;
        public double SpawnTime;

        public DifficultyLevel Difficulty;

        public float ResponseWindow;
        public int MaxActiveTargets;

        public bool WantsPair;
    }


    [Header("References")]
    [SerializeField] private JuanAndresTargetSpawner _targetSpawner;

    [Header("Beat Map")]
    [SerializeField] private BeatMapSO _beatMap;

    [Header("Timing")]
    [Min(0.05f)]
    [SerializeField] private float _reactionLeadTime = 0.5f;

    [Min(1)]
    [SerializeField] private int _lookAheadSpawns = 4;

    [Header("Difficulty")]
    [SerializeField] private JuanAndresDifficultyConfigSO _difficultyConfig;
    [SerializeField] private DifficultyLevelEventChannelSO _difficultyChanged;
    [SerializeField] private DifficultyLevel _manualDifficulty = DifficultyLevel.Normal;
    [SerializeField] private bool _useDynamicDifficulty;


    private readonly List<ScheduledSpawn> _scheduledSpawns =
        new List<ScheduledSpawn>();
    private readonly List<JuanAndresSpawnPoint> _availableSpawnPoints =
        new List<JuanAndresSpawnPoint>();
    private readonly HashSet<JuanAndresSpawnPoint> _lastSpawnPoints =
        new HashSet<JuanAndresSpawnPoint>();
    private ExperienceMusicClock _musicClock;
    private bool _isRunning;
    private int _nextBeatIndex;
    private int _nextPairId;
    private DifficultyLevel _currentDifficulty;
    private JuanAndresDifficultyProfile _currentProfile;


    public void BeginExperience()
    {
        if (_isRunning)
        {
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        _musicClock =
            FindFirstObjectByType<ExperienceMusicClock>();

        if (_musicClock == null)
        {
            Debug.LogError(
                "[JuanAndresSpawnDirector] No se encontró ExperienceMusicClock.",
                this
            );

            return;
        }

        if (_useDynamicDifficulty &&
            _difficultyChanged != null)
        {
            _difficultyChanged.Raised += SetDifficulty;

            _currentDifficulty =
                _difficultyChanged.CurrentDifficulty;
        }
        else
        {
            _currentDifficulty =
                _manualDifficulty;
        }

        _currentProfile =
            _difficultyConfig.GetProfile(
                _currentDifficulty
            );

        _scheduledSpawns.Clear();
        _lastSpawnPoints.Clear();

        _nextBeatIndex = 0;
        _nextPairId = 0;

        while (
            _nextBeatIndex < _beatMap.Beats.Count &&
            _beatMap.Beats[_nextBeatIndex].Time <
            _musicClock.SongTime)
        {
            _nextBeatIndex++;
        }

        _isRunning = true;

        FillSchedule();
    }


    public void EndExperience()
    {
        if (_useDynamicDifficulty && _difficultyChanged != null)
        {
            _difficultyChanged.Raised -= SetDifficulty;
        }

        _isRunning = false;

        _scheduledSpawns.Clear();
        _lastSpawnPoints.Clear();

        if (_targetSpawner != null)
        {
            _targetSpawner.ReleaseAllTargets();
        }
    }


    private void Update()
    {
        if (!_isRunning ||
            _musicClock == null ||
            !_musicClock.IsPlaying ||
            Time.timeScale <= 0 ||
            AudioListener.pause)
        {
            return;
        }

        ProcessScheduledSpawns();
        FillSchedule();
    }


    private void FillSchedule()
    {
        while (
            _scheduledSpawns.Count < _lookAheadSpawns &&
            _nextBeatIndex < _beatMap.Beats.Count)
        {
            int beatCount =
                _currentProfile.SpawnEveryNBeats;

            int beatIndex =
                GetMostIntenseBeatIndex(
                    _nextBeatIndex,
                    beatCount
                );

            _nextBeatIndex +=
                beatCount;

            ScheduleBeat(
                beatIndex
            );
        }
    }


    private void ScheduleBeat(int beatIndex)
    {
        double expectedTime =
            _beatMap.Beats[beatIndex].Time;

        double spawnTime =
            expectedTime -
            _reactionLeadTime;

        if (spawnTime < _musicClock.SongTime)
        {
            return;
        }

        ScheduledSpawn scheduledSpawn =
            new ScheduledSpawn
            {
                BeatIndex = beatIndex,
                ExpectedTime = expectedTime,
                SpawnTime = spawnTime,

                Difficulty =
                    _currentDifficulty,

                ResponseWindow =
                    _currentProfile.ResponseWindow,

                MaxActiveTargets =
                    _currentProfile.MaxActiveTargets,

                WantsPair =
                    Random.value <
                    _currentProfile.DualTargetProbability
            };

        _scheduledSpawns.Add(
            scheduledSpawn
        );
    }


    private void ProcessScheduledSpawns()
    {
        double songTime =
            _musicClock.SongTime;

        while (
            _scheduledSpawns.Count > 0 &&
            songTime >=
            _scheduledSpawns[0].SpawnTime)
        {
            ScheduledSpawn scheduledSpawn =
                _scheduledSpawns[0];

            _scheduledSpawns.RemoveAt(0);

            if (
                songTime -
                scheduledSpawn.SpawnTime >
                0.1)
            {
                continue;
            }

            SpawnScheduled(
                scheduledSpawn
            );
        }
    }


    private void SpawnScheduled(ScheduledSpawn scheduledSpawn)
    {
        int availableCount =
            _targetSpawner.GetAvailableSpawnPoints(
                _availableSpawnPoints
            );

        if (availableCount == 0)
        {
            return;
        }

        int occupiedCount =
            _targetSpawner.SpawnPointCount -
            availableCount;

        int remainingSlots =
            scheduledSpawn.MaxActiveTargets -
            occupiedCount;

        if (remainingSlots <= 0)
        {
            return;
        }

        int eligibleCount =
            GetEligibleSpawnPoints();

        if (eligibleCount == 0)
        {
            return;
        }

        bool canSpawnPair =
            scheduledSpawn.WantsPair &&
            eligibleCount >= 2 &&
            remainingSlots >= 2;

        if (canSpawnPair)
        {
            SpawnPair(
                scheduledSpawn
            );

            return;
        }

        SpawnSingle(
            scheduledSpawn
        );
    }


    private void SpawnSingle(ScheduledSpawn scheduledSpawn)
    {
        if (GetEligibleSpawnPoints() == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, _availableSpawnPoints.Count);

        JuanAndresSpawnPoint spawnPoint =
            _availableSpawnPoints[randomIndex];

        JuanAndresActionDirection direction =
            GetRandomDirection();

        JuanAndresToolType tool =
            GetRandomTool();

        double expireTime =
            scheduledSpawn.ExpectedTime +
            scheduledSpawn.ResponseWindow;

        JuanAndresTarget target =
            _targetSpawner.ReserveTarget(
                spawnPoint,
                direction,
                tool,
                scheduledSpawn.Difficulty,
                scheduledSpawn.SpawnTime,
                scheduledSpawn.ExpectedTime,
                expireTime,
                -1
            );

        if (target == null)
        {
            return;
        }

        if (_targetSpawner.ActivateTarget(target))
        {
            RememberLastSpawnPoint(spawnPoint);
        }
    }


    private void SpawnPair(ScheduledSpawn scheduledSpawn)
    {
        int eligibleCount =
            GetEligibleSpawnPoints();

        if (eligibleCount < 2)
        {
            SpawnSingle(scheduledSpawn);
            return;
        }

        int firstIndex = Random.Range(0, _availableSpawnPoints.Count);

        JuanAndresSpawnPoint firstPoint =
            _availableSpawnPoints[firstIndex];

        _availableSpawnPoints.RemoveAt(firstIndex);

        int secondIndex = Random.Range(0, _availableSpawnPoints.Count);

        JuanAndresSpawnPoint secondPoint =
            _availableSpawnPoints[secondIndex];

        JuanAndresToolType firstTool =
            Random.value < 0.5f
                ? JuanAndresToolType.Soap
                : JuanAndresToolType.Brush;

        JuanAndresToolType secondTool =
            firstTool == JuanAndresToolType.Soap
                ? JuanAndresToolType.Brush
                : JuanAndresToolType.Soap;

        int pairId =
            _nextPairId++;

        double expireTime =
            scheduledSpawn.ExpectedTime +
            scheduledSpawn.ResponseWindow;

        JuanAndresTarget firstTarget =
            _targetSpawner.ReserveTarget(
                firstPoint,
                GetRandomDirection(),
                firstTool,
                scheduledSpawn.Difficulty,
                scheduledSpawn.SpawnTime,
                scheduledSpawn.ExpectedTime,
                expireTime,
                pairId
            );

        JuanAndresTarget secondTarget =
            _targetSpawner.ReserveTarget(
                secondPoint,
                GetRandomDirection(),
                secondTool,
                scheduledSpawn.Difficulty,
                scheduledSpawn.SpawnTime,
                scheduledSpawn.ExpectedTime,
                expireTime,
                pairId
            );

        if (firstTarget == null ||
            secondTarget == null)
        {
            if (firstTarget != null)
            {
                _targetSpawner.CancelReservedTarget(firstTarget);
            }

            if (secondTarget != null)
            {
                _targetSpawner.CancelReservedTarget(secondTarget);
            }

            SpawnSingle(scheduledSpawn);
            return;
        }

        bool firstActivated =
            _targetSpawner.ActivateTarget(firstTarget);

        bool secondActivated =
            _targetSpawner.ActivateTarget(secondTarget);

        if (firstActivated &&
            secondActivated)
        {
            RememberLastSpawnPoints(firstPoint, secondPoint);
        }
    }


    private JuanAndresActionDirection GetRandomDirection()
    {
        return Random.value < 0.5f
            ? JuanAndresActionDirection.Clockwise
            : JuanAndresActionDirection.CounterClockwise;
    }


    private JuanAndresToolType GetRandomTool()
    {
        return Random.value < 0.5f
            ? JuanAndresToolType.Soap
            : JuanAndresToolType.Brush;
    }


    private int GetEligibleSpawnPoints()
    {
        _targetSpawner.GetAvailableSpawnPoints(
            _availableSpawnPoints
        );

        for (
            int i = _availableSpawnPoints.Count - 1;
            i >= 0;
            i--)
        {
            if (_lastSpawnPoints.Contains(
                _availableSpawnPoints[i]))
            {
                _availableSpawnPoints.RemoveAt(
                    i
                );
            }
        }

        return _availableSpawnPoints.Count;
    }


    private void RememberLastSpawnPoint(JuanAndresSpawnPoint spawnPoint)
    {
        _lastSpawnPoints.Clear();

        if (spawnPoint != null)
        {
            _lastSpawnPoints.Add(
                spawnPoint
            );
        }
    }


    private void RememberLastSpawnPoints(JuanAndresSpawnPoint firstPoint, JuanAndresSpawnPoint secondPoint)
    {
        _lastSpawnPoints.Clear();

        if (firstPoint != null)
        {
            _lastSpawnPoints.Add(
                firstPoint
            );
        }

        if (secondPoint != null)
        {
            _lastSpawnPoints.Add(
                secondPoint
            );
        }
    }


    private int GetMostIntenseBeatIndex(int startIndex, int beatCount)
    {
        int endIndex =
            Mathf.Min(
                startIndex + beatCount,
                _beatMap.Beats.Count
            );

        int selectedIndex =
            startIndex;

        float highestIntensity =
            _beatMap.Beats[startIndex].Intensity;

        for (
            int i = startIndex + 1;
            i < endIndex;
            i++)
        {
            float intensity =
                _beatMap.Beats[i].Intensity;

            if (intensity <= highestIntensity)
            {
                continue;
            }

            highestIntensity =
                intensity;

            selectedIndex =
                i;
        }

        return selectedIndex;
    }


    private void SetDifficulty(DifficultyLevel difficulty)
    {
        if (!_useDynamicDifficulty ||
            _currentDifficulty == difficulty)
        {
            return;
        }

        _currentDifficulty =
            difficulty;

        _currentProfile =
            _difficultyConfig.GetProfile(
                difficulty
            );

        Debug.Log(
            $"[JuanAndresSpawnDirector] Difficulty changed to {difficulty}.",
            this
        );
    }


    private bool ValidateReferences()
    {
        if (_targetSpawner == null)
        {
            Debug.LogError(
                "[JuanAndresSpawnDirector] TargetSpawner no está asignado.",
                this
            );

            return false;
        }

        if (
            _beatMap == null ||
            _beatMap.Beats.Count == 0)
        {
            Debug.LogError(
                "[JuanAndresSpawnDirector] BeatMap no es válido.",
                this
            );

            return false;
        }

        if (_difficultyConfig == null)
        {
            Debug.LogError(
                "[JuanAndresSpawnDirector] DifficultyConfig no está asignado.",
                this
            );

            return false;
        }

        return true;
    }
}