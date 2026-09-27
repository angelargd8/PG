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


    private const int MissedShotsPerFailure = 5;


    [Header("Gameplay")]
    [SerializeField] private GunShooter[] _guns;
    [SerializeField] private SegmentPool _segmentPool;
    [SerializeField] private DannielPlayerHitbox _playerHitbox;

    [Header("Music")]
    [SerializeField] private BeatMapSO _beatMap;

    [Header("Metrics")]
    [SerializeField] private InteractionResultEventChannelSO _interactionRegistered;
    [SerializeField] private DifficultyLevelEventChannelSO _difficultyChanged;

    [Header("Score")]
    [SerializeField] private ScoreProfileSO _scoreProfile;
    [SerializeField] private ScoreProfileEventChannelSO _scoreProfileChanged;
    [SerializeField] private ScoreBonusEventChannelSO _scoreBonusAwarded;


    private readonly Dictionary<PooledBullet, PendingShot> _pendingShots = new Dictionary<PooledBullet, PendingShot>();
    private ExperienceMusicClock _musicClock;
    private DifficultyLevel _currentDifficulty = DifficultyLevel.Normal;
    private int _missedShotCount;
    private bool _isRunning;
    private int _enemyProjectileLayer;

    private void Awake()
    {
        _enemyProjectileLayer = LayerMask.NameToLayer("EnemyProjectile");
    }

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

        _missedShotCount = 0;

        if (_difficultyChanged != null)
        {
            _difficultyChanged.Raised +=
                HandleDifficultyChanged;
        }

        foreach (GunShooter gun in _guns)
        {
            gun.ShotFired += HandleShotFired;
        }

        _segmentPool.EnemiesMissed += HandleEnemiesMissed;

        _playerHitbox.HitByEnemyProjectile += HandlePlayerHit;

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
            _segmentPool.EnemiesMissed -= HandleEnemiesMissed;
        }

        if (_playerHitbox != null)
        {
            _playerHitbox.HitByEnemyProjectile -= HandlePlayerHit;
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

        _missedShotCount = 0;

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


    private void HandleBulletTriggerEntered(PooledBullet bullet, Collider other)
    {
        if (!_pendingShots.ContainsKey(bullet))
        {
            return;
        }

        if (other.gameObject.layer == _enemyProjectileLayer)
        {
            RegisterBonus(bullet, other);
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

            ResolveSuccessfulShot(
                bullet,
                feedbackPosition
            );

            return;
        }

        RegisterMissedShot(bullet);
    }


    private void HandleBulletDespawned(PooledBullet bullet)
    {
        if (!_pendingShots.ContainsKey(bullet))
        {
            return;
        }

        RegisterMissedShot(bullet);
    }


    private void HandlePlayerHit(Vector3 hitPosition)
    {
        if (!_isRunning ||
            _musicClock == null)
        {
            return;
        }

        double eventTime =
            _musicClock.SongTime;

        InteractionResult result =
            new InteractionResult(
                minigameId: "Danniel",
                interactionType: InteractionType.PlayerHit,
                outcome: InteractionOutcome.Failed,
                difficulty: _currentDifficulty,
                expectedTime: eventTime
            );

        _interactionRegistered.RaiseEvent(
            result
        );

        Debug.Log(
            $"[DannielInteractionController] " +
            $"Player hit by enemy projectile | " +
            $"Difficulty: {_currentDifficulty}",
            this
        );
    }


    private void ResolveSuccessfulShot(PooledBullet bullet, Vector3 feedbackPosition)
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
                outcome: InteractionOutcome.Success,
                difficulty: shot.Difficulty,
                expectedTime: shot.ExpectedTime,
                actualTime: shot.ActualTime,
                feedbackPosition: feedbackPosition
            );

        _interactionRegistered.RaiseEvent(
            result
        );
    }


    private void RegisterMissedShot(PooledBullet bullet)
    {
        if (!_pendingShots.TryGetValue(
            bullet,
            out PendingShot shot
        ))
        {
            return;
        }

        UntrackShot(bullet);

        if (shot.Difficulty != _currentDifficulty)
        {
            return;
        }

        _missedShotCount++;

        Debug.Log(
            $"[DannielInteractionController] " +
            $"Missed shot: {_missedShotCount}/{MissedShotsPerFailure}",
            this
        );

        if (_missedShotCount < MissedShotsPerFailure)
        {
            return;
        }

        _missedShotCount = 0;

        RegisterFailure(
            InteractionType.GunShoot
        );
    }


    private void RegisterFailure(InteractionType interactionType)
    {
        double eventTime =
            _musicClock != null
                ? _musicClock.SongTime
                : 0.0;

        InteractionResult result =
            new InteractionResult(
                minigameId: "Danniel",
                interactionType: interactionType,
                outcome: InteractionOutcome.Failed,
                difficulty: _currentDifficulty,
                expectedTime: eventTime
            );

        _interactionRegistered.RaiseEvent(
            result
        );
    }


    private void HandleEnemiesMissed(int count, DifficultyLevel difficulty)
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


    private void RegisterBonus(PooledBullet bullet, Collider enemyProjectile)
    {
        if (!_pendingShots.ContainsKey(bullet))
        {
            return;
        }

        Vector3 feedbackPosition =
            enemyProjectile.bounds.center;

        UntrackShot(bullet);

        if (_scoreProfile.BonusPoints <= 0)
        {
            return;
        }

        ScoreBonus bonus =
            new ScoreBonus(
                _scoreProfile.BonusPoints,
                feedbackPosition
            );

        _scoreBonusAwarded.RaiseEvent(bonus);

        Debug.Log(
            $"[DannielInteractionController] " +
            $"Enemy projectile destroyed | " +
            $"Bonus: +{_scoreProfile.BonusPoints}",
            this
        );
    }


    private void HandleDifficultyChanged(DifficultyLevel difficulty)
    {
        if (_currentDifficulty == difficulty)
        {
            return;
        }

        _currentDifficulty = difficulty;

        _missedShotCount = 0;

        Debug.Log(
            $"[DannielInteractionController] " +
            $"Difficulty changed to {_currentDifficulty}. " +
            $"Missed shot counter reset.",
            this
        );
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

        if (_scoreBonusAwarded == null)
        {
            Debug.LogError("[DannielInteractionController] ScoreBonusAwarded no está asignado.", this);
            return false;
        }

        if (_enemyProjectileLayer < 0)
        {
            Debug.LogError("[DannielInteractionController] No existe la layer EnemyProjectile.", this);
            return false;
        }

        return true;
    }
}