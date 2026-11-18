#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class JoaquinHealthProbe
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;
    static JoaquinHealthProbe() => EditorApplication.update += Tick;
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool("JoaquinHealth.Pending", true);
        EditorApplication.EnterPlaymode();
    }
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Fields).SetValue(target, value);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("JOAQUIN HEALTH: " + message);
        checks++;
    }
    private static void Drain(IEnumerator iterator)
    {
        while (iterator.MoveNext()) if (iterator.Current is IEnumerator nested) Drain(nested);
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool("JoaquinHealth.Pending", false)) return;
        SessionState.SetBool("JoaquinHealth.Pending", false);
        try { Test(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    private static void Defeat(EnemyController enemy, int hits)
    {
        for (int i = 1; i <= hits; i++)
        {
            enemy.TakeDamage(1);
            Check(enemy.IsAlive == (i < hits), $"death must occur on hit {hits}, actual hit {i}");
        }
        Check(!enemy.gameObject.activeSelf, "death returns enemy to pool");
    }
    private static void Test()
    {
        var config = AssetDatabase.LoadAssetAtPath<JoaquinDifficultyConfigSO>("Assets/JoaquinDifficultyConfig.asset");
        Check(config != null, "real config imported");
        Check(config.GetProfile(DifficultyLevel.Easy).EnemyHealth == 1, "easy config");
        Check(config.GetProfile(DifficultyLevel.Normal).EnemyHealth == 3, "normal config");
        Check(config.GetProfile(DifficultyLevel.Hard).EnemyHealth == 5, "hard config");
        var target = new GameObject("Player target").transform;
        var poolRoot = new GameObject("Preload root");
        poolRoot.SetActive(false);
        var pools = new EnemyPool[2];
        for (int i = 0; i < pools.Length; i++)
        {
            var template = new GameObject(i == 0 ? "EnemyJoaquinn" : "EnemyJoaquincho");
            template.SetActive(false);
            Set(template.AddComponent<EnemyController>(), "maxHealth", 5);
            var host = new GameObject("Pool " + i);
            host.transform.SetParent(poolRoot.transform);
            pools[i] = host.AddComponent<EnemyPool>();
            Set(pools[i], "enemyPrefab", template);
            Set(pools[i], "playerTargetOverride", target);
            Set(pools[i], "prewarmCount", 1);
            Drain(pools[i].Preload());
        }
        poolRoot.SetActive(true);
        var spawner = new GameObject("Spawner").AddComponent<EnemyWaveSpawner>();
        Set(spawner, "enemyPools", pools);
        Set(spawner, "spawnPoints", new[] { new GameObject("Spawn A").transform, new GameObject("Spawn B").transform });
        var spawnGroup = typeof(EnemyWaveSpawner).GetMethod("SpawnGroup", Fields);
        EnemyController[] firstSpawn = null;
        foreach (var level in new[] { DifficultyLevel.Hard, DifficultyLevel.Easy, DifficultyLevel.Normal, DifficultyLevel.Hard })
        {
            var profile = config.GetProfile(level);
            spawner.SetDifficulty(1, 2, 2, 4, profile.EnemyHealth);
            Check((int)spawnGroup.Invoke(spawner, new object[] { 2 }) == 2, "both enemy types spawn");
            var enemies = spawner.GetComponentsInChildren<EnemyController>();
            Check(enemies.Length == 2, "two active enemies");
            if (firstSpawn == null) firstSpawn = enemies;
            else foreach (var enemy in enemies) Check(Array.IndexOf(firstSpawn, enemy) >= 0, "same pooled instances reused");
            foreach (var enemy in enemies) Defeat(enemy, profile.EnemyHealth);
        }
        spawner.SetDifficulty(1, 2, 2, 4, 5);
        spawnGroup.Invoke(spawner, new object[] { 2 });
        var existing = spawner.GetComponentsInChildren<EnemyController>();
        foreach (var enemy in existing) enemy.TakeDamage(1);
        spawner.SetDifficulty(1, 2, 2, 4, 1);
        foreach (var enemy in existing) Defeat(enemy, 4); // No healing or retroactive damage on difficulty change.
        foreach (var pool in pools)
        {
            Defeat(pool.GetEnemy(spawner.transform, Vector3.zero, Quaternion.identity, 1).GetComponent<EnemyController>(), 1);
            Defeat(pool.GetEnemy(spawner.transform, Vector3.zero, Quaternion.identity).GetComponent<EnemyController>(), 5);
            Defeat(pool.GetEnemy(spawner.transform, Vector3.zero, Quaternion.identity, 0).GetComponent<EnemyController>(), 1);
        }
        Debug.Log($"JOAQUIN HEALTH PROBE PASSED: {checks} checks");
    }
}
#endif
