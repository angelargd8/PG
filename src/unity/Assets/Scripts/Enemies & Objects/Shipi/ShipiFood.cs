using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShipiFood : MonoBehaviour
{
    private Renderer[] _wholeRenderers;
    private Collider[] _wholeColliders;
    private GameObject _cutVisual;


    public ShipiFoodDefinitionSO Definition { get; private set; }
    public ShipiCutDirection ExpectedDirection { get; private set; }
    public DifficultyLevel Difficulty { get; private set; }

    public ShipiMovePoint CurrentPoint { get; private set; }
    public int CurrentPointIndex { get; private set; }

    public double CueTime { get; private set; }
    public double ExpectedCutTime { get; private set; }

    public bool IsResolved { get; private set; }


    private void Awake()
    {
        EnsureVisualReferences();
    }


    private void EnsureVisualReferences()
    {

        if (_wholeRenderers != null && _wholeColliders != null)
        {
            return;
        }

        _wholeRenderers =
            GetComponentsInChildren<Renderer>(
                true
            );

        _wholeColliders =
            GetComponentsInChildren<Collider>(
                true
            );
    }


    public void Initialize(
        ShipiFoodDefinitionSO definition,
        ShipiCutDirection expectedDirection,
        DifficultyLevel difficulty)
    {
        Definition = definition;
        ExpectedDirection = expectedDirection;
        Difficulty = difficulty;

        CurrentPoint = null;
        CurrentPointIndex = -1;

        CueTime = 0d;
        ExpectedCutTime = 0d;

        IsResolved = false;
    }


    public void MoveToPoint(
        ShipiMovePoint point,
        int pointIndex,
        double beatTime)
    {
        if (point == null)
        {
            return;
        }

        CurrentPoint = point;
        CurrentPointIndex = pointIndex;

        transform.SetParent(
            point.transform,
            false
        );

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (_cutVisual != null)
        {
            _cutVisual.transform.SetParent(
                point.transform,
                false
            );

            _cutVisual.transform.localPosition =
                Vector3.zero;

            _cutVisual.transform.localRotation =
                Quaternion.identity;
        }

        if (point.Type ==
            ShipiMovePointType.Cutting)
        {
            ExpectedCutTime = beatTime;
        }
    }

    public void SetCutTiming(
        double cueTime,
        double expectedCutTime)
    {
        CueTime = cueTime;
        ExpectedCutTime = expectedCutTime;
    }

    public void ShowCutVisual(GameObject cutPrefab)
    {
        if (cutPrefab == null ||
            _cutVisual != null)
        {
            return;
        }

        SetWholeVisualEnabled(
            false
        );

        Transform parent =
            CurrentPoint != null
                ? CurrentPoint.transform
                : transform.parent;

        _cutVisual =
            Instantiate(
                cutPrefab,
                parent
            );

        _cutVisual.transform.localPosition =
            Vector3.zero;

        _cutVisual.transform.localRotation =
            Quaternion.identity;
    }


    private void SetWholeVisualEnabled(bool enabled)
    {
        EnsureVisualReferences();

        for (int i = 0;
            i < _wholeRenderers.Length;
            i++)
        {
            if (_wholeRenderers[i] != null)
            {
                _wholeRenderers[i].enabled =
                    enabled;
            }
        }

        for (int i = 0;
            i < _wholeColliders.Length;
            i++)
        {
            if (_wholeColliders[i] != null)
            {
                _wholeColliders[i].enabled =
                    enabled;
            }
        }
    }


    public void MarkResolved()
    {
        IsResolved = true;
    }

    public void ResetFood()
    {
        if (_cutVisual != null)
        {
            _cutVisual.SetActive(
                false
            );

            Destroy(
                _cutVisual
            );

            _cutVisual = null;
        }

        SetWholeVisualEnabled(
            true
        );
        
        Definition = null;

        ExpectedDirection = ShipiCutDirection.None;

        Difficulty = DifficultyLevel.Normal;

        CurrentPoint = null;
        CurrentPointIndex = -1;

        ExpectedCutTime = 0d;

        IsResolved = false;

        transform.localPosition =
            Vector3.zero;

        transform.localRotation =
            Quaternion.identity;
    }
}
