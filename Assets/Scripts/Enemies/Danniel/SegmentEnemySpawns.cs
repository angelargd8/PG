using UnityEngine;

public class SegmentEnemySpawns : MonoBehaviour
{
    [SerializeField]
    private Transform[] spawnPoints;


    public Transform[] SpawnPoints =>
        spawnPoints;


    public SegmentContent Content
    {
        get;
        private set;
    }


    private void Awake()
    {
        Content =
            GetComponent<SegmentContent>();
    }
}