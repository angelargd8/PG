using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class JuanAndresTarget : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private RectTransform _directionRing;
    [SerializeField] private RectTransform _lifetimeFill;
    [SerializeField] private Image _toolIcon;


    [Header("Tool Sprites")]
    [SerializeField] private Sprite _soapSprite;
    [SerializeField] private Sprite _brushSprite;


    private JuanAndresTargetPool _pool;
    private JuanAndresSpawnPoint _spawnPoint;
    private ExperienceMusicClock _musicClock;
    private Transform _head;
    private Vector3 _directionBaseScale;
    private Vector3 _lifetimeBaseScale;
    private bool _isResolved;
    private bool _visualStateInitialized;


    public JuanAndresActionDirection ExpectedDirection { get; private set; }
    public JuanAndresToolType ExpectedTool { get; private set; }
    public DifficultyLevel Difficulty { get; private set; }
    public double SpawnTime { get; private set; }
    public double ExpectedTime { get; private set; }
    public double ExpireTime { get; private set; }
    public int PairId { get; private set; }
    public bool IsPaired => PairId >= 0;
    public bool IsResolved => _isResolved;


    private void Update()
    {
        if (_isResolved ||
            _musicClock == null)
        {
            return;
        }

        UpdateLifetime();
    }


    private void LateUpdate()
    {
        FacePlayer();
    }


    public void Initialize(
        JuanAndresTargetPool pool,
        JuanAndresSpawnPoint spawnPoint,
        JuanAndresActionDirection expectedDirection,
        JuanAndresToolType expectedTool,
        DifficultyLevel difficulty,
        double spawnTime,
        double expectedTime,
        double expireTime,
        int pairId)
    {
        EnsureVisualStateInitialized();

        _pool = pool;
        _spawnPoint = spawnPoint;

        ExpectedDirection = expectedDirection;
        ExpectedTool = expectedTool;
        Difficulty = difficulty;

        SpawnTime = spawnTime;
        ExpectedTime = expectedTime;
        ExpireTime = expireTime;

        PairId = pairId;

        _isResolved = false;

        ResolveRuntimeReferences();

        transform.SetParent(
            spawnPoint.transform,
            false
        );

        transform.localPosition =
            Vector3.zero;

        ConfigureDirection();
        ConfigureTool();
        ResetLifetimeVisual();

        FacePlayer();
    }


    private void EnsureVisualStateInitialized()
    {
        if (_visualStateInitialized)
        {
            return;
        }

        if (_directionRing != null)
        {
            _directionBaseScale =
                _directionRing.localScale;
        }

        if (_lifetimeFill != null)
        {
            _lifetimeBaseScale =
                _lifetimeFill.localScale;
        }

        _visualStateInitialized = true;
    }


    public bool TryResolve()
    {
        if (_isResolved)
        {
            return false;
        }

        _isResolved = true;

        ReleaseTarget();

        return true;
    }


    public void ResetTarget()
    {
        EnsureVisualStateInitialized();

        if (_spawnPoint != null)
        {
            _spawnPoint.Release(this);
        }

        _spawnPoint = null;
        _pool = null;

        _isResolved = false;

        PairId = -1;

        ResetVisuals();
    }


    private void UpdateLifetime()
    {
        double songTime =
            _musicClock.SongTime;

        double totalDuration =
            ExpireTime - SpawnTime;

        float remaining = 0f;

        if (totalDuration > 0.0)
        {
            remaining =
                Mathf.Clamp01(
                    (float)(
                        (ExpireTime - songTime) /
                        totalDuration
                    )
                );
        }

        if (_lifetimeFill != null)
        {
            _lifetimeFill.localScale =
                _lifetimeBaseScale *
                remaining;
        }

        if (songTime >= ExpireTime)
        {
            Expire();
        }
    }


    private void Expire()
    {
        if (_isResolved)
        {
            return;
        }

        _isResolved = true;

        ReleaseTarget();
    }


    private void ReleaseTarget()
    {
        if (_pool != null)
        {
            _pool.ReleaseTarget(this);
            return;
        }

        gameObject.SetActive(false);
    }


    private void ConfigureDirection()
    {
        if (_directionRing == null)
        {
            return;
        }

        Vector3 scale =
            _directionBaseScale;

        float absoluteX =
            Mathf.Abs(scale.x);

        scale.x =
            ExpectedDirection ==
            JuanAndresActionDirection.Clockwise
                ? absoluteX
                : -absoluteX;

        _directionRing.localScale =
            scale;
    }


    private void ConfigureTool()
    {
        if (_toolIcon == null)
        {
            return;
        }

        _toolIcon.sprite =
            ExpectedTool ==
            JuanAndresToolType.Soap
                ? _soapSprite
                : _brushSprite;
    }


    private void ResetLifetimeVisual()
    {
        if (_lifetimeFill != null)
        {
            _lifetimeFill.localScale =
                _lifetimeBaseScale;
        }
    }


    private void ResetVisuals()
    {
        if (_directionRing != null)
        {
            _directionRing.localScale =
                _directionBaseScale;
        }

        if (_lifetimeFill != null)
        {
            _lifetimeFill.localScale =
                _lifetimeBaseScale;
        }
    }


    private void ResolveRuntimeReferences()
    {
        if (_musicClock == null)
        {
            _musicClock =
                FindFirstObjectByType<ExperienceMusicClock>();
        }

        if (_head == null)
        {
            Camera mainCamera =
                Camera.main;

            if (mainCamera != null)
            {
                _head =
                    mainCamera.transform;
            }
        }
    }


    private void FacePlayer()
    {
        if (_head == null)
        {
            return;
        }

        Vector3 direction =
            transform.position -
            _head.position;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation =
            Quaternion.LookRotation(
                direction.normalized
            );
    }
}