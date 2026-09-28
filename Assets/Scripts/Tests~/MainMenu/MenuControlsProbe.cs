#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Reflection;

public sealed class MenuControlsProbe : MonoBehaviour
{
    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private int checks;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("MENUPROBE: " + message);
        checks++;
    }
    private IEnumerator Start()
    {
        var routine = Run();
        while (true)
        {
            object next;
            try { if (!routine.MoveNext()) break; next = routine.Current; }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); yield break; }
            yield return next;
        }
        Debug.Log($"MENUPROBE PASSED: {checks} checks, drag, sticks, model visibility, pause, scene changes and volume.");
        EditorApplication.Exit(0);
    }
    private IEnumerator Run()
    {
        var eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        var canvas = new GameObject("Canvas", typeof(RectTransform)).AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        viewport.SetParent(canvas.transform, false);
        viewport.sizeDelta = new Vector2(300, 200);
        var scroll = viewport.gameObject.AddComponent<XRDropdownScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.inertia = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.viewport = viewport;
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1); content.sizeDelta = new Vector2(0, 700);
        scroll.content = content;
        var template = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Toggle), typeof(EventTrigger));
        template.transform.SetParent(content, false);
        template.SetActive(false);
        template.AddComponent<ScrollRectItemDragRelay>();
        // Match TMP_Dropdown.CreateItem/AddItem: active clone first, parent second.
        template.SetActive(true);
        var item = Instantiate(template);
        item.transform.SetParent(content, false);
        template.SetActive(false);
        var toggle = item.GetComponent<Toggle>();
        toggle.isOn = false;
        int selections = 0;
        toggle.onValueChanged.AddListener(_ => selections++);
        Canvas.ForceUpdateCanvases();
        yield return null;
        scroll.verticalNormalizedPosition = 1;
        var pointer = new PointerEventData(eventSystem) { pointerId = 20, button = PointerEventData.InputButton.Left,
            position = new Vector2(100, 100), pressPosition = new Vector2(100, 100), eligibleForClick = true };
        var input = eventSystem.gameObject.AddComponent<MenuProbeInputModule>();
        pointer.pointerPress = item;
        pointer.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(item);
        ExecuteEvents.Execute(item, pointer, ExecuteEvents.initializePotentialDrag);
        Check(pointer.pointerDrag == scroll.gameObject, "press transfers drag ownership to the scroll view");
        Check(pointer.eligibleForClick, "press without movement can select");
        float startY = content.anchoredPosition.y;
        pointer.position += new Vector2(0, 80);
        pointer.delta = new Vector2(0, 80);
        input.Drag(pointer);
        Check(pointer.dragging && !pointer.eligibleForClick && pointer.pointerPress == null,
            "input module cancels option selection when held-trigger drag begins");
        Check(content.anchoredPosition.y > startY + 50, "first drag includes motion from original press");
        float beforeStick = content.anchoredPosition.y;
        scroll.ScrollWithThumbstick(-1, 0.2f);
        Check(Mathf.Approximately(beforeStick, content.anchoredPosition.y), "stick cannot fight held-trigger drag");
        pointer.position += new Vector2(0, 40);
        pointer.delta = new Vector2(0, 40);
        input.Drag(pointer);
        Check(content.anchoredPosition.y > beforeStick + 30, "holding continues scrolling on following frames");
        if (pointer.eligibleForClick) ExecuteEvents.Execute(item, pointer, ExecuteEvents.pointerClickHandler);
        ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
        Check(selections == 0, "release after drag does not select or close dropdown");
        pointer.eligibleForClick = true;
        ExecuteEvents.Execute(item, pointer, ExecuteEvents.pointerClickHandler);
        Check(selections == 1, "ordinary click still selects");
        scroll.verticalNormalizedPosition = 1;
        scroll.ScrollWithThumbstick(-1, 0.25f);
        float down = scroll.verticalNormalizedPosition;
        Check(down < 1, "stick down scrolls down");
        scroll.ScrollWithThumbstick(1, 0.1f);
        Check(scroll.verticalNormalizedPosition > down, "stick up scrolls up");
        float deadzoneStart = scroll.verticalNormalizedPosition;
        scroll.ScrollWithThumbstick(0.1f, 1);
        Check(Mathf.Approximately(deadzoneStart, scroll.verticalNormalizedPosition), "stick drift ignored");
        scroll.ScrollWithThumbstick(-1, 100);
        Check(Mathf.Approximately(scroll.verticalNormalizedPosition, 0), "clamped at list bottom");
        scroll.ScrollWithThumbstick(1, 100);
        Check(Mathf.Approximately(scroll.verticalNormalizedPosition, 1), "clamped at list top");
        scroll.gameObject.SetActive(false);
        scroll.ScrollWithThumbstick(-1, 1);
        scroll.gameObject.SetActive(true);
        Check(Mathf.Approximately(scroll.verticalNormalizedPosition, 1), "closed list ignores sticks");
        pointer.scrollDelta = new Vector2(0, -10);
        ExecuteEvents.Execute(item, pointer, ExecuteEvents.scrollHandler);
        Check(content.anchoredPosition.y > 0, "mouse wheel still forwarded");
        var originalScene = SceneManager.GetActiveScene();
        var menuScene = SceneManager.CreateScene("MainMenu");
        var gameplayScene = SceneManager.CreateScene("ProbeGameplay");
        SceneManager.SetActiveScene(gameplayScene);
        var pauseChannel = ScriptableObject.CreateInstance<BoolEventChannelSO>();
        var pauseObject = new GameObject("PauseController");
        pauseObject.SetActive(false);
        var pause = pauseObject.AddComponent<PauseMenuController>();
        var panel = new GameObject("PausePanel");
        var actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
        var action = actionAsset.AddActionMap("Probe").AddAction("ProbePause", InputActionType.Button);
        var actionReference = InputActionReference.Create(action);
        SetField(pause, "_pausePanel", panel);
        SetField(pause, "_pauseAction", actionReference);
        SetField(pause, "_gameplayPauseChanged", pauseChannel);
        pauseObject.SetActive(true);
        var rig = new GameObject("Rig");
        rig.SetActive(false);
        var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.SetParent(rig.transform);
        var renderer = model.GetComponent<Renderer>();
        var visibility = rig.AddComponent<XRControllerModelVisibility>();
        SetField(visibility, "controllerVisuals", new Transform[] { model.transform });
        SetField(visibility, "gameplayPauseChanged", pauseChannel);
        rig.SetActive(true);
        Check(renderer.forceRenderingOff && renderer.enabled, "gameplay hides model without disabling controller");
        pauseChannel.RaiseEvent(true);
        Check(renderer.forceRenderingOff, "internal gameplay pause does not show controller model");
        pause.Pause();
        Check(!renderer.forceRenderingOff && pause.IsPaused, "opening PauseMenu shows model");
        pause.Resume();
        Check(renderer.forceRenderingOff && !pause.IsPaused, "resuming hides model");
        SceneManager.SetActiveScene(menuScene);
        Check(!renderer.forceRenderingOff, "entering MainMenu shows model");
        SceneManager.SetActiveScene(gameplayScene);
        Check(renderer.forceRenderingOff, "leaving MainMenu hides model again");
        pause.Pause();
        pauseObject.SetActive(false);
        Check(renderer.forceRenderingOff, "disabling pause controller clears visible model");
        visibility.enabled = false;
        Check(!renderer.forceRenderingOff, "disabling visibility component restores renderer state");
        Destroy(rig); Destroy(pauseObject); Destroy(panel); Destroy(actionReference); Destroy(pauseChannel);
        Destroy(actionAsset);
        SceneManager.SetActiveScene(originalScene);
        yield return SceneManager.UnloadSceneAsync(menuScene);
        yield return SceneManager.UnloadSceneAsync(gameplayScene);
        float originalVolume = AudioListener.volume;
        AudioListener.volume = 0.37f;
        var volumeObject = new GameObject("MinMaxSlider", typeof(RectTransform));
        volumeObject.SetActive(false);
        var slider = volumeObject.AddComponent<Slider>();
        slider.value = 0.8f;
        int notifications = 0;
        slider.onValueChanged.AddListener(_ => notifications++);
        volumeObject.AddComponent<HeadsetVolumeSlider>();
        volumeObject.SetActive(true);
        Check(Mathf.Approximately(slider.value, 0.37f) && Mathf.Approximately(AudioListener.volume, 0.37f), "menu reads actual volume without overwriting it");
        Check(notifications == 0, "initial synchronization does not emit change events");
        slider.value = 0.64f;
        Check(Mathf.Approximately(AudioListener.volume, 0.64f) && notifications == 1, "slider adjusts desktop app volume once");
        slider.value = 0;
        Check(AudioListener.volume == 0, "mute at minimum");
        slider.value = 1;
        Check(AudioListener.volume == 1, "maximum volume");
        volumeObject.SetActive(false);
        AudioListener.volume = 0.23f;
        volumeObject.SetActive(true);
        Check(Mathf.Approximately(slider.value, 0.23f), "reopening reads external volume change");
        slider.value = 0.5f;
        Check(notifications == 4 && AudioListener.volume == 0.5f, "reopening does not duplicate listeners");
        AudioListener.volume = 0.7f;
        yield return new WaitForSecondsRealtime(0.6f);
        Check(Mathf.Approximately(slider.value, 0.7f) && notifications == 4, "external volume polling updates UI without feedback loop");
        Destroy(volumeObject);
        yield return null;
        Check(Mathf.Approximately(AudioListener.volume, 0.7f), "leaving menu preserves volume");
        AudioListener.volume = originalVolume;
    }
}
public sealed class MenuProbeInputModule : PointerInputModule
{
    public override void Process() { }
    public void Drag(PointerEventData data) => ProcessDrag(data);
}
#endif
