# Ausencia de controles y anclajes

Ejecutar `ControllerAvailabilityProbe.Run` en un proyecto Unity 6000.3.9f1 temporal, con `-batchmode -nographics`. Copiar a una carpeta Runtime (no Editor):

- `ControllerAvailabilityProbe.cs`
- `SceneWeaponEquipController`, `SceneWeaponFollower`, `RightWeaponAnchor`
- `JeremySwordAttachement.cs`, `XRHapticFeedback`
- `IExperienceRuntime`, `VoidEventChannelSO`, `BoolEventChannel.cs`

Solo necesita los modulos de Unity utilizados por esas clases. Verificar `CONTROLLER PROBE PASSED` y salida 0.

La prueba cubre armas de ambas manos sin anclajes, reintentos sin repetir avisos, aparicion/desactivacion/reactivacion de anclajes, cancelacion al terminar y llamadas hapticas sin dispositivos. No reproduce desconexiones fisicas de Quest ni demuestra la causa de un cierre nativo del runtime XR.
