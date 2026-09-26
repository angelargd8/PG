using UnityEngine;

public abstract class HighScoreStorage : MonoBehaviour
{
    public abstract int LoadHighScore(string scoreId);

    public abstract void SaveHighScore(string scoreId, int highScore);
}