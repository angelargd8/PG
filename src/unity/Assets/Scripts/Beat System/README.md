# Generación y uso de BeatMap

Este documento explica cómo se genera el `BeatMapSO` del proyecto, qué datos calcula automáticamente y qué ajustes siguen siendo manuales. La implementación está en `BeatMapGenerator.cs`, `BeatIntensityAnalyzer.cs`, `BeatMapSO.cs`, `ExperienceBeatPlayer.cs` y `ExperienceMusicClock.cs`.

## Resumen del proceso

```text
AudioClip seleccionado
        │
        ├─ lectura de muestras y conversión a mono
        ├─ envolvente de energía por ventanas de 1024 muestras
        ├─ estimación del BPM y de la fase del primer pulso
        ├─ generación de tiempos equidistantes hasta el final del audio
        ├─ cálculo de intensidad relativa cerca de cada tiempo
        └─ creación de un asset BeatMapSO junto al AudioClip
```

El resultado es una cuadrícula rítmica estimada. No es una transcripción de todos los transitorios de la canción: todos los tiempos se derivan de un BPM común y mantienen el mismo intervalo.

## Generar un BeatMap en Unity

1. En la ventana **Project**, selecciona el `AudioClip` de la canción.
2. En el menú, ejecuta **Tools > Music > Generate Beat Map**.
3. Si Unity no puede leer las muestras, comprueba que el audio esté cargado. El generador intenta cargar sus datos; si `GetData` falla, el importador del clip debe usar **Load Type: Decompress On Load**. Aplica el cambio y vuelve a generar.
4. El asset se crea en la misma carpeta que el audio, con el nombre `<nombre del clip>_BeatMap.asset`. Si ya existe, Unity elige una ruta única.
5. Revisa el asset y escucha el audio con la escena o herramienta de validación. Corrige manualmente tiempos o metadatos musicales si hace falta.

El generador requiere un clip con muestras, canales y frecuencia válidos. Para el análisis de tempo promedia los canales para formar una señal mono. Después agrupa las muestras completas en ventanas de 1024; una posible cola menor a una ventana no participa en esa estimación.

## Cómo estima el tempo y los tiempos

### 1. Envolvente de energía

Para cada ventana de 1024 muestras calcula la energía media: la suma de las muestras al cuadrado dividida entre 1024. La frecuencia de esa envolvente es `frecuencia de muestreo / 1024`.

### 2. BPM

Compara la envolvente consigo misma a distintos retrasos (*lags*) mediante una suma de productos. Los retrasos evaluados corresponden inicialmente a tempos entre 70 y 180 BPM; el retraso con mayor correlación determina el BPM estimado.

Luego ajusta el resultado a un rango preferido de 100 a 180 BPM duplicándolo o dividiéndolo por dos. Esto resuelve algunas ambigüedades de octava, pero puede representar un pulso percibido como medio tiempo o doble tiempo respecto de la interpretación musical.

### 3. Fase inicial y cuadrícula

Con el BPM estimado calcula la duración de un pulso (`60 / BPM`). Busca un desplazamiento inicial dentro de ese periodo y elige el que acumula más energía en la envolvente. Ese desplazamiento se convierte en el tiempo del primer beat.

A partir de ahí genera tiempos con el mismo intervalo hasta la duración del clip, inclusive si el siguiente tiempo cae exactamente en el final. Por tanto, el método supone un tempo constante y no detecta cambios de tempo, silencios estructurales, síncopas ni desviaciones expresivas de cada golpe.

## Cómo calcula la intensidad

Para cada tiempo generado, `BeatIntensityAnalyzer` calcula el RMS del audio dentro de una ventana de 150 ms centrada en el beat (75 ms a cada lado), recortada al rango disponible del clip. Combina la energía de los canales al cuadrar cada muestra antes de promediar, de modo que canales en oposición de fase no se cancelen entre sí en este cálculo.

Después escala las intensidades relativas usando los percentiles 5 y 95 para que unos pocos picos no aplanen toda la canción. Los valores finales quedan entre 0 y 1. Si el audio es prácticamente silencioso, devuelve ceros. Esta intensidad representa el nivel RMS de la mezcla alrededor del beat; no identifica instrumentos ni determina por sí sola si un beat es un acento musical.

## Datos incluidos y edición manual

`BeatMapSO` almacena:

- El `AudioClip` asociado y el BPM estimado.
- Pulsos por compás y figura del pulso. El valor inicial es 4/4; no se detecta automáticamente.
- La lista ordenada de beats con tiempo e intensidad.
- Por beat: marca de acento, número de compás, pulso dentro del compás y subdivisión.
- Secciones opcionales: tipo, etiqueta, inicio incluido y final excluido.

La generación crea los beats desde sus tiempos y recalcula sus intensidades. Los demás metadatos de cada beat conservan sus valores iniciales: acento desactivado, compás y pulso sin definir (`0`), y una subdivisión. La lista de secciones queda vacía. El generador no decide automáticamente compases, acentos, subdivisiones ni secciones.

Puedes ajustar estos datos en el Inspector del asset. `BeatMapSO.OnValidate` limita los valores a rangos válidos y ordena cronológicamente beats y secciones. El asset también migra una lista antigua de tiempos de beat a la estructura nueva cuando se carga.

**Volver a generar** crea otro asset con una ruta única; no actualiza un `BeatMapSO` existente. En cambio, el comando de recálculo descrito abajo modifica las intensidades del asset seleccionado.

## Recalcular solo las intensidades

1. Selecciona el `BeatMapSO` en la ventana **Project**.
2. Ejecuta **Tools > Music > Recalculate Beat Intensities**.

El asset debe tener audio asociado y al menos un beat. El cálculo usa los tiempos existentes. Guarda las intensidades nuevas, pero conserva los tiempos y los metadatos editados manualmente, como acentos, compases, subdivisiones y secciones.

## Validación y reproducción

`BeatMapValidator` es una herramienta de escucha sencilla: reproduce el audio asociado, compara `AudioSource.time` con el próximo tiempo del mapa y puede disparar un clip de clic y escribir el índice/tiempo en la consola. No corrige el mapa automáticamente.

Durante la experiencia, `ExperienceMusicClock` lee el tiempo y el estado del `PlayableDirector` compartido. `ExperienceBeatPlayer` busca por búsqueda binaria el primer beat posterior al tiempo actual, considera el beat más reciente y emite `BeatReached` si no llega con más retraso que `Max Beat Lateness` (0.1 segundos por defecto). Tras un salto que deje atrás varios beats no reproduce una ráfaga histórica; omite los beats antiguos. Un retroceso del Timeline, cambio de mapa, pausa o desactivación puede emitir `PlaybackReset` según el caso.

## Límites de la estimación

- La estimación es automática y aproximada; debe verificarse escuchando la canción.
- El tempo se modela como constante. Las canciones con cambios de tempo pueden requerir corrección o un flujo de edición distinto.
- La elección de BPM puede caer en medio tiempo o doble tiempo, especialmente por la preferencia de 100–180 BPM.
- La fase inicial se estima desde energía por ventanas, no mediante detección precisa del transitorio de cada instrumento.
- Las intensidades describen energía relativa de la mezcla en torno a cada pulso; no equivalen a una evaluación perceptual ni a acentos anotados.
- Compás, acentos, subdivisiones y secciones necesitan configuración manual si el juego los requiere.
