using UnityEngine;

public class SegmentEnemySpawns : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    public Transform[] SpawnPoints => spawnPoints;
}
