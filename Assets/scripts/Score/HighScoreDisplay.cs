using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HighScoreDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text _highScoreText;
    [SerializeField] private ExperienceDefinitionSO _experience;
    [SerializeField] private ExperienceSceneSelector _sceneSelector;


    private HighScoreSystem _highScoreSystem;


    private void Start()
    {
        _highScoreSystem =
            FindFirstObjectByType<HighScoreSystem>();

        if (_highScoreSystem == null)
        {
            Debug.LogError(
                "[HighScoreDisplay] No se encontró HighScoreSystem.",
                this
            );

            return;
        }

        RefreshHighScore();
    }


    private void OnEnable()
    {
        if (_sceneSelector != null)
        {
            _sceneSelector.SelectionChanged += RefreshHighScore;
        }
    }


    private void OnDisable()
    {
        if (_sceneSelector != null)
        {
            _sceneSelector.SelectionChanged -= RefreshHighScore;
        }
    }


    private void RefreshHighScore()
    {
        if (_highScoreSystem == null ||
            _experience == null ||
            _sceneSelector == null ||
            _highScoreText == null)
        {
            return;
        }

        ScoreRunContext context = ScoreRunContext.Create(
            _experience,
            _sceneSelector.SelectedScene,
            _sceneSelector.PlayFullSequence
        );

        int highScore =
            _highScoreSystem.GetHighScore(context);

        _highScoreText.text = $"{highScore} pts";

        Debug.Log(
            $"[HighScoreDisplay] " +
            $"Id: {context.ScoreId} | " +
            $"High Score: {highScore}",
            this
        );
    }
}