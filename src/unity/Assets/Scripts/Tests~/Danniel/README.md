# Danniel: disparos y reciclaje

Ejecutar `DannielShootingProbeSetup.Start` en un proyecto temporal de Unity 6000.3.9f1. El setup crea una escena vacía; no ejecutarlo en el proyecto principal.

Copiar el probe a runtime y su setup a Editor. Incluir los scripts reales BulletPool, PooledBullet, EnemyShooter, DannielRhythmDirector, BeatMapSO, ExperienceMusicClock, IExperienceRuntime e IExperiencePreloadable.

La prueba comprueba con física de Unity que una bala puede salir atravesando un collider de su propio tirador y que esa excepción se actualiza al reutilizarla para otro enemigo. Comprueba expiración, 80 balas simultáneas con un límite de retención de 4, y 500 turnos del director con 24 tiradores que se desactivan y reutilizan. Los turnos se invocan directamente: no es una reproducción audiovisual completa de la escena ni una prueba de rendimiento en Quest.

También reproduce el cruce del límite de alcance entre el aviso y el disparo: el alcance decide si se puede anunciar un ataque nuevo; un ataque anunciado se completa al beat si el tirador y sus dependencias siguen activos. Reciclar al enemigo cancela su reserva. Cada aviso solo puede dispararse una vez.

Para investigar una incidencia durante Play, el menú contextual de DannielRhythmDirector incluye `Log Runtime Shooting State`. Imprime el tiempo musical y los tiradores registrados, pendientes y elegibles, con sus distancias. BulletPool incluye `Log Bullet Pool State`, con balas activas y disponibles. Activar Log Beats registra también los ataques anunciados que no pudieron ejecutarse.

## Presupuesto de segmentos en Android

`DannielSegmentProbeSetup.Start` comprueba el código real de SegmentPool seleccionando Android, Windows Editor y Windows Player. Incluir también SegmentPool, SegmentAnchors, SegmentEnemySpawns, SegmentContent, EnemySpawnDirector, EnemyPool, EnemyController, EnemyMeleeAI, PlayerTargetProvider y DifficultyLevel en el proyecto temporal.

Usa las longitudes de anchors de los dos prefabs de Danniel y verifica 300 reciclajes: presupuestos de 4, 3 y 2 activos; continuidad EndPoint/StartPoint; alternancia de tipos; retirada del tramo trasero únicamente cuando su final está detrás del área inicial; y ausencia de instancias adicionales durante la partida. Es una prueba funcional, no una medición de FPS o memoria en Quest.

La escena Danniel configura Android con 3 activos y 4 enemigos base por segmento. Editor/PC/Quest Link mantienen 4 activos y 6 enemigos base. Se adelanta la retirada trasera una longitud de segmento respecto a Recycle Z (-80), sin retirar el tramo que todavía cubre el área inicial. Para alternar dos tipos con un presupuesto impar de 3 activos, se conservan 4 instancias físicas (3 activas y 1 de reserva). La reserva evita Instantiate/Destroy durante gameplay; esta configuración reduce trabajo de segmentos activos, no el número total de instancias de segmentos en memoria.
