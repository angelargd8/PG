# Regresion: precarga de Shipi antes de Awake

Ejecutar `ShipiPreloadProbe.Run` con Unity 6000.3.9f1, `-batchmode -nographics`, en un proyecto temporal separado. Copiar estos scripts a `Assets/Runtime` del proyecto temporal (el runner no debe estar en una carpeta Editor):

- `ShipiPreloadProbe.cs`
- `ShipiFood`, `ShipiFoodPool`, `ShipiFoodDefinitionSO`, `ShipiMovePoint`
- `ShipiCutDirection`, `DifficultyLevel`
- `ExperienceSceneBootstrap` (archivo `ExperienceSceneBoostrap.cs`), `ExperiencePreloadOperation`
- `IExperiencePreloadable`, `IExperienceRuntime`
- `VoidEventChannelSO`, `ExperienceSceneActivationEventChannelSO`

Usar el ProjectVersion del proyecto principal y sus dependencias `com.unity.modules.*`. No requiere las escenas, prefabs, audio ni paquetes XR del proyecto principal.

El fixture reproduce la jerarquia real de precarga: root inactivo, pool inactivo, seis definiciones y comidas cuyos Awake aun no se han ejecutado. Usa el bootstrap y el adaptador de corutinas reales. Verifica precarga sin activar gameplay, referencias de hijos inactivos, precarga repetida, activacion y 60 ciclos de corte/devolucion/reutilizacion.

Con la version anterior de ShipiFood y el argumento adicional `-shipiExpectFailure`, comprueba que se reproduce el NullReferenceException en `ShipiFood.SetWholeVisualEnabled` desde `ResetFood` y `ShipiFoodPool.Preload`. Con la correccion, sin ese argumento, debe terminar con `SHIPI PRELOAD PROBE PASSED` y codigo de salida 0.
