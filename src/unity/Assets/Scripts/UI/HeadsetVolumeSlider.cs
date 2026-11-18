using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public sealed class HeadsetVolumeSlider : MonoBehaviour
{
    private Slider slider;
    private float nextRefresh;
#if UNITY_ANDROID && !UNITY_EDITOR
    private const int MusicStream = 3; // android.media.AudioManager.STREAM_MUSIC
    private AndroidJavaObject deviceAudio;
    private int maxVolume;
#endif

    private void OnEnable()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.wholeNumbers = false;
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                deviceAudio = activity.Call<AndroidJavaObject>("getSystemService", "audio");
            if (deviceAudio == null || deviceAudio.Call<bool>("isVolumeFixed"))
                throw new InvalidOperationException("El dispositivo no permite ajustar su volumen multimedia.");
            maxVolume = deviceAudio.Call<int>("getStreamMaxVolume", MusicStream);
            if (maxVolume <= 0) throw new InvalidOperationException("El rango de volumen del dispositivo no es valido.");
#endif
            slider.interactable = true;
            RefreshVolume();
            slider.onValueChanged.AddListener(SetVolume);
        }
        catch (Exception error) { DisableControl(error); }
    }

    private void SetVolume(float value)
    {
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            int index = Mathf.RoundToInt(Mathf.Clamp01(value) * maxVolume);
            if (index != deviceAudio.Call<int>("getStreamVolume", MusicStream))
                deviceAudio.Call("setStreamVolume", MusicStream, index, 0);
#else
            // Editor / desktop / Quest Link: this controls the app, not Windows system volume.
            AudioListener.volume = Mathf.Clamp01(value);
#endif
            RefreshVolume();
        }
        catch (Exception error) { DisableControl(error); }
    }

    private void RefreshVolume()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        float volume = deviceAudio.Call<int>("getStreamVolume", MusicStream) / (float)maxVolume;
#else
        float volume = AudioListener.volume;
#endif
        // Opening the menu reads the device; it must never overwrite its current volume.
        slider.SetValueWithoutNotify(Mathf.Clamp01(volume));
        nextRefresh = Time.unscaledTime + 0.5f;
    }

    private void Update()
    {
        if (!slider.interactable || Time.unscaledTime < nextRefresh) return;
        try { RefreshVolume(); } // Also reflect the headset's physical volume buttons.
        catch (Exception error) { DisableControl(error); }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused && isActiveAndEnabled && slider != null && slider.interactable)
        {
            try { RefreshVolume(); }
            catch (Exception error) { DisableControl(error); }
        }
    }

    private void DisableControl(Exception error)
    {
        slider.interactable = false;
        slider.onValueChanged.RemoveListener(SetVolume);
        Debug.LogWarning($"[HeadsetVolumeSlider] {error.Message}", this);
    }

    private void OnDisable()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(SetVolume);
#if UNITY_ANDROID && !UNITY_EDITOR
        deviceAudio?.Dispose();
        deviceAudio = null;
#endif
    }
}
