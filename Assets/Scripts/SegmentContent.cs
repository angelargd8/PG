using UnityEngine;
using System.Collections.Generic;

public class SegmentContent : MonoBehaviour
{
    private List<GameObject> activeEnemies =
        new List<GameObject>();


    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy != null)
        {
            activeEnemies.Add(enemy);
        }
    }


    public void UnregisterEnemy(GameObject enemy)
    {
        activeEnemies.Remove(enemy);
    }


    public void ClearEnemies(EnemyPool enemyPool)
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null &&
                enemy.activeSelf)
            {
                enemyPool.ReleaseEnemy(enemy);
            }
        }

        activeEnemies.Clear();
    }
}