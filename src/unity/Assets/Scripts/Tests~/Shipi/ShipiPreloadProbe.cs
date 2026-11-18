#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ShipiPreloadProbe
{
    private const string Pending = "ShipiPreloadProbe.Pending";
    static ShipiPreloadProbe() => EditorApplication.update += Tick;
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    private static void Tick()
    {
        if (EditorApplication.isPlaying && EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("Shipi preload tests").AddComponent<ShipiPreloadProbeRunner>();
    }
    public static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    public static object Get(object target, string field) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
}

public sealed class ShipiPreloadProbeRunner : MonoBehaviour
{
    private int checks;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("SHIPI PROBE: " + message);
        checks++;
    }
    private void Start() => StartCoroutine(ExperiencePreloadOperation.Run(Run(), error =>
    {
        Debug.LogException(error);
        EditorApplication.Exit(1);
    }));

    private IEnumerator Run()
    {
        var root = new GameObject("Inactive ExperienceContent");
        root.SetActive(false);
        var poolObject = new GameObject("Food pool");
        poolObject.transform.SetParent(root.transform, false);
        var pool = poolObject.AddComponent<ShipiFoodPool>();
        var definitions = new ShipiFoodDefinitionSO[6];
        for (int i = 0; i < definitions.Length; i++)
        {
            var template = new GameObject("Food template " + i);
            template.SetActive(false);
            template.AddComponent<MeshRenderer>();
            template.AddComponent<BoxCollider>();
            var child = new GameObject("Inactive visual child", typeof(MeshRenderer), typeof(BoxCollider));
            child.transform.SetParent(template.transform, false);
            child.SetActive(false);
            var food = template.AddComponent<ShipiFood>();
            Check(ShipiPreloadProbe.Get(food, "_wholeRenderers") == null, "template Awake has not run");
            definitions[i] = ScriptableObject.CreateInstance<ShipiFoodDefinitionSO>();
            ShipiPreloadProbe.Set(definitions[i], "_wholePrefab", food);
        }
        ShipiPreloadProbe.Set(pool, "_foodDefinitions", definitions);
        ShipiPreloadProbe.Set(pool, "_prewarmPerFood", 1);
        var bootstrap = new GameObject("Shipi bootstrap").AddComponent<ExperienceSceneBootstrap>();
        ShipiPreloadProbe.Set(bootstrap, "gameplayRoot", root);
        ShipiPreloadProbe.Set(bootstrap, "preloaders", new MonoBehaviour[] { pool });
        Exception failure = null;
        yield return ExperiencePreloadOperation.Run(bootstrap.Prepare(), error => failure = error);

        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-shipiExpectFailure") >= 0)
        {
            Check(failure is NullReferenceException, "original code reproduces null reference");
            Check(failure.StackTrace.Contains("ShipiFood.SetWholeVisualEnabled"), "failure originates in food reset visual cache");
            Debug.Log("SHIPI ORIGINAL ERROR REPRODUCED: " + failure);
            EditorApplication.Exit(0);
            yield break;
        }

        Check(failure == null, "preload succeeds: " + failure);
        Check(bootstrap.IsPrepared && !bootstrap.IsRunning, "scene prepared without starting gameplay");
        Check(!root.activeSelf && pool.ActiveFoodCount == 0, "preload leaves root inactive and no active food");
        Check(pool.GetComponentsInChildren<ShipiFood>(true).Length == 6, "all food types prewarmed");
        foreach (var food in pool.GetComponentsInChildren<ShipiFood>(true))
        {
            Check(!food.gameObject.activeSelf, "prewarmed food remains inactive");
            Check(((Renderer[])ShipiPreloadProbe.Get(food, "_wholeRenderers")).Length == 2, "inactive child renderer cached");
            Check(((Collider[])ShipiPreloadProbe.Get(food, "_wholeColliders")).Length == 2, "inactive child collider cached");
            food.ResetFood();
        }
        yield return pool.Preload();
        Check(pool.GetComponentsInChildren<ShipiFood>(true).Length == 6, "preload is idempotent");
        bootstrap.BeginExperience();
        var cutPrefab = new GameObject("Cut visual template");
        cutPrefab.SetActive(false);
        for (int i = 0; i < 60; i++)
        {
            var food = pool.GetFood(definitions[i % 6], ShipiCutDirection.TopToBottom, DifficultyLevel.Normal);
            Check(food != null && food.gameObject.activeInHierarchy, "food activates after scene start");
            food.ShowCutVisual(cutPrefab);
            Check(!food.GetComponent<Renderer>().enabled && !food.GetComponent<Collider>().enabled, "cut hides intact visual and collisions");
            food.MarkResolved();
            pool.ReleaseFood(food);
            Check(food.GetComponent<Renderer>().enabled && food.GetComponent<Collider>().enabled, "return restores intact visual and collisions");
            Check(!food.IsResolved && food.Definition == null && !food.gameObject.activeSelf, "return resets reused food");
            yield return null; // Allow deferred destruction of cut visual before reuse.
        }
        Check(pool.ActiveFoodCount == 0, "all reused foods returned");
        bootstrap.EndExperience();
        Check(!root.activeSelf, "scene can deactivate after use");
        Debug.Log($"SHIPI PRELOAD PROBE PASSED: {checks} checks");
        EditorApplication.Exit(0);
    }
}
#endif
