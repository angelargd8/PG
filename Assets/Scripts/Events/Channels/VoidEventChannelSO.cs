using UnityEngine;
using System;

/* Basic Event Channel 
 
This script is based on the unitys doc
link: https://unity.com/how-to/scriptableobjects-event-channels-game-code#creating-the-event-channel-assets
 */


[CreateAssetMenu(
    fileName = "VoidEventChannelSO", 
    menuName = "Scriptable Objects/Events/Void Event Channel")]

public sealed class VoidEventChannelSO : ScriptableObject
{

    // evento al que se suscriben los consumidores
    public event Action Raised;


    // publica el evento y notifica a todos los consumidores
    public void RaiseEvent()
    {
        Raised?.Invoke();
    }
}
