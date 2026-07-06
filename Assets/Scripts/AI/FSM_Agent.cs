using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Enumeración de estados posibles
public enum AgentState
{
    Idle,
    Patrol,
    Chase,
    Evade,
    Flock
}

public class FSM_Agent : MonoBehaviour
{
    [Header("References")]
    public Transform playerTarget;
    public Transform[] waypoints;
    public List<FlockAgent> flockNeighbors;

    [Header("State Configuration")]
    public AgentState currentState = AgentState.Idle;
    public float detectionRange = 15f;
    public float evadeRange = 5f;
    public float idleTime = 2f;

    // Componentes
    private NavMeshAgent navAgent;
    private FlockAgent flockComponent;
    private float stateTimer;
    private int currentWaypoint;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        flockComponent = GetComponent<FlockAgent>();

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        // Iniciar en estado Idle
        EnterState(AgentState.Idle);
    }

    void Update()
    {
        stateTimer += Time.deltaTime;
        // Evaluar condiciones de transición (siempre)
        EvaluateTransitions();
        // Ejecutar el estado actual
        UpdateState();
    }

    void EvaluateTransitions()
    {
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        AgentState newState = currentState;

        // Jerarquía de decisiones
        if (distance < evadeRange)
        {
            newState = AgentState.Evade;
        }
        else if (distance < detectionRange)
        {
            // Si hay jugadores cerca y muchos compañeros, puede hacer flock
            if (flockComponent != null && flockComponent.neighbors.Count > 3)
                newState = AgentState.Flock;
            else
                newState = AgentState.Chase;
        }
        else if (waypoints != null && waypoints.Length > 0)
        {
            newState = AgentState.Patrol;
        }
        else
        {
            newState = AgentState.Idle;
        }

        if (newState != currentState)
        {
            EnterState(newState);
        }
    }

    void EnterState(AgentState newState)
    {
        // Salir del estado actual
        ExitState(currentState);

        // Cambiar estado
        currentState = newState;
        stateTimer = 0f;

        // Entrar al nuevo estado
        switch (currentState)
        {
            case AgentState.Idle: OnEnterIdle(); break;
            case AgentState.Patrol: OnEnterPatrol(); break;
            case AgentState.Chase: OnEnterChase(); break;
            case AgentState.Evade: OnEnterEvade(); break;
            case AgentState.Flock: OnEnterFlock(); break;
        }
    }

    void ExitState(AgentState state)
    {
        switch (state)
        {
            case AgentState.Idle: OnExitIdle(); break;
            case AgentState.Patrol: OnExitPatrol(); break;
            case AgentState.Chase: OnExitChase(); break;
            case AgentState.Evade: OnExitEvade(); break;
            case AgentState.Flock: OnExitFlock(); break;
        }
    }

    void UpdateState()
    {
        switch (currentState)
        {
            case AgentState.Idle: UpdateIdle(); break;
            case AgentState.Patrol: UpdatePatrol(); break;
            case AgentState.Chase: UpdateChase(); break;
            case AgentState.Evade: UpdateEvade(); break;
            case AgentState.Flock: UpdateFlock(); break;
        }
    }

    // ===== MÉTODOS DE ESTADO: IDLE =====
    void OnEnterIdle()
    {
        if (navAgent.isOnNavMesh) navAgent.ResetPath();
        Debug.Log("Entering Idle state");
    }
    void OnExitIdle() { }
    void UpdateIdle()
    {
        // Esperar un tiempo y luego cambiar a Patrol automáticamente
        if (stateTimer > idleTime && waypoints.Length > 0)
        {
            EnterState(AgentState.Patrol);
        }
    }

    // ===== MÉTODOS DE ESTADO: PATROL =====
    void OnEnterPatrol()
    {
        currentWaypoint = 0;
        navAgent.autoBraking = false;
        GoToNextWaypoint();
        Debug.Log("Entering Patrol state");
    }
    void OnExitPatrol() { }
    void UpdatePatrol()
    {
        if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
        {
            GoToNextWaypoint();
        }
    }
    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        navAgent.SetDestination(waypoints[currentWaypoint].position);
    }

    // ===== MÉTODOS DE ESTADO: CHASE =====
    void OnEnterChase()
    {
        navAgent.autoBraking = true;
        Debug.Log("Entering Chase state");
    }
    void OnExitChase() { }
    void UpdateChase()
    {
        if (playerTarget != null)
        {
            navAgent.SetDestination(playerTarget.position);
        }
    }

    // ===== MÉTODOS DE ESTADO: EVADE =====
    void OnEnterEvade()
    {
        navAgent.autoBraking = true;
        Debug.Log("Entering Evade state");
    }
    void OnExitEvade() { }
    void UpdateEvade()
    {
        if (playerTarget == null) return;
        Vector3 fleeDirection = (transform.position - playerTarget.position).normalized;
        Vector3 fleePosition = transform.position + fleeDirection * 10f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleePosition, out hit, 10f, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
        }
    }

    // ===== MÉTODOS DE ESTADO: FLOCK =====
    void OnEnterFlock()
    {
        navAgent.enabled = false; // Desactivar NavMesh para usar movimiento directo
        Debug.Log("Entering Flock state");
    }
    void OnExitFlock()
    {
        navAgent.enabled = true;
        // Restaurar posición del agente en el NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
    }
    void UpdateFlock()
    {
        if (flockComponent != null)
        {
            // El flock se maneja en el componente FlockAgent
            // Solo actualizamos la posición en el NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 1f, NavMesh.AllAreas))
                transform.position = hit.position;
        }
    }

    // Visualización del estado actual en la escena
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        switch (currentState)
        {
            case AgentState.Idle: Gizmos.color = Color.gray; break;
            case AgentState.Patrol: Gizmos.color = Color.blue; break;
            case AgentState.Chase: Gizmos.color = Color.red; break;
            case AgentState.Evade: Gizmos.color = Color.magenta; break;
            case AgentState.Flock: Gizmos.color = Color.cyan; break;
        }
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}