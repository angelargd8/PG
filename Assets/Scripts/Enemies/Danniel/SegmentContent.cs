using UnityEngine;
using System.Collections.Generic;

public class SegmentContent : MonoBehaviour
{
    // por el momento habrá pocos enemigos
    // Damos capacidad inicial para evitar crecimiento interno
    private readonly List<GameObject> activeEnemies =
        new List<GameObject>(4);


    public int ActiveEnemyCount =>
        activeEnemies.Count;


    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        activeEnemies.Add(enemy);

        EnemyController enemyScript =
            enemy.GetComponent<EnemyController>();

        if (enemyScript != null)
        {
            enemyScript.SetSegmentContent(this);
        }
    }


    public void UnregisterEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        int index =
            activeEnemies.IndexOf(enemy);

        if (index < 0)
            return;


        // Remove sin desplazar toda la lista
        int lastIndex =
            activeEnemies.Count - 1;

        activeEnemies[index] =
            activeEnemies[lastIndex];

        activeEnemies.RemoveAt(lastIndex);
    }


    public void ClearEnemies(EnemyPool enemyPool)
    {
        if (enemyPool == null)
            return;


        // Recorremos hacia atrás para poder
        // modificar la lista de forma segura
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy =
                activeEnemies[i];

            // Primero quitar nuestra referencia
            activeEnemies.RemoveAt(i);


            if (enemy == null)
                continue;


            EnemyController enemyScript =
                enemy.GetComponent<EnemyController>();

            if (enemyScript != null)
            {
                enemyScript.ClearSegmentContent(this);
            }


            if (enemy.activeSelf)
            {
                enemyPool.ReleaseEnemy(enemy);
            }
        }
    }
}