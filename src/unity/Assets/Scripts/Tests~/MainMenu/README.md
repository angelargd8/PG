# MainMenu: arrastre y volumen

`MenuControlsProbeSetup.Start` ejecuta las pruebas en un proyecto Unity temporal. Copiar `MenuControlsProbe.cs`, `ScrollRectItemDragRelay.cs` y `HeadsetVolumeSlider.cs` a sus scripts de runtime, y `MenuControlsProbeSetup.cs` a `Editor`. Requiere UGUI y Input System. Incluir también XRDropdownScrollRect, XRControllerModelVisibility, PauseMenuController, PauseMenuFollower y los canales Bool/Void. El probe usa escenas temporales para comprobar MainMenu, gameplay y PauseMenu. No ejecutar el setup en el proyecto principal: crea una escena de prueba.

Verifica eventos reales de UGUI sobre una opción con `Toggle` y `EventTrigger`: arrastrar desplaza el `ScrollRect`, soltar un arrastre no selecciona la opción, pulsar normalmente sí selecciona, y el scroll se propaga. También prueba lectura inicial del volumen, cambios del slider, límites, reapertura y sincronización sin duplicar listeners.

En el APK de Quest, `HeadsetVolumeSlider` usa el volumen multimedia del dispositivo mediante [Android AudioManager](https://developer.android.com/reference/android/media/AudioManager#setStreamVolume(int,%20int,%20int)). Lee el valor al abrir el panel y cada medio segundo para reflejar los botones físicos. No aplica un volumen inicial propio. El postprocesador de Android añade `MODIFY_AUDIO_SETTINGS` al manifest generado, conservando las otras entradas. Si el dispositivo rechaza el control, se deshabilita el slider y se registra la causa.

En Editor, Windows y Quest Link cambia `AudioListener.volume` (volumen de la aplicación). No cambia el volumen del sistema operativo del PC ni el del visor conectado por Link. El nivel se conserva al cambiar de escena.

Validación pendiente en hardware: arrastrar con el rayo del controlador, comprobar el volumen nativo en Quest standalone y comprobar que sus botones físicos actualicen el slider. Las pruebas aisladas no simulan el audio nativo Android ni el tracking de un visor.

El scroll de startScene usa XRDropdownScrollRect en Template: Trigger sostenido y movimiento del rayo arrastra las opciones; un clic sin arrastre selecciona. Con la lista abierta, arriba/abajo de cualquiera de los sticks desplaza la lista, con zona muerta y velocidad ajustables. Se prioriza el arrastre sobre el stick y se evita aplicar dos veces el scroll que también envía XRI. Bootstrap desconecta Teleport Mode y Teleport Mode Cancel de ambos ControllerInputActionManager, conservando los rayos de UI.

XRControllerModelVisibility oculta solamente los renderers de Left/Right Controller Visual. Los muestra cuando MainMenu es la escena activa o PauseMenuController.IsPaused es verdadero. Una pausa interna o la pantalla de resultados no muestra los modelos. La comprobación usa el canal GameplayPauseChanged existente y los cambios de escena activa.

Regresión de TMP: la prueba instancia la opción activa sin padre y después la coloca en Content, como hace TMP_Dropdown. ScrollRectItemDragRelay vuelve a resolver su ScrollRect en OnTransformParentChanged; buscarlo solamente en OnEnable dejaba sin referencia a las opciones generadas.
