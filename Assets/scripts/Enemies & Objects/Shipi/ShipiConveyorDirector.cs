using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiConveyDirector :
    MonoBehaviour,
    IExperienceRuntime
{
    [Header("References")]
    [SerializeField] private ShipiFoodSpawner _foodSpawner;

    [Header("Move Points")]
    [SerializeField] private ShipiMovePoint[] _movePoints;

    [Header("Beat Map")]
    [SerializeField] private BeatMapSO _beatMap;

    [Header("Difficulty")]
    [SerializeField] private ShipiDifficultyConfigSO _difficultyConfig;
    [SerializeField] private DifficultyLevelEventChannelSO _difficultyChanged;


    public event Action<ShipiFood> FoodEnteredCuttingPoint;
    public event Action<ShipiFood> FoodLeftCuttingPoint;


    private readonly List<ShipiFood> _activeFoods =
        new List<ShipiFood>();

    private ExperienceMusicClock _musicClock;

    private bool _isRunning;

    private int _nextBeatIndex;
    private int _beatsSinceLastMove;

    private DifficultyLevel _currentDifficulty;
    private ShipiDifficultyProfile _currentProfile;


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
                "[ShipiFoodDirector] No se encontró ExperienceMusicClock.",
                this
            );

            return;
        }

        if (_difficultyChanged != null)
        {
            _difficultyChanged.Raised +=
                SetDifficulty;

            _currentDifficulty =
                _difficultyChanged.CurrentDifficulty;
        }
        else
        {
            _currentDifficulty =
                DifficultyLevel.Normal;
        }

        _currentProfile =
            _difficultyConfig.GetProfile(
                _currentDifficulty
            );

        _activeFoods.Clear();

        _nextBeatIndex = 0;

        while (
            _nextBeatIndex < _beatMap.Beats.Count &&
            _beatMap.Beats[_nextBeatIndex].Time <
            _musicClock.SongTime)
        {
            _nextBeatIndex++;
        }

        // Hace que el primer beat disponible
        // produzca un movimiento/spawn.
        _beatsSinceLastMove =
            _currentProfile.MoveEveryNBeats - 1;

        _isRunning = true;
    }


    public void EndExperience()
    {
        if (_difficultyChanged != null)
        {
            _difficultyChanged.Raised -= SetDifficulty;
        }

        _isRunning = false;

        _activeFoods.Clear();

        if(_foodSpawner != null)
        {
            _foodSpawner.ReleaseAll();
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

        ProcessBeats();
    }


    private void ProcessBeats()
    {
        double songTime =
            _musicClock.SongTime;

        while (
            _nextBeatIndex < _beatMap.Beats.Count &&
            songTime >=
            _beatMap.Beats[_nextBeatIndex].Time)
        {
            double beatTime =
                _beatMap.Beats[_nextBeatIndex].Time;

            ProcessBeat(
                beatTime
            );

            _nextBeatIndex++;
        }
    }


    private void ProcessBeat(double beatTime)
    {
        _beatsSinceLastMove++;

        if (_beatsSinceLastMove <
            _currentProfile.MoveEveryNBeats)
        {
            return;
        }

        _beatsSinceLastMove = 0;

        MoveFoods(
            beatTime
        );

        SpawnFood(
            beatTime
        );
    }


    private void MoveFoods(double beatTime)
    {
        for (
            int i = _activeFoods.Count - 1;
            i >= 0;
            i--)
        {
            ShipiFood food =
                _activeFoods[i];

            if (food == null)
            {
                _activeFoods.RemoveAt(i);
                continue;
            }

            int currentIndex =
                food.CurrentPointIndex;

            if (currentIndex < 0)
            {
                continue;
            }

            ShipiMovePoint currentPoint =
                _movePoints[currentIndex];

            if (currentPoint.Type ==
                ShipiMovePointType.Cutting)
            {
                FoodLeftCuttingPoint?.Invoke(
                    food
                );
            }

            int nextIndex =
                currentIndex + 1;

            if (nextIndex >=
                _movePoints.Length)
            {
                Destroy(
                    food
                );

                _activeFoods.RemoveAt(i);

                continue;
            }

            ShipiMovePoint nextPoint =
                _movePoints[nextIndex];

            food.MoveToPoint(
                nextPoint,
                nextIndex,
                beatTime
            );

            if (nextPoint.Type ==
                ShipiMovePointType.Cutting)
            {
                FoodEnteredCuttingPoint?.Invoke(
                    food
                );
            }

            if (nextPoint.Type ==
                ShipiMovePointType.End)
            {
                Destroy(
                    food
                );

                _activeFoods.RemoveAt(i);
            }
        }
    }


    private void SpawnFood(double beatTime)
    {
        ShipiMovePoint entrance =
            _movePoints[0];

        ShipiFood food =
            _foodSpawner.Spawn(
                entrance,
                _currentDifficulty
            );

        if (food == null)
        {
            return;
        }

        food.MoveToPoint(
            entrance,
            0,
            beatTime
        );

        _activeFoods.Add(
            food
        );
    }


    private void SetDifficulty(
        DifficultyLevel difficulty)
    {
        if (_currentDifficulty ==
            difficulty)
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
            $"[ShipiFoodDirector] Difficulty changed to {difficulty}. " +
            $"Move every {_currentProfile.MoveEveryNBeats} beats.",
            this
        );
    }


    private bool ValidateReferences()
    {
        if (_foodSpawner == null)
        {
            Debug.LogError(
                "[ShipiFoodDirector] FoodSpawner no está asignado.",
                this
            );

            return false;
        }

        if (_movePoints == null ||
            _movePoints.Length < 2)
        {
            Debug.LogError(
                "[ShipiFoodDirector] No hay suficientes MovePoints.",
                this
            );

            return false;
        }

        for (int i = 0; i < _movePoints.Length; i++)
        {
            if (_movePoints[i] == null)
            {
                Debug.LogError(
                    $"[ShipiFoodDirector] MovePoint {i} es null.",
                    this
                );

                return false;
            }
        }

        if (_movePoints[0].Type !=
            ShipiMovePointType.Entrance)
        {
            Debug.LogError(
                "[ShipiFoodDirector] El primer MovePoint debe ser Entrance.",
                this
            );

            return false;
        }

        if (_movePoints[_movePoints.Length - 1].Type !=
            ShipiMovePointType.End)
        {
            Debug.LogError(
                "[ShipiFoodDirector] El último MovePoint debe ser End.",
                this
            );

            return false;
        }

        if (_beatMap == null ||
            _beatMap.Beats.Count == 0)
        {
            Debug.LogError(
                "[ShipiFoodDirector] BeatMap no es válido.",
                this
            );

            return false;
        }

        if (_difficultyConfig == null)
        {
            Debug.LogError(
                "[ShipiFoodDirector] DifficultyConfig no está asignado.",
                this
            );

            return false;
        }

        return true;
    }
}