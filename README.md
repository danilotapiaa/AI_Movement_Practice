# AI Movement Practice

Proyecto de práctica en Unity centrado en comportamientos de movimiento e inteligencia artificial para agentes NPC. Incluye persecución/evasión basada en `NavMeshAgent`, patrullaje por waypoints, wandering (deambulación aleatoria), flocking (comportamiento de bandada tipo boids) y una máquina de estados finitos (FSM) que combina todos estos comportamientos en un único agente que reacciona según la distancia al jugador.

El objetivo es experimentar con distintas técnicas de IA de movimiento (steering behaviors, NavMesh pathfinding y FSM) más que construir un juego completo: no hay condiciones de victoria/derrota ni un sistema de juego definido, es un entorno de pruebas para observar y ajustar el comportamiento de los agentes.

## Controles

No hay controles de jugador implementados en los scripts actuales (no se encontró un script de movimiento de jugador; solo hay un `InputSystem_Actions.inputactions` de la plantilla base de Unity). El foco del proyecto está en observar el comportamiento autónomo de los agentes de IA en la escena `SampleScene`, ajustando sus parámetros desde el Inspector.

## Stack

- **Unity**: 6000.4.9f1

## Estructura de `Assets/`

- `Scripts/AI/` — Lógica de movimiento e IA de los agentes:
  - `ChasePlayer.cs`, `ChaseEvade.cs` — persecución y evasión con `NavMeshAgent`.
  - `PatrolAgent.cs` — patrullaje entre waypoints.
  - `WanderAgent.cs` — deambulación aleatoria sobre el NavMesh.
  - `FlockAgen.cs` — comportamiento de bandada (cohesión, separación, alineación).
  - `FSM_Agent.cs` — máquina de estados finitos que combina Idle, Patrol, Chase, Evade y Flock en un solo agente.
- `Scripts/Debug/` — `StateDebugger.cs`, muestra el estado actual del FSM en un `Text` de la UI.
- `Scenes/` — Escena principal `SampleScene` con el `NavMesh` horneado (`NavMesh-NavMeshSurface`).
- `Prefabs/` — Carpeta reservada para prefabs de agentes (vacía por ahora).
- `Waypoints/` — Carpeta reservada para puntos de patrullaje (vacía por ahora).
- `Settings/` — Configuración del render pipeline (URP).
- `TutorialInfo/` — Contenido de la plantilla base de Unity (sin modificar).

## Estado actual

Proyecto experimental/en progreso. Los scripts de IA están funcionales y documentados, pero:
- Las carpetas `Prefabs/` y `Waypoints/` están vacías: los prefabs y waypoints se configuran directamente en la escena, aún no se han extraído como assets reutilizables.
- No existe un script de control de jugador; se depende de un `GameObject` con tag `Player` ya presente en la escena.
- `FlockAgen.cs` tiene un nombre con una probable errata (debería ser `FlockAgent`), se mantiene tal cual para no romper referencias en la escena/prefabs.
