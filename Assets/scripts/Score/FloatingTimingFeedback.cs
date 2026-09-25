using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FloatingTimingFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text _text;

    [Header("Animation")]
    [SerializeField] private float _verticalMovement = 0.2f;
    [Min(0.1f)]
    [SerializeField] private float _duration = 0.8f;

    [Header("Colors")]
    [SerializeField] private Color _perfectColor = new Color32(255, 215, 0, 255);
    [SerializeField] private Color _goodColor = new Color32(192, 192, 192, 255);
    [SerializeField] private Color _niceColor = new Color32(205, 127, 50, 255);
    [SerializeField] private Color _outlineColor = Color.black;
    [Range(0f, 1f)]
    [SerializeField] private float _outlineWidth = 0.2f;

    private Transform _head;


    private void Awake()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            _head = mainCamera.transform;
        }
    }


    public void Show(TimingJudgement judgement, Vector3 position)
    {
        if (_text == null)
        {
            Debug.LogError(
                "[FloatingTimingFeedback] TMP_Text is not assigned.",
                this
            );

            return;
        }

        transform.position = position;

        _text.text = GetSpanishText(judgement);
        _text.color = GetFeedbackColor(judgement);

        _text.outlineColor = _outlineColor;
        _text.outlineWidth = _outlineWidth;

        gameObject.SetActive(true);

        Renderer textRenderer = _text.GetComponent<Renderer>();

        StopAllCoroutines();
        StartCoroutine(Animate());
    }


    private IEnumerator Animate()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition =
            startPosition + Vector3.up * _verticalMovement;

        Color startColor = _text.color;
        startColor.a = 1f;

        Color endColor = startColor;
        endColor.a = 0f;

        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / _duration);

            transform.position = Vector3.Lerp(startPosition, endPosition, progress);

            _text.color = Color.Lerp(startColor, endColor, progress);

            FacePlayer();

            yield return null;
        }

        _text.color = startColor;
        gameObject.SetActive(false);
    }


    private void FacePlayer()
    {
        if (_head == null)
        {
            return;
        }

        Vector3 direction = transform.position - _head.position;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private string GetSpanishText(TimingJudgement judgement)
    {
        return judgement switch
        {
            TimingJudgement.Nice => "BIEN",
            TimingJudgement.Good => "MUY BIEN",
            TimingJudgement.Perfect => "PERFECTO",
            _ => string.Empty
        };
    }

    private Color GetFeedbackColor(TimingJudgement judgement)
    {
        return judgement switch
        {
            TimingJudgement.Perfect => _perfectColor,
            TimingJudgement.Good => _goodColor,
            TimingJudgement.Nice => _niceColor,
            _ => Color.white
        };
    }
}