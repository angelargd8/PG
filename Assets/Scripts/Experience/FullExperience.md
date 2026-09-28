# Configuración de Full Experience

En `Assets/ScriptableObjects/Experiences/EXP_Punko.asset`, **Full Sequence** selecciona la secuencia. El modo individual sigue usando la lista **Scenes** y `ExperienceReady`.

`SEQ_Punko_Test` usa Alex 0–31, Joaquin 31–125, Danniel 125–156 y Jeremy 156–187 segundos. `SEQ_Punko_Final` reserva también los intervalos de Shipi y JuanAndres; asignarla cuando sus escenas estén preparadas.

Cada segmento define **Scene**, **Start Time**, **End Time**, **Transition** y **Preload Before Playback**. Los intervalos deben empezar en cero y ser consecutivos. La transición corresponde a la entrada al segmento. `CameraFade` configura el color y la duración del fade de cámara.

**Preload Before Playback** está activado para Danniel en ambas secuencias. Durante `Loading` se cargan y preparan los primeros dos segmentos más los marcados. La canción comienza después. El contenido de Danniel permanece inactivo hasta su turno y se descarga después de terminar su segmento. Esto implica tres escenas de gameplay residentes al principio de la prueba: Alex, Joaquin y Danniel; solo una ejecuta gameplay. Las demás escenas siguen usando la precarga de la siguiente con **Preload Lead Seconds**. Marcar más escenas aumenta memoria y tiempo de carga inicial.

El `ExperienceSceneBootstrap` de cada escena debe estar fuera de **Gameplay Root**, y este root debe estar guardado inactivo. **Preloaders** y **Runtime Systems** mantienen su orden explícito; el bootstrap también incluye pools y armas que implementen las interfaces dentro del contenido. La precarga prepara pools sin activar ese contenido. `ExperienceSceneActivation` activa/desactiva una escena específica; no vuelve a publicar `ExperienceReady` ni reinicia la canción o el score.

Los pools de balas implementan `IExperienceRuntime`: `EndExperience` cancela sus proyectiles antes de desactivar el contenido. Esa cancelación no publica `Despawned`, para evitar contabilizar las balas pendientes como fallos. `OnDisable` también limpia las balas en una desactivación directa, sin cambiar su padre durante el cambio de estado de la jerarquía.

La configuración **Release Unused Assets** libera recursos después de descargar escenas. Medir el rendimiento y la memoria en Quest: la precarga reduce el trabajo cercano al cambio, pero la activación de renderers, animadores y shaders también puede tener coste.

Pruebas de regresión: `Assets/Scripts/Tests~/FullExperience`. Se ejecutan en un proyecto temporal separado; `FullExperienceProbeSetup.Start` construye escenas mínimas y un Timeline de audio real, luego prueba precarga inactiva, límites temporales, pausas, cancelación, modo individual y limpieza de balas.
