# Prueba del cierre con créditos

Ejecutar en un proyecto temporal, nunca sobre las escenas del proyecto principal:

1. `powershell -NoProfile -ExecutionPolicy Bypass -File Assets/Scripts/Tests~/Credits/StageProbe.ps1`
2. Ejecutar Unity 6000.3.9f1 con `-batchmode -nographics -projectPath <TEMP>/PG-Credits-Validation -executeMethod CreditsProbeSetup.Start -logFile <TEMP>/PG-Credits-Validation/validation.log`.
3. Verificar `CREDITS PROBE PASSED` y salida 0.

StageProbe acepta `-ProbePath` para una carpeta nueva y copia las clases reales del proyecto, las dependencias necesarias y los paquetes de UI/Timeline cacheados. El fixture crea escenas mínimas independientes y un Timeline con audio. El runner adelanta ese Timeline al final, sin sustituir el detector de final de canción.

Comprueba Full e individual, cinco segundos completos después del reveal (con timeScale=0), el orden créditos/resultados, conservación del puntaje, rechazo de bonos tardíos, eventos duplicados, descarga de gameplay, colocación del Canvas, salida al menú durante fade y créditos, reinicio de otra partida y compatibilidad sin créditos configurados. No mide rendimiento ni legibilidad en Quest.
