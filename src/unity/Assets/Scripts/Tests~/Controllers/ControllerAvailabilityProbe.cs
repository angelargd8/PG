#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR;

[InitializeOnLoad]
public static class ControllerAvailabilityProbe
{
    static ControllerAvailabilityProbe() => EditorApplication.update += Tick;
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool("ControllerProbe.Pending", true);
        EditorApplication.EnterPlaymode();
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool("ControllerProbe.Pending", false)) return;
        SessionState.SetBool("ControllerProbe.Pending", false);
        try { Test(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    private static void Call(object target, string method) => target.GetType().GetMethod(method, Flags).Invoke(target, null);
    private static int checks;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }
    private static void Test()
    {
        int errors = 0, missingWarnings = 0;
        Application.LogCallback listener = (message, stack, type) =>
        {
            if (type == LogType.Exception || type == LogType.Error) errors++;
            if (type == LogType.Warning && message.Contains("no esta disponible")) missingWarnings++;
        };
        Application.logMessageReceived += listener;
        try
        {
            for (int hand = 0; hand < 2; hand++)
            {
                var go = new GameObject("Weapon", typeof(MeshRenderer));
                var follower = go.AddComponent<SceneWeaponFollower>();
                var equip = go.AddComponent<SceneWeaponEquipController>();
                Set(equip, "weaponFollower", follower);
                Set(equip, "hand", (SceneWeaponEquipController.Hand)hand);
                equip.BeginExperience();
                int warnings = missingWarnings;
                for (int i = 0; i < 50; i++) { Set(equip, "_nextAnchorAttempt", 0f); Call(equip, "Update"); }
                Check(missingWarnings == warnings, "missing anchor warnings must not repeat");
                Check(!go.GetComponent<Renderer>().enabled, "weapon hidden without anchor");
                var anchor = new GameObject(hand == 0 ? "RightWeaponAnchor" : "LeftWeaponAnchor");
                if (hand == 0) anchor.AddComponent<RightWeaponAnchor>();
                anchor.transform.position = new Vector3(2, 3, 4);
                Set(equip, "_nextAnchorAttempt", 0f); Call(equip, "Update");
                Check(go.GetComponent<Renderer>().enabled && go.transform.position == anchor.transform.position, "weapon reconnects");
                anchor.SetActive(false); Call(equip, "Update");
                Check(!go.GetComponent<Renderer>().enabled, "weapon hidden when hand disappears");
                anchor.SetActive(true); Set(equip, "_nextAnchorAttempt", 0f); Call(equip, "Update");
                Check(go.GetComponent<Renderer>().enabled, "weapon reconnects again");
                equip.EndExperience(); Call(equip, "Update");
                Check(!go.GetComponent<Renderer>().enabled, "ending cancels reconnect");
                UnityEngine.Object.DestroyImmediate(go);

                var swordGo = new GameObject("Sword", typeof(MeshRenderer));
                var sword = swordGo.AddComponent<JeremySwordAttachment>();
                Set(sword, "_hand", hand == 0 ? JeremySwordAttachment.Hand.Right : JeremySwordAttachment.Hand.Left);
                anchor.SetActive(false); sword.BeginExperience();
                Check(!swordGo.GetComponent<Renderer>().enabled, "sword hidden without anchor");
                anchor.SetActive(true); Set(sword, "_nextAnchorAttempt", 0f); Call(sword, "LateUpdate");
                Check(swordGo.GetComponent<Renderer>().enabled && swordGo.transform.position == anchor.transform.position, "sword reconnects");
                sword.EndExperience(); Call(sword, "LateUpdate");
                Check(!swordGo.GetComponent<Renderer>().enabled, "sword remains hidden after end");
                UnityEngine.Object.DestroyImmediate(swordGo);
                UnityEngine.Object.DestroyImmediate(anchor);
            }
            var hapticsGo = new GameObject("Haptics");
            var haptics = hapticsGo.AddComponent<XRHapticFeedback>();
            for (int i = 0; i < 50; i++)
            {
                haptics.Pulse(XRNode.LeftHand);
                haptics.Pulse(XRNode.RightHand);
                Set(haptics, "_nextConnectionCheck", 0f); Call(haptics, "Update");
            }
            Check(errors == 0, "missing controllers and anchors must not log errors or exceptions");
            UnityEngine.Object.DestroyImmediate(hapticsGo);
            Debug.Log($"CONTROLLER PROBE PASSED: {checks} checks");
        }
        finally { Application.logMessageReceived -= listener; }
    }
}
#endif
