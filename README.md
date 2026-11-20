# Proyectos de graduación

## Implementación de un sistema de secuenciación usando la Arquitectura Dirigida por Eventos en un videoclip musical en realidad virtual para analizar la percepción de presencia mediante el I-Group Presence Questionnaire

## IMPLEMENTACIÓN DE UN SISTEMA ADAPTATIVO DE INTERACCIÓN Y AJUSTE DE DIFICULTAD DINÁMICO PARA UNA EXPERIENCIA DE VIDEOCLIP GAMIFICADO EN REALIDAD VIRTUAL

**PUNKO VR**

## Descripción

El propósito del proyecto es desarrollar un videoclip musical interactivo en realidad virtual que sincronice escenas, sonidos e interacciones con los momentos de una canción, usando una arquitectura dirigida por eventos. Luego, se evalúa cómo esa experiencia influye en la sensación de presencia de los usuarios mediante el cuestionario IPQ (I-Group Presence Questionnaire).

Una dificultad fija puede ser demasiado fácil para algunas personas y frustrante para otras, además de hacer que la música pase a segundo plano. Este proyecto adapta los retos de los minijuegos según cómo juega cada persona, mientras continúa el videoclip, para mantener una experiencia equilibrada y disfrutable.

El submódulo [`PG-analisis-resultados`](src/PG-analisis-resultados/) contiene los análisis de trazabilidad de eventos, respuestas del IPQ y rendimiento técnico. El análisis de rendimiento se realizó con el build utilizado durante las pruebas. Las ejecuciones de PC mediante Quest Link y las del APK en Meta Quest 3S se analizan por separado porque usan herramientas, dispositivos y frecuencias de muestreo diferentes. Este submódulo documenta y analiza resultados; no contiene el código fuente completo de la aplicación VR. Las respuestas del IPQ no están vinculadas con las capturas de rendimiento.

## Tecnologías Utilizadas

- Unity `6000.3.9f1`, C# y Universal Render Pipeline (URP).
- Unity XR Interaction Toolkit, OpenXR y Android XR.
- Python 3.10 o superior para los análisis.
- Jupyter Notebook, pandas, NumPy, Matplotlib, dataframe-image y openpyxl.
- Git y submódulos Git.

## Requisitos Previos

- Git.
- Unity `6000.3.9f1` para abrir y ejecutar la aplicación.
- Android Build Support de Unity, con Android SDK, NDK y OpenJDK, para generar un APK.
- Un dispositivo compatible con XR para probar la experiencia.
- Python 3.10 o superior, pip y Jupyter Notebook o JupyterLab para ejecutar los análisis del submódulo.

## Instalación

### Aplicación de realidad virtual

1. Clonar el repositorio con sus submódulos:

   ```bash
   git clone --recurse-submodules https://github.com/GraduacionesUVG/2026-22869-garcia-donis-angela-rosana
   cd PG
   ```

2. Si el submódulo no se descargó durante la clonación, inicializarlo:

   ```bash
   git submodule update --init --recursive
   ```

3. En Unity Hub, agregar y abrir `src/unity/` con Unity `6000.3.9f1`. Unity resolverá las dependencias declaradas en `Packages/manifest.json`.

4. Ejecutar la escena inicial configurada por el equipo desde el Editor. Para generar un APK, instalar Android Build Support y seleccionar Android en los Build Profiles.

### Análisis de resultados

1. Desde la raíz del repositorio, crear un entorno virtual:

   ```bash
   python -m venv .venv
   ```

2. Activar el entorno virtual.

   En Windows PowerShell:

   ```powershell
   .\.venv\Scripts\Activate.ps1
   ```

   En macOS o Linux:

   ```bash
   source .venv/bin/activate
   ```

3. Instalar las dependencias de análisis:

   ```bash
   python -m pip install jupyter pandas numpy matplotlib dataframe-image openpyxl
   ```

4. Abrir Jupyter desde el submódulo:

   ```bash
   cd src/PG-analisis-resultados
   jupyter notebook
   ```

5. Ejecutar los cuadernos en orden, según el análisis requerido:
   - `Eventos/EDA.ipynb`: trazabilidad de la Arquitectura Dirigida por Eventos.
   - `IPQ/resultados-IPQ.ipynb`: resultados del cuestionario IPQ.
   - `rendimiento/rendimiento.ipynb`: rendimiento técnico de las ejecuciones.

Los análisis son descriptivos y exploratorios. Las mediciones de PC y APK no son directamente equivalentes, y las respuestas del IPQ se deben manejar de acuerdo con las autorizaciones y condiciones de uso de esos datos.

## Capturas

Las capturas del sistema están en [`/capturas`](capturas/). La entrega debe incluir al menos 10 imágenes.

## Demo

El video demostrativo está en [`/demo/demo.mp4`](DEMO/demo.mp4).

## Documentación

- [Informe final del equipo](docs/informe_final.pdf)
- [Consentimiento firmado para el uso de foto y video](docs/https://github.com/GraduacionesUVG/2026-22869-garcia-donis-angela-rosana)

## Estructura del Repositorio

```text
capturas/                       Capturas del sistema
demo/
  demo.mp4                       Video demostrativo
docs/
  informe_final.pdf              Informe final del equipo
  consentimiento_firmado.pdf    Consentimiento firmado
src/
  unity/                         Proyecto Unity completo
    Assets/
    Packages/
    ProjectSettings/
  PG-analisis-resultados/        Submódulo de análisis
README.md
```

No se incluyeron credenciales, claves privadas ni keystores de firma en el repositorio.
