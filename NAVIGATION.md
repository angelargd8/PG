# Flujo inicial de escenas

## Descripción

Se implementó el flujo inicial de navegación del proyecto utilizando una arquitectura basada en eventos.

Actualmente, la aplicación sigue este recorrido:

```text
Bootstrap
   ↓
MainMenu
   ↓
Loading
   ↓
SampleScene
```

El objetivo de esta primera implementación es comprobar que las escenas pueden cargarse y descargarse correctamente de forma aditiva, manteniendo los sistemas globales separados del contenido de cada nivel.

## Escenas actuales

### `00_Bootstrap`

Es la primera escena que debe ejecutarse y permanece cargada durante toda la aplicación.

Contiene los sistemas responsables de controlar el flujo general:

* `AppStateMachine`
* `SceneFlowManager`
* `Otros Managers`, preparado para futuras implementaciones.

Desde esta escena se carga el menú principal de forma aditiva.

### `MainMenu`

Contiene la interfaz del menú principal y el botón para iniciar la experiencia.

Cuando el usuario presiona el botón **Iniciar**, el `MainMenuController` no carga directamente la siguiente escena. En su lugar, publica el evento:

```text
StartExperienceRequested
```

Este evento es un `ScriptableObject Event Channel`, lo que permite que el menú se comunique con otros sistemas sin tener una referencia directa hacia ellos.

### `Loading`

Es una escena temporal que se muestra durante la transición entre el menú y el nivel.

Actualmente funciona como una pantalla intermedia mientras el `SceneFlowManager`:

1. Descarga el menú principal.
2. Carga la escena de prototipo.
3. Establece el prototipo como escena activa.
4. Descarga la pantalla de carga.

Más adelante puede incluir una barra de progreso, animaciones o transiciones visuales.

### `SampleScene`

Es la escena utilizada para comprobar que el flujo funciona correctamente.

Por ahora contiene únicamente objetos de prueba. En el futuro será reemplazada o convertida en la primera sección de la experiencia musical.

## Funcionamiento del flujo

```text
La aplicación inicia
        ↓
Bootstrap inicializa los sistemas
        ↓
AppStateMachine carga MainMenu
        ↓
El usuario presiona Iniciar
        ↓
MainMenuController publica StartExperienceRequested
        ↓
AppStateMachine recibe el evento
        ↓
Cambia el estado a Loading
        ↓
SceneFlowManager carga Loading
        ↓
Descarga MainMenu
        ↓
Carga SampleScene
        ↓
Descarga Loading
        ↓
AppStateMachine cambia al estado Experience
```

## Responsabilidad de los componentes

### `MainMenuController`

Produce el evento cuando el usuario presiona el botón de inicio.

No conoce ni controla directamente las escenas.

### `StartExperienceRequested`

Es el canal de eventos compartido entre el menú y la máquina de estados.

Permite la comunicación entre sistemas independientes y entre diferentes escenas.

### `AppStateMachine`

Controla el estado general de la aplicación.

Los estados actuales son:

```text
Booting
MainMenu
Loading
Experience
Ending
```

También valida que la transición solo pueda ejecutarse cuando la aplicación se encuentra en el menú.

### `SceneFlowManager`

Es el único sistema encargado de:

* Cargar escenas.
* Descargar escenas.
* Establecer la escena activa.
* Controlar el orden de las transiciones.

Las escenas se cargan de forma aditiva para que `Bootstrap` pueda permanecer activo durante toda la aplicación.

## Arquitectura utilizada

En esta implementación se utilizan:

* `UnityEvent` para conectar el botón del menú con `MainMenuController`.
* Eventos de C# dentro de sistemas individuales cuando sea necesario.
* `ScriptableObject Event Channels` para comunicar escenas y sistemas independientes.
* `AppStateMachine` para controlar los estados generales.
* `SceneFlowManager` para manejar las escenas.

El botón del menú no utiliza directamente:

```csharp
SceneManager.LoadScene();
```

En su lugar, produce un evento que es procesado por los sistemas responsables del flujo.

## Configuración de ejecución

Las escenas deben estar agregadas a la configuración de compilación:

```text
0 - Bootstrap
1 - MainMenu
2 - Loading
3 - SampleScene
```

Las pruebas deben iniciarse siempre desde:

```text
0 Bootstrap
```

Si se inicia directamente desde `MainMenu` o `SampleScene`, los sistemas globales del Bootstrap no estarán disponibles.

## TODO: next step (aun por pensar)

Después de validar este flujo, se agregará una escena `ExperienceCore`, que contendrá los sistemas persistentes de la experiencia musical, como:

* XR Origin.
* Reproducción de la canción.
* Timeline.
* SequenceDirector.
* Sistemas compartidos de interacción, audio y sincronización.

Posteriormente, el flujo podrá ampliarse a:

```text
Bootstrap
   ↓
MainMenu
   ↓
Loading
   ↓
ExperienceCore + Scene01
   ↓
Scene02
   ↓
Scene03
   ↓
Scene04
   ↓
Scene05
   ↓
Scene06
   ↓
Fin de la canción
```
