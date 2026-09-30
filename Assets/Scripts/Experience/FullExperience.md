# Configuración de Full Experience

En `Assets/ScriptableObjects/Experiences/EXP_Punko.asset`, **Full Sequence** selecciona la secuencia. El modo individual sigue usando la lista **Scenes** y `ExperienceReady`.

`EXP_Punko` usa `SEQ_Punko_Final`: Alex 0–31.16, Joaquin 31.16–62.33, Shipi 62.33–93.50, JuanAndres 93.50–124.66, Danniel 124.66–155.83 y Jeremy 155.83–187 segundos. `SEQ_Punko_Test` conserva la secuencia anterior de cuatro escenas para pruebas.

Cada segmento define **Scene**, **Start Time**, **End Time**, **Transition** y **Preload Before Playback**. Los intervalos deben empezar en cero y ser consecutivos. La transición corresponde a la entrada al segmento. `CameraFade` configura el color y la duración del fade de cámara.

**Preload Before Playback** está activado para Danniel en ambas secuencias. Durante `Loading` se cargan y preparan los primeros dos segmentos más los marcados. La canción comienza después. El contenido de Danniel permanece inactivo hasta su turno y se descarga después de terminar su segmento. Esto implica tres escenas de gameplay residentes al principio de la prueba: Alex, Joaquin y Danniel; solo una ejecuta gameplay. Las demás escenas siguen usando la precarga de la siguiente con **Preload Lead Seconds**. Marcar más escenas aumenta memoria y tiempo de carga inicial.

El `ExperienceSceneBootstrap` de cada escena debe estar fuera de **Gameplay Root**, y este root debe estar guardado inactivo. **Preloaders** y **Runtime Systems** mantienen su orden explícito; el bootstrap también incluye pools y armas que implementen las interfaces dentro del contenido. La precarga prepara pools sin activar ese contenido. `ExperienceSceneActivation` activa/desactiva una escena específica; no vuelve a publicar `ExperienceReady` ni reinicia la canción o el score.

Los pools de balas implementan `IExperienceRuntime`: `EndExperience` cancela sus proyectiles antes de desactivar el contenido. Esa cancelación no publica `Despawned`, para evitar contabilizar las balas pendientes como fallos. `OnDisable` también limpia las balas en una desactivación directa, sin cambiar su padre durante el cambio de estado de la jerarquía.

La configuración **Release Unused Assets** libera recursos después de descargar escenas. Medir el rendimiento y la memoria en Quest: la precarga reduce el trabajo cercano al cambio, pero la activación de renderers, animadores y shaders también puede tener coste.

Pruebas de regresión: `Assets/Scripts/Tests~/FullExperience`. Se ejecutan en un proyecto temporal separado; `FullExperienceProbeSetup.Start` construye escenas mínimas y un Timeline de audio real, luego prueba precarga inactiva, límites temporales, pausas, cancelación, modo individual y limpieza de balas.

Shipi y JuanAndres usan sus objetos ExperienceContent existentes como Gameplay Root, guardados inactivos. Sus bootstraps están conectados a ExperienceSceneActivation y ExperienceReady, para admitir Full Experience y el modo individual. Ambas escenas están habilitadas en Build Settings.

## Créditos al finalizar

`EXP_Punko` configura **Credits Scene = SCN_Credits**, **Credits Transition = CameraFade** y **Credits Visible Seconds = 5**. Aplica tanto a Full Experience como a cualquier escena individual de esa experiencia. `SCN_Credits.SceneName` es `CreditScene`, incluida en Build Settings. No se añade como segmento musical ni como opción de juego individual.

Al alcanzar el final real del audio, `SequenceDirector` pausa el Timeline y publica `SongFinished` una sola vez. `ScoreSystem` deja de aceptar interacciones/bonos y la UI oculta el HUD. `SceneFlowManager` congela el gameplay, ejecuta el fade, termina y descarga las escenas de juego y carga los créditos de forma aditiva. Después del fade de entrada cuenta cinco segundos completos con tiempo real, hace fade de salida y descarga los créditos. Conserva `ExperienceCore` y publica el `ExperienceCompleted` existente para guardar el puntaje y mostrar resultados; finalmente revela esos resultados.

Los fades usan el SO configurado y los créditos usan tiempo real porque la canción ya terminó y el gameplay está pausado. La salida al menú cancela el cierre, espera las operaciones de carga en curso y descarga los créditos antes de restaurar el menú. Una definición sin Credits Scene conserva el cierre directo a resultados. El Canvas de créditos conserva su pertenencia a CreditScene y `VRLoadingCanvasBinder` lo coloca frente a la cámara XR sin trasladarlo a la jerarquía persistente.

Regresión del cierre: `Assets/Scripts/Tests~/Credits`, con pruebas en Unity aislado de Full, individual, duración visible, puntaje congelado, eventos duplicados, cancelación y configuración sin créditos.
