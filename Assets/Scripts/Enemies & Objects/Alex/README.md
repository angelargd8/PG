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
En cada parada espera un segundo en `Standing Idle 03`. Lanza al beat en cualquier estado, durante los desplazamientos y durante las transiciones.
Usa `Standing Run Left` para centro -> (1) y (2) -> centro; usa `Standing Walk Right` para (1) -> centro y centro -> (2).
Conserva la orientacion del enemigo y su separacion vertical respecto a los waypoints. El desplazamiento lo controla el script con root motion desactivado durante la experiencia.

En `AlexWaypointMotion` se pueden cambiar `Idle Duration` (1 s), `Run Left Speed` (2 m/s) y `Walk Right Speed` (1 m/s). La espera ya no depende de cuantos objetos se lancen. Los tres clips tienen Loop Time activado.

Las transiciones estan definidas en `Assets/assets/Anims/Alex_Anim.controller`. El script solo actualiza el parametro entero `MoveDirection`: 0 = idle, -1 = izquierda, 1 = derecha. Cada estado tiene transiciones hacia los otros dos, condicionadas por ese parametro. Las flechas usan `Has Exit Time` desactivado, `Fixed Duration` activado, duracion de 0.15 s e `Interruption Source: None`. Esto permite completar cada mezcla antes de evaluar el siguiente cambio de direccion; la combinacion anterior de interrupciones sin orden reiniciaba la mezcla y dejaba idle como estado actual. La duracion y las condiciones se editan seleccionando la flecha en el Animator. No se llama a Play ni CrossFade desde el script, ni se consultan nombres de estados para autorizar lanzamientos.

El director recoge el beat y crea el objeto en LateUpdate, despues de evaluar la pose animada de las manos. Las calabazas devueltas siguen el destino actual de EnemyAlex mientras se desplaza. Al pausar conserva el tramo; al saltar el reloj reinicia en el centro. Se verificaron dos ciclos de movimiento con reloj/Animator simulados, sin necesidad de disparos para avanzar. Ademas, se reprodujo el bloqueo de idle con el Animator real de Unity 6000.3.9f1 en un proyecto temporal y se verifico la correccion usando el modelo y los clips de Alex (idle, izquierda, idle, derecha, idle, izquierda, derecha). Falta comprobar visualmente las mezclas y el deslizamiento de los pies en la escena completa.

### Contacto y puntuacion

1. En `AlexWarhammer`, ajustar `Minimum Strike Speed` (inicial: 1.2 m/s), el collider de la cabeza y la amplitud/duracion haptica. La velocidad se mide en la cabeza del martillo, incluyendo el movimiento por rotacion; la velocidad del proyectil no convierte un toque en golpe.
2. En `AlexThrowDirector`, ajustar `Beat Window` (inicial: +/-0.16 s respecto al beat de llegada), `Travel Beats`, `Throw Every Beats`, altura, distancia y separacion de los carriles.
3. Mantener un martillo quieto: Dollar y calabazas deben sumar. Golpear Dollar debe restar. Golpear una calabaza a tiempo debe sumar y devolverla; fuera de tiempo debe restar.
4. Confirmar que vibra solamente el mando que contacta y que un contacto con ambos martillos no duplica puntos.
5. Pausar/reanudar, reiniciar o saltar el Timeline: se devuelven los objetos activos a sus pools sin penalizacion ni lanzamientos de beats antiguos.

Las instancias se precargan (8 por variante), usan Rigidbody cinematico y colliders trigger. Al resolverlas se reciclan; los prefabs originales conservan sus componentes. La compilacion C# contra las librerias instaladas de Unity y las reglas de puntuacion se verificaron por consola. Las trayectorias, colisiones y haptics necesitan validacion en Play Mode/visor.
