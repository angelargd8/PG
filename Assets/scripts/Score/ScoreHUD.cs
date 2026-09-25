using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScoreHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _deltaText;

    [Header("Events")]
    [SerializeField] private ScoreChangedEventChannelSO _scoreChanged;

    [Header("Colors")]
    [SerializeField] private Color _neutralColor = Color.white;
    [SerializeField] private Color _positiveColor = Color.green;
    [SerializeField] private Color _negativeColor = Color.red;

    [Header("Feedback")]
    [Min(0.1f)]
    [SerializeField] private float _feedbackDuration = 0.6f;

    private Coroutine _feedbackCoroutine;


    private void OnEnable()
    {
        if (_scoreChanged != null)
        {
            _scoreChanged.Raised += HandleScoreChanged;
        }
    }


    private void OnDisable()
    {
        if (_scoreChanged != null)
        {
            _scoreChanged.Raised -= HandleScoreChanged;
        }

        if (_feedbackCoroutine != null)
        {
            StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = null;
        }
    }


    private void Start()
    {
        SetScore(0);

        if (_deltaText != null)
        {
            _deltaText.gameObject.SetActive(false);
        }
    }


    private void HandleScoreChanged(ScoreChange scoreChange)
    {
        SetScore(scoreChange.CurrentScore);

        if (scoreChange.Delta == 0)
        {
            return;
        }

        if (_feedbackCoroutine != null)
        {
            StopCoroutine(_feedbackCoroutine);
        }

        _feedbackCoroutine = StartCoroutine(
            ShowScoreFeedback(scoreChange.Delta)
        );
    }


    private IEnumerator ShowScoreFeedback(int delta)
    {
        bool isPositive = delta > 0;

        Color feedbackColor = isPositive
            ? _positiveColor
            : _negativeColor;

        if (_scoreText != null)
        {
            _scoreText.color = feedbackColor;
        }

        if (_deltaText != null)
        {
            _deltaText.color = feedbackColor;
            _deltaText.text = delta > 0
                ? $"+{delta}"
                : delta.ToString();

            _deltaText.gameObject.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(_feedbackDuration);

        if (_scoreText != null)
        {
            _scoreText.color = _neutralColor;
        }

        if (_deltaText != null)
        {
            _deltaText.gameObject.SetActive(false);
        }

        _feedbackCoroutine = null;
    }


    private void SetScore(int score)
    {
        if (_scoreText == null)
        {
            return;
        }

        _scoreText.text = score.ToString();
        _scoreText.color = _neutralColor;
    }
}