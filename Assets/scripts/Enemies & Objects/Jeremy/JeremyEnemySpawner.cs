using UnityEngine;

[DisallowMultipleComponent]
public sealed class JeremyEnemySpawner : MonoBehaviour
{
    public void SpawnAt(Transform spawnPoint)
    {
        Debug.Log(
            $"Spawn solicitado en: {spawnPoint.name}",
            this
        );
    }
}