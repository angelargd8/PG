#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public sealed class CreditsProbeRunner : MonoBehaviour
{
    private int checks, completions, finishes, finalScore;
    private AppStateMachine app;
    private ExperienceDefinitionSO experience;
    private VoidEventChannelSO songFinished, menu;
    private ExperienceEventChannelSO requested;
    private ScoreBonusEventChannelSO bonus;
    private GameObject resultsPanel;
    private static T Asset<T>(string name) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>($"Assets/{name}.asset");
    private void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("CREDITSPROBE: " + message);
        checks++;
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(ExperiencePreloadOperation.Run(Run(), error => { Debug.LogException(error); EditorApplication.Exit(1); }));
    }
    private IEnumerator Until(Func<bool> condition, string label)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 30;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble > deadline) throw new Exception("Timeout: " + label);
            yield return null;
        }
    }
    private IEnumerator Begin(bool full)
    {
        requested.RaiseEvent(new ExperienceRequest(experience, 0, full));
        yield return Until(() => app.CurrentState == AppState.Experience, "experience ready");
        yield return null;
        resultsPanel = GameObject.Find("Results").GetComponent<ResultsScreenController>()
            .GetType().GetField("_resultsPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(FindFirstObjectByType<ResultsScreenController>()) as GameObject;
        Check(!resultsPanel.activeSelf, "results hidden before ending");
        bonus.RaiseEvent(new ScoreBonus(123, Vector3.zero));
        Check(FindFirstObjectByType<ScoreSystem>().CurrentScore == 123, "score starts fresh");
    }
    private void FinishSong()
    {
        var music = FindFirstObjectByType<PlayableDirector>();
        music.time = music.duration;
        music.Evaluate();
    }
    private IEnumerator BackToMenu()
    {
        menu.RaiseEvent();
        yield return Until(() => app.CurrentState == AppState.MainMenu, "return to menu");
        Check(!SceneManager.GetSceneByName("CreditScene").isLoaded, "credits unloaded on exit");
        Check(!SceneManager.GetSceneByName("ExperienceCore").isLoaded, "core unloaded on exit");
        Check(Time.timeScale == 1 && !AudioListener.pause, "pause restored on exit");
    }
    private IEnumerator Run()
    {
        app = FindFirstObjectByType<AppStateMachine>();
        experience = Asset<ExperienceDefinitionSO>("Experience");
        requested = Asset<ExperienceEventChannelSO>("Requested");
        songFinished = Asset<VoidEventChannelSO>("SongFinished");
        menu = Asset<VoidEventChannelSO>("MenuRequested");
        bonus = Asset<ScoreBonusEventChannelSO>("Bonus");
        Asset<VoidEventChannelSO>("Completed").Raised += () => completions++;
        songFinished.Raised += () => finishes++;
        Asset<FinalScoreEventChannelSO>("FinalScore").Raised += value =>
        {
            finalScore = value;
            Asset<RunResultEventChannelSO>("Result").RaiseEvent(new RunResult(new ScoreRunContext("test", "Test", false), value, 0, value, true));
        };
        yield return Until(() => app.CurrentState == AppState.MainMenu, "initial menu");

        foreach (bool full in new[] { false, true })
        {
            yield return Begin(full);
            int previousCompletions = completions, previousFinishes = finishes;
            FinishSong();
            yield return Until(() => SceneManager.GetActiveScene().name == "CreditScene", "credits activation");
            double visibleStart = Time.realtimeSinceStartupAsDouble;
            Check(finishes == previousFinishes + 1, "one song-finished event");
            Check(completions == previousCompletions && !resultsPanel.activeSelf, "credits precede results");
            Check(Time.timeScale == 0 && AudioListener.pause, "gameplay frozen during credits");
            Check(FindFirstObjectByType<FullExperienceDirector>().LoadedSceneCount == 0, "gameplay scenes released");
            Check(GameObject.Find("Score HUD") == null, "HUD hidden for credits");
            bonus.RaiseEvent(new ScoreBonus(999, Vector3.zero));
            Check(FindFirstObjectByType<ScoreSystem>().CurrentScore == 123, "late bonus cannot alter final score");
            songFinished.RaiseEvent(); // Duplicate completion must not restart the credits timer.
            yield return null;
            var canvas = FindFirstObjectByType<VRLoadingCanvasBinder>();
            Check(canvas != null && canvas.gameObject.scene.name == "CreditScene", "canvas stays owned by credits scene");
            Check(Vector3.Distance(canvas.transform.position, Camera.main.transform.position) > 1, "canvas placed in front of XR camera");
            yield return Until(() => completions > previousCompletions, "results after credits");
            Check(Time.realtimeSinceStartupAsDouble - visibleStart >= 5.55, "five full seconds plus reveal and fade out");
            Check(completions == previousCompletions + 1 && finalScore == 123, "one completion preserves score");
            Check(resultsPanel.activeSelf, "existing results screen displayed");
            Check(!SceneManager.GetSceneByName("CreditScene").isLoaded, "credits unloaded before results");
            yield return BackToMenu();
        }

        foreach (bool duringCredits in new[] { false, true })
        {
            yield return Begin(true);
            int previous = completions;
            FinishSong();
            if (duringCredits) yield return Until(() => SceneManager.GetActiveScene().name == "CreditScene", "cancel during credits");
            else yield return Until(() => Time.timeScale == 0, "cancel during fade out");
            yield return BackToMenu();
            Check(completions == previous, "cancel never submits results");
        }

        var creditsDefinition = experience.CreditsScene;
        CreditsProbeSetup.Set(experience, "creditsScene", null);
        yield return Begin(false);
        int before = completions;
        FinishSong();
        yield return Until(() => completions > before, "legacy experience without credits");
        Check(!SceneManager.GetSceneByName("CreditScene").isLoaded && resultsPanel.activeSelf, "legacy direct results still work");
        yield return BackToMenu();
        CreditsProbeSetup.Set(experience, "creditsScene", creditsDefinition);
        Debug.Log($"CREDITS PROBE PASSED: {checks} checks");
        EditorApplication.Exit(0);
    }
}
#endif
