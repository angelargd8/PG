using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerPrefsHighScoreStorage : HighScoreStorage
{
    private const string KeyPrefix = "HighScore_";


    public override int LoadHighScore(string scoreId)
    {
        if (string.IsNullOrEmpty(scoreId))
        {
            return 0;
        }

        string key = GetKey(scoreId);

        int highScore =
            PlayerPrefs.GetInt(key, 0);

        return Mathf.Max(0, highScore);
    }


    public override void SaveHighScore(string scoreId, int highScore)
    {
        if (string.IsNullOrEmpty(scoreId))
        {
            return;
        }

        string key = GetKey(scoreId);

        PlayerPrefs.SetInt(
            key,
            Mathf.Max(0, highScore)
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"[HighScoreStorage] Saved | " +
            $"{scoreId}: {highScore}",
            this
        );
    }


    private string GetKey(string scoreId)
    {
        return $"{KeyPrefix}{scoreId}";
    }
}