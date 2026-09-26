# Alex: objetos al ritmo

La escena `Assets/Scenes/Skeepers/Alex.unity` ya conecta estos componentes:

- `EnemyThrowPool`: `AlexThrowPool` con cuatro pools independientes (Dolars, TPumpkin A/F/G) y `AlexThrowDirector`.
- `EnemyAlex.prefab`: `AlexThrowHands`.
- Instancia `EnemyAlex` de la escena: `AlexWaypointMotion`, enlazado a los tres waypoints.
- Ambos warhammers de la escena: `AlexWarhammer`, con su mano correspondiente.
- `ExperienceSceneBootstrap`: precarga el pool e inicia el director.

Ejecutar desde el flujo normal de Bootstrap/menu para cargar ExperienceCore, el reloj musical, los eventos, el HUD y los mandos XR. Abrir Alex sola no carga esos sistemas.

## Reglas

| Objeto | Toque suave | Golpe al beat | Golpe fuera del beat |
| --- | --- | --- | --- |
| Dollar | Suma | Resta | Resta |
| Calabaza A/F/G | Suma, sin exigir beat | Suma y regresa hacia EnemyAlex | Resta |

El primer contacto resuelve el objeto. No vuelve a puntuar al tocar otro collider, el otro martillo ni al regresar al enemigo. Cualquier contacto resuelto vibra en el mando que lo hizo.

`AlexScoreProfile.asset` reutiliza el sistema de puntuacion de Jeremy con un perfil independiente: 100 puntos base por exito, -50 por fallo, multiplicador normal 1.25 y los mismos bonos de timing. Un toque suma 125 con esta dificultad. Un golpe correcto de calabaza suma ademas el bono de timing. Dejar pasar un objeto da 0 puntos; se puede cambiar `Missed Points` en el perfil. El sistema compartido mantiene el puntaje minimo en cero.

## Manos y trayectoria

Usar dos anchors hijos de los huesos permite ajustar el punto de salida y seguir la animacion. `AlexThrowHands` acepta anchors asignados; si estan vacios, crea `LeftThrowAnchor` y `RightThrowAnchor` al iniciar usando el rig humanoide o los nombres exactos `mixamorig:LeftHand` / `mixamorig:RightHand`. Los anchors creados en Play Mode no se guardan en el prefab. Para ajustes permanentes, crear esos hijos en el prefab y asignarlos, o editar los offsets locales del componente antes de jugar.

El director usa `ExperienceBeatPlayer` de ExperienceCore y su BeatMap actual. Por defecto lanza cada dos beats y llega dos beats despues; los golpes devueltos llegan al enemigo en un beat futuro. Rota Dollar/A/F/G y distribuye las salidas entre ambas manos. La trayectoria apunta a una posicion fija por lanzamiento, delante y por debajo de la camara XR. `Hit Target` permite asignar un centro de golpe explicito. `Return Target` permite cambiar el destino de las devoluciones.

## Ajustes y prueba en visor

### Recorrido y animaciones

El ciclo es `waypoint -> waypoint (1) -> waypoint -> waypoint (2) -> waypoint -> ...`.
Al llegar dentro de `Waypoint Distance` y terminar de frenar, espera `Wait Time At Waypoint` (4 segundos en la escena) antes de avanzar. Tambien espera al iniciar en el centro. Durante la espera mantiene idle, sigue mirando al jugador y lanza al beat. La pausa del juego congela el tiempo de espera restante. Un valor de 0 permite continuar sin pausa. Lanza al beat en cualquier estado, durante los desplazamientos y durante las transiciones.
El desplazamiento horizontal usa un Rigidbody cinematico interpolado en FixedUpdate, con aceleracion y frenado por distancia restante. El componente agrega el Rigidbody si falta. Conserva la altura inicial y mantiene root motion desactivado durante la experiencia.
Alex gira suavemente hacia `Player`, incluso mientras se desplaza. Si no se asigna, busca la camara del jugador y luego PlayerTargetProvider. Si no encuentra jugador se detiene hasta encontrarlo. La direccion del clip se calcula con la velocidad lateral real respecto a su orientacion, no con un numero de tramo. No hay estados Patrol/Chase ni comprobaciones de vision.

En `AlexWaypointMotion` se ajustan `Wait Time At Waypoint` (4 s), las velocidades laterales, `Acceleration` (3 m/s2), `Deceleration` (4 m/s2), `Waypoint Distance` (0.08 m) y `Turn Speed` (180 grados/s). La escena usa 0.5 m/s en ambos sentidos. Los tres clips tienen Loop Time activado.
`MoveSpeed` controla el multiplicador de reproduccion de los dos estados de desplazamiento, segun la velocidad real. El estado llamado `Standing Run Left` ahora reproduce `Standing Walk Left 1`. `Run Animation Reference Speed` y `Walk Animation Reference Speed` representan la velocidad que encaja con los pasos del clip a 1x; en la escena ambos valen 1 m/s y se calibran visualmente si los pies resbalan. Caminar a 0.5 m/s con referencia 1 reproduce el clip a 0.5x. Idle mantiene su velocidad normal.

Los FBX Walk Left y Walk Right usan `Loop Time` y `Loop Pose` activados, con `Root Transform Position (XZ) > Bake Into Pose` desactivado y `Based Upon: Center of Mass`. Asi el desplazamiento horizontal del clip queda en root motion, que el script no aplica; la ruta mueve el Rigidbody. Hornear ese desplazamiento en la pose hacia que el cuerpo se alejara de su raiz y saltara al repetirse el clip, aunque el Animator no cambiara de estado. En la prueba aislada con el modelo a escala 2, el salto de cadera de Walk Right al cerrar el ciclo bajo de 2.313 m a 0.0039 m; ambos estados permanecieron activos sin reiniciarse durante 32 segundos por sentido.

Las transiciones estan definidas en `Assets/assets/Anims/Alex_Anim.controller`. El script actualiza `MoveDirection` (int: 0 = idle, -1 = izquierda, 1 = derecha) y `MoveSpeed` (float). Cada estado tiene transiciones hacia los otros dos, condicionadas por MoveDirection. Las flechas usan `Has Exit Time` desactivado, `Fixed Duration` activado, duracion de 0.15 s e `Interruption Source: None`. Esa duracion solo controla la mezcla visual, no la espera ni la duracion del desplazamiento. No se llama a Play ni CrossFade desde el script, ni se consultan nombres de estados para autorizar lanzamientos.

El director recoge el beat y crea el objeto en LateUpdate, despues de evaluar la pose animada de las manos. Las calabazas devueltas siguen el destino actual de EnemyAlex mientras se desplaza. Al pausar conserva el tramo y la espera pendiente. El recorrido se inicializa en el centro solamente al comenzar una nueva experiencia (`Begin` despues de `End`). Los cambios del reloj musical y `PlaybackReset` resincronizan los lanzamientos, pero no cambian la posicion, el destino ni la espera de Alex. Antes, un avance del reloj superior a 0.5 s o cualquier retroceso teletransportaba el Rigidbody al centro; un frame lento podia activar ese reinicio. El movimiento y las animaciones se verificaron con simulacion real de Rigidbody y Animator en Unity 6000.3.9f1. Falta ajustar visualmente la calibracion de los pasos en la escena completa.

### Contacto y puntuacion

1. En `AlexWarhammer`, ajustar `Minimum Strike Speed` (inicial: 1.2 m/s), el collider de la cabeza y la amplitud/duracion haptica. La velocidad se mide en la cabeza del martillo, incluyendo el movimiento por rotacion; la velocidad del proyectil no convierte un toque en golpe.
2. En `AlexThrowDirector`, ajustar `Beat Window` (inicial: +/-0.16 s respecto al beat de llegada), `Travel Beats`, `Throw Every Beats`, altura, distancia y separacion de los carriles.
3. Mantener un martillo quieto: Dollar y calabazas deben sumar. Golpear Dollar debe restar. Golpear una calabaza a tiempo debe sumar y devolverla; fuera de tiempo debe restar.
4. Confirmar que vibra solamente el mando que contacta y que un contacto con ambos martillos no duplica puntos.
5. Pausar/reanudar, reiniciar o saltar el Timeline: se devuelven los objetos activos a sus pools sin penalizacion ni lanzamientos de beats antiguos.

Las instancias se precargan (8 por variante), usan Rigidbody cinematico y colliders trigger. Al resolverlas se reciclan; los prefabs originales conservan sus componentes. La compilacion C# contra las librerias instaladas de Unity y las reglas de puntuacion se verificaron por consola. Las trayectorias, colisiones y haptics necesitan validacion en Play Mode/visor.
