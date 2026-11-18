# Diagnósticos de flujo para Android / Meta Quest

`ExperienceDiagnostics` agrega registros de diagnóstico con el prefijo estable `[PGDIAG]`. Está pensado para reconstruir una ejecución desde Logcat y funciona también en compilaciones Release. No crea eventos de negocio: observa los canales, eventos C# y operaciones que ya usa la aplicación.

## Niveles

El nivel se lee al iniciar el proceso desde `Application.persistentDataPath/experience-diagnostics.txt`. El archivo debe contener una sola palabra en UTF-8: `off`, `flow` o `detailed`. Si no existe o su contenido no es válido, el nivel queda en `off` y no se escriben mensajes `[PGDIAG]`.

| Nivel | Qué registra |
|---|---|
| `off` | No emite los mensajes añadidos por este sistema. Es el valor predeterminado. |
| `flow` | Solicitudes, validaciones, preparación, reproducción, cambios de segmento, operaciones de escena, cierre y resúmenes de beats, interacciones y cambios de puntuación frecuentes. |
| `detailed` | Lo anterior, más publicaciones, receptores, retorno de cada manejador y detalle individual de beats y acciones de sus consumidores. Puede generar muchos mensajes. |

También se puede cambiar desde código en ejecución con `ExperienceDiagnostics.SetLevel(ExperienceDiagnosticLevel.Flow)` (o `Off` / `Detailed`). El método guarda el nivel en el mismo archivo; no depende de `Debug.isDebugBuild`.

## Habilitar el nivel en una APK instalada

Estos comandos son para **PowerShell en Windows**. Sustituye el valor de `$package` por el identificador de paquete de la APK instalada. Puedes localizar candidatos de aplicaciones instaladas por el usuario con `adb shell pm list packages -3`; si la aplicación ya está abierta, la cabecera `[PGDIAG][HEADER]` incluye `app_id` y `config_path` (los valores se escriben codificados para conservar una sola línea).

```powershell
$package = "com.ejemplo.aplicacion"
Set-Content -NoNewline -Encoding ascii .\experience-diagnostics.txt "flow"
adb shell mkdir -p "/sdcard/Android/data/$package/files"
adb push .\experience-diagnostics.txt "/sdcard/Android/data/$package/files/experience-diagnostics.txt"
adb shell am force-stop $package
adb shell monkey -p $package 1
```

Usa `detailed` en el archivo para obtener el detalle individual. Para volver a desactivar los mensajes, escribe `off`, vuelve a copiar el archivo y reinicia el proceso. La ruta habitual de `persistentDataPath` en Android es `/storage/emulated/0/Android/data/<paquete>/files`; el valor efectivo aparece en la cabecera cuando el nivel está activo.

## Capturar una ejecución completa

Abre una primera ventana de **PowerShell** y comienza la captura antes de abrir la aplicación:

```powershell
adb logcat -c
adb logcat -v threadtime | Tee-Object -FilePath .\quest-run-full.log
```

En una segunda ventana de **PowerShell**, inicia la aplicación:

```powershell
$package = "com.ejemplo.aplicacion"
adb shell monkey -p $package 1
```

Realiza la experiencia hasta regresar al menú. Luego detén la captura de la primera ventana con `Ctrl+C`. El archivo completo conserva el contexto previo al inicio y cualquier registro de Unity o del sistema.

Para capturar solo el proceso una vez iniciado, calcula el PID cada vez (cambia al reiniciar la aplicación) y filtra por el prefijo:

```powershell
$package = "com.ejemplo.aplicacion"
$appPid = (adb shell pidof -s $package).Trim()
if (-not $appPid) { throw "No se encontró el proceso; inicia la aplicación y vuelve a consultar el PID." }
adb logcat --pid=$appPid -v threadtime | Select-String -Pattern '\[PGDIAG\]' | Tee-Object -FilePath .\quest-run-pgdiag.log
```

Para filtrar después el archivo completo:

```powershell
Select-String -Path .\quest-run-full.log -Pattern '\[PGDIAG\]' | Set-Content -Encoding utf8 .\quest-run-pgdiag.log
```

Los registros usan el prefijo `[PGDIAG]` como filtro de contenido; no dependen de un tag Android fijo. `record_seq` identifica cada línea, `event_id` relaciona publicación y receptores, y `operation_id` relaciona el inicio y cierre de una operación. `mono_s` es tiempo monotónico del proceso; `musical_time_s` se registra aparte y figura como `unavailable` antes de registrar el reloj musical.

## Cobertura instrumentada

| Flujo | Publicador / inicio | Receptor / resultado observado |
|---|---|---|
| Solicitud y validación | `ExperienceLaunchButton` publica `ExperienceRequested` | `AppStateMachine` registra recepción, rechazo o transición iniciada |
| Carga y preparación | `SceneFlowManager` y `ExperienceSceneBootstrap`; `FullExperienceDirector` en secuencia completa | Inicio/fin de carga, preparación, precargadores, activación del root, `BeginExperience` / `EndExperience`, descarga e interrupciones |
| Inicio de reproducción | `AppStateMachine` publica `ExperienceReady` tras la preparación | `SequenceDirector` registra recepción, solicitud de `PlayableDirector.Play()` y estado observado después de solicitarla |
| Segmentos y escenas | `FullExperienceDirector` selecciona por tiempo musical y publica `ExperienceSceneActivation` | Bootstrap de cada segmento registra activación/desactivación y cambios de estado |
| Beats y efectos | `ExperienceBeatPlayer` publica `BeatReached` / `PlaybackReset` con la tolerancia configurada | `AlexThrowDirector`, `EnemyWaveSpawner`, `BeatLightController` y `PlayerBeatVFXController` registran recepción, acción o motivo de descarte en detalle |
| Interacción y puntuación | Controladores de interacción, `ScoreSystem`, perfiles y sistemas de resultado publican en sus canales | Métricas, dificultad, estado del jugador, HUD, resultado e historial registran los eventos instrumentados; los eventos frecuentes se resumen en `flow` |
| Cierre y retorno | `SequenceDirector` informa fin de canción; `SceneFlowManager` gestiona créditos y limpieza | `ScoreSystem`, resultados y `AppStateMachine` registran cierre, retorno al menú y fin de ejecución |

En `detailed`, `receive` indica la entrada al manejador; `handler_returned` indica que ese manejador regresó. `handler_dispatch_returned` se escribe cuando regresó la distribución completa. Una excepción queda registrada y se vuelve a lanzar. Los registros de beats indican retraso de evaluación/publicación del evento; no miden latencia audiovisual percibida. La aplicación no toma muestras ni descarta voluntariamente estos registros, pero `Debug.Log` y el búfer de Logcat no ofrecen aquí un contador fiable de mensajes perdidos por saturación; conserva la captura completa y usa `flow` si el volumen detallado dificulta la lectura.

## Revisión de documentación y límites de validación

La arquitectura descrita por los README coincide en general con el código: los canales ScriptableObject conectan emisores y receptores, `SceneFlowManager` coordina la carga y `FullExperienceDirector` elige segmentos usando el reloj musical. La implementación no tiene un gestor central que distribuya todos los eventos. `ExperienceCore` aloja servicios compartidos; `SequenceDirector` inicia el Timeline y `FullExperienceDirector` coordina segmentos.

El código actual confirma dos precisiones del flujo: quien publica `ExperienceReady` después de recibir la preparación satisfactoria es `AppStateMachine`; y el valor de tolerancia de `ExperienceBeatPlayer` es configurable, por lo que los registros muestran el valor real del componente en lugar de asumir el valor predeterminado mencionado en el README de beats.

La instrumentación permite comprobar el flujo cuando se ejecute en Unity o Quest. No constituye por sí misma evidencia de una ejecución ni mide rendimiento, memoria o latencia audiovisual. Para el informe experimental, conserva el archivo Logcat de una ejecución individual, otra completa y una cancelación durante carga/preparación, si están disponibles.
