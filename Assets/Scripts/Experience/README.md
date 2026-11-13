# ¿Qué es `ExperienceCore`?

`ExperienceCore` es la escena de servicios compartidos de una experiencia. `SceneFlowManager` la carga de forma aditiva antes de preparar la escena de juego o la secuencia completa. Allí viven los sistemas que necesitan un reloj musical y los componentes globales que coordinan el inicio, la reproducción y el cierre.

La escena permanece cargada mientras se ejecuta la experiencia, aunque Unity cambie la escena activa entre segmentos de gameplay. Al regresar al menú, `SceneFlowManager` apaga la secuencia y descarga `ExperienceCore` junto con las escenas de juego.

## Responsabilidades musicales

En `ExperienceCore`, los componentes musicales trabajan sobre el mismo `PlayableDirector`:

- **`PlayableDirector`** reproduce el Timeline de la canción.
- **`SequenceDirector`** escucha el canal `ExperienceReady`, reinicia el Timeline a cero y lo reproduce una vez. Al alcanzar el final real de audio, pausa el Timeline y publica `SongFinished`; si ese canal no está asignado, publica `ExperienceCompleted` directamente.
- **`ExperienceMusicClock`** expone el tiempo y el estado de reproducción del `PlayableDirector`. `FullExperienceDirector` lo consulta para decidir qué segmento debe estar activo.
- **`ExperienceBeatPlayer`**, cuando se usa, publica los beats del `BeatMapSO` usando el mismo reloj musical.

Así los límites de escenas, beats y sistemas de juego pueden consultar una única referencia temporal.

## Flujo de carga y ejecución

1. `SceneFlowManager` carga `LoadingScene` y después `ExperienceCore`.
2. Carga y prepara una escena individual o los segmentos iniciales de la secuencia completa.
3. Cuando la preparación termina, `AppStateMachine` publica `ExperienceReady`.
4. `SequenceDirector` inicia el Timeline. En una secuencia completa, `FullExperienceDirector` también empieza a seguir el reloj y activa el segmento correspondiente.
5. Al terminar la canción, se publica el cierre. Si la experiencia tiene créditos, `SceneFlowManager` los muestra antes de publicar `ExperienceCompleted`.
6. Al volver al menú, `SceneFlowManager` detiene los sistemas y descarga `ExperienceCore`.

En modo individual, la escena de gameplay preparada también escucha `ExperienceReady` mediante su `ExperienceSceneBootstrap`. En modo completo, el director global usa el tiempo de `ExperienceCore` para activar cada escena de segmento en su frontera musical.

## Referencias que deben coincidir

- El nombre de la escena configurado en `SceneFlowManager` debe coincidir con el archivo de escena y con su inclusión en Build Settings. El valor inicial del campo es `ExperienceCore`.
- `SequenceDirector` debe tener asignado el `PlayableDirector`, el canal `ExperienceReady` y los canales de cierre que se utilicen.
- `ExperienceMusicClock` debe referenciar el mismo `PlayableDirector` que controla la canción.
- Si se usa `FullExperienceDirector`, debe existir un `ExperienceMusicClock` accesible cuando se prepara la secuencia; su código falla la preparación si no lo encuentra.
- Si se usa `ExperienceBeatPlayer`, asigna un `BeatMapSO` con beats. Puedes asignar el reloj directamente o dejar que el componente lo busque al iniciar.
- Los canales ScriptableObject que conectan publicadores y receptores deben ser las mismas referencias en los componentes correspondientes.

## Diagnóstico rápido

- **La secuencia completa no se prepara:** verifica que `ExperienceCore` esté cargada, que haya un `ExperienceMusicClock` y que este apunte al Timeline correcto.
- **La canción no inicia:** revisa que `ExperienceReady` esté asignado en `SequenceDirector` y que el Timeline tenga duración finita mayor que cero.
- **Los beats no aparecen:** revisa el `BeatMapSO`, el reloj y el valor de `Max Beat Lateness` en `ExperienceBeatPlayer`.
- **Una escena individual funciona sola pero no desde el menú:** iníciala mediante el flujo de Bootstrap para que se carguen `ExperienceCore` y los sistemas compartidos.

## Scripts relacionados

- `SceneManagement/SceneFlowManager.cs`
- `Application/AppStateMachine.cs`
- `Events/SequenceDirector.cs`
- `Experience/FullExperienceDirector.cs`
- `Beat System/ExperienceMusicClock.cs`
- `Beat System/ExperienceBeatPlayer.cs`
- `Experience/ExperienceSceneBoostrap.cs` (la clase se llama `ExperienceSceneBootstrap`)