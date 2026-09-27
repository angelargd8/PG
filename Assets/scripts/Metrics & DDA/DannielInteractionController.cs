using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DannielInteractionController :
    MonoBehaviour,
    IExperienceRuntime
{
    private sealed class PendingShot
    {
        public double ExpectedTime;
        public double ActualTime;
        public DifficultyLevel Difficulty;
    }


    [Header("Gameplay")]
    [SerializeField] private GunShooter[] _guns;
    [SerializeField] private SegmentPool _segmentPool;

    [Header("Music")]
    [SerializeField] private BeatMapSO _beatMap;

    [Header("Metrics")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;
    [SerializeField] private DifficultyLevelEventChannelSO _difficultyChanged;

    [Header("Score")]
    [SerializeField] private ScoreProfileSO _scoreProfile;
    [SerializeField] private ScoreProfileEventChannelSO _scoreProfileChanged;


    private readonly Dictionary<PooledBullet, PendingShot> _pendingShots =
        new Dictionary<PooledBullet, PendingShot>();

    private ExperienceMusicClock _musicClock;

    private DifficultyLevel _currentDifficulty =
        DifficultyLevel.Normal;

    private bool _isRunning;


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
                "[DannielInteractionController] " +
                "No se encontró ExperienceMusicClock.",
                this
            );

            return;
        }

        _currentDifficulty =
            _difficultyChanged != null
                ? _difficultyChanged.CurrentDifficulty
                : DifficultyLevel.Normal;

        if (_difficultyChanged != null)
        {
            _difficultyChanged.Raised +=
                HandleDifficultyChanged;
        }

        foreach (GunShooter gun in _guns)
        {
            gun.ShotFired += HandleShotFired;
        }

        _segmentPool.EnemiesMissed +=
            HandleEnemiesMissed;

        _scoreProfileChanged.RaiseEvent(
            _scoreProfile
        );

        _isRunning = true;
    }


    public void EndExperience()
    {
        if (_difficultyChanged != null)
        {
            _difficultyChanged.Raised -=
                HandleDifficultyChanged;
        }

        if (_guns != null)
        {
            foreach (GunShooter gun in _guns)
            {
                if (gun != null)
                {
                    gun.ShotFired -=
                        HandleShotFired;
                }
            }
        }

        if (_segmentPool != null)
        {
            _segmentPool.EnemiesMissed -=
                HandleEnemiesMissed;
        }

        foreach (PooledBullet bullet in _pendingShots.Keys)
        {
            if (bullet == null)
            {
                continue;
            }

            bullet.TriggerEntered -=
                HandleBulletTriggerEntered;

            bullet.Despawned -=
                HandleBulletDespawned;
        }

        _pendingShots.Clear();

        _isRunning = false;
    }


    private void OnDisable()
    {
        EndExperience();
    }


    private void HandleShotFired(PooledBullet bullet)
    {
        if (!_isRunning ||
            bullet == null ||
            _musicClock == null)
        {
            return;
        }

        double actualTime =
            _musicClock.SongTime;

        double expectedTime =
            GetNearestBeatTime(actualTime);

        if (_pendingShots.ContainsKey(bullet))
        {
            UntrackShot(bullet);
        }

        PendingShot shot =
            new PendingShot
            {
                ExpectedTime = expectedTime,
                ActualTime = actualTime,
                Difficulty = _currentDifficulty
            };

        _pendingShots[bullet] = shot;

        bullet.TriggerEntered +=
            HandleBulletTriggerEntered;

        bullet.Despawned +=
            HandleBulletDespawned;
    }


    private void HandleBulletTriggerEntered(
        PooledBullet bullet,
        Collider other
    )
    {
        if (!_pendingShots.ContainsKey(bullet))
        {
            return;
        }

        EnemyController enemy =
            other.GetComponentInParent<EnemyController>();

        if (enemy != null)
        {
            Vector3 feedbackPosition =
                other.ClosestPoint(
                    bullet.transform.position
                );

            ResolveShot(
                bullet,
                InteractionOutcome.Success,
                feedbackPosition
            );

            return;
        }

        ResolveShot(
            bullet,
            InteractionOutcome.Failed,
            null
        );
    }


    private void HandleBulletDespawned(PooledBullet bullet)
    {
        if (!_pendingShots.ContainsKey(bullet))
        {
            return;
        }

        ResolveShot(
            bullet,
            InteractionOutcome.Failed,
            null
        );
    }


    private void ResolveShot(
        PooledBullet bullet,
        InteractionOutcome outcome,
        Vector3? feedbackPosition
    )
    {
        if (!_pendingShots.TryGetValue(
            bullet,
            out PendingShot shot
        ))
        {
            return;
        }

        UntrackShot(bullet);

        InteractionResult result =
            new InteractionResult(
                minigameId: "Danniel",
                interactionType: InteractionType.GunShoot,
                outcome: outcome,
                difficulty: shot.Difficulty,
                expectedTime: shot.ExpectedTime,
                actualTime: shot.ActualTime,
                feedbackPosition: feedbackPosition
            );

        _interactionRegistered.RaiseEvent(
            result
        );
    }


    private void HandleEnemiesMissed(
        int count,
        DifficultyLevel difficulty
    )
    {
        if (!_isRunning ||
            count <= 0 ||
            _musicClock == null)
        {
            return;
        }

        double missTime =
            _musicClock.SongTime;

        for (int i = 0; i < count; i++)
        {
            InteractionResult result =
                new InteractionResult(
                    minigameId: "Danniel",
                    interactionType: InteractionType.GunShoot,
                    outcome: InteractionOutcome.Missed,
                    difficulty: difficulty,
                    expectedTime: missTime
                );

            _interactionRegistered.RaiseEvent(
                result
            );
        }
    }


    private void HandleDifficultyChanged(DifficultyLevel difficulty)
    {
        _currentDifficulty = difficulty;
    }


    private void UntrackShot(PooledBullet bullet)
    {
        if (bullet == null)
        {
            return;
        }

        bullet.TriggerEntered -=
            HandleBulletTriggerEntered;

        bullet.Despawned -=
            HandleBulletDespawned;

        _pendingShots.Remove(bullet);
    }


    private double GetNearestBeatTime(double songTime)
    {
        IReadOnlyList<BeatMapSO.Beat> beats =
            _beatMap.Beats;

        int low = 0;
        int high = beats.Count;

        while (low < high)
        {
            int middle =
                low + (high - low) / 2;

            if (beats[middle].Time < songTime)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        if (low <= 0)
        {
            return beats[0].Time;
        }

        if (low >= beats.Count)
        {
            return beats[beats.Count - 1].Time;
        }

        double previousTime =
            beats[low - 1].Time;

        double nextTime =
            beats[low].Time;

        double previousDifference =
            Math.Abs(songTime - previousTime);

        double nextDifference =
            Math.Abs(nextTime - songTime);

        return previousDifference <= nextDifference
            ? previousTime
            : nextTime;
    }


    private bool ValidateReferences()
    {
        if (_guns == null ||
            _guns.Length == 0)
        {
            Debug.LogError(
                "[DannielInteractionController] No hay armas asignadas.",
                this
            );

            return false;
        }

        foreach (GunShooter gun in _guns)
        {
            if (gun == null)
            {
                Debug.LogError(
                    "[DannielInteractionController] Hay un arma null.",
                    this
                );

                return false;
            }
        }

        if (_segmentPool == null)
        {
            Debug.LogError(
                "[DannielInteractionController] " +
                "SegmentPool no está asignado.",
                this
            );

            return false;
        }

        if (_beatMap == null ||
            _beatMap.Beats.Count == 0)
        {
            Debug.LogError(
                "[DannielInteractionController] BeatMap inválido.",
                this
            );

            return false;
        }

        if (_interactionRegistered == null)
        {
            Debug.LogError(
                "[DannielInteractionController] " +
                "InteractionRegistered no está asignado.",
                this
            );

            return false;
        }

        if (_scoreProfile == null ||
            _scoreProfileChanged == null)
        {
            Debug.LogError(
                "[DannielInteractionController] " +
                "Falta configuración de Score.",
                this
            );

            return false;
        }

        return true;
    }
}