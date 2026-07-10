using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

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

    [Header("State Transition Tuning")]
    public float evadeExitDistance = 8f;
    public float chaseExitDistance = 12f;
    public float minTimeInState = 1f;
    public float transitionCooldown = 0.5f;

    [Header("Stuck Detection")]
    public float stuckDetectionTime = 2f;
    public float stuckDistanceThreshold = 0.3f;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showGizmos = true;

    private NavMeshAgent navAgent;
    private FlockAgent flockComponent;
    private float stateTimer;
    private int currentWaypoint;
    private float timeSinceLastTransition;
    private float transitionCooldownTimer;
    private bool isInitialized = false;

    private Vector3 lastPosition;
    private float stuckTimer;
    private bool isStuck = false;
    private bool isTransitioning = false;

    void Start()
    {
        InitializeComponents();
        InitializeState();
    }

    void InitializeComponents()
    {
        navAgent = GetComponent<NavMeshAgent>();
        flockComponent = GetComponent<FlockAgent>();

        if (navAgent == null)
        {
            Debug.LogError($"[FSM_Agent] NavMeshAgent missing on {gameObject.name}!");
            enabled = false;
            return;
        }

        if (navAgent.isOnNavMesh)
        {
            navAgent.autoBraking = true;
            navAgent.stoppingDistance = 0.5f;
            navAgent.speed = 3.5f;
        }

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        if (flockComponent != null && flockComponent.neighbors == null)
        {
            flockComponent.neighbors = new List<FlockAgent>();
        }

        lastPosition = transform.position;
        isInitialized = true;
    }

    void InitializeState()
    {
        EnterState(AgentState.Idle);
        timeSinceLastTransition = 0f;
        transitionCooldownTimer = 0f;
    }

    void Update()
    {
        if (!isInitialized || navAgent == null) return;

        stateTimer += Time.deltaTime;
        timeSinceLastTransition += Time.deltaTime;

        if (transitionCooldownTimer > 0)
            transitionCooldownTimer -= Time.deltaTime;

        CheckIfStuck();

        if (transitionCooldownTimer <= 0 && !isTransitioning)
        {
            EvaluateTransitions();
        }

        UpdateState();
    }

    void CheckIfStuck()
    {
        if (currentState != AgentState.Chase && currentState != AgentState.Evade)
        {
            isStuck = false;
            stuckTimer = 0f;
            return;
        }

        if (!navAgent.isOnNavMesh || !navAgent.hasPath)
        {
            stuckTimer = 0f;
            isStuck = false;
            return;
        }

        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < stuckDistanceThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckDetectionTime && !isStuck)
            {
                isStuck = true;
                if (showDebugLogs) Debug.Log($"[FSM_Agent] {gameObject.name} is stuck!");
                ForceStateChange();
            }
        }
        else
        {
            stuckTimer = 0f;
            isStuck = false;
        }

        lastPosition = transform.position;
    }

    void ForceStateChange()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            EnterState(AgentState.Patrol);
        }
        else
        {
            EnterState(AgentState.Idle);
        }

        if (navAgent.isOnNavMesh)
        {
            Vector3 randomDirection = Random.insideUnitSphere * 3f;
            randomDirection.y = 0;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position + randomDirection, out hit, 3f, NavMesh.AllAreas))
            {
                navAgent.Warp(hit.position);
                navAgent.ResetPath();
            }
        }
    }

    void EvaluateTransitions()
    {
        if (playerTarget == null)
        {
            if (waypoints != null && waypoints.Length > 0 && currentState != AgentState.Patrol)
            {
                EnterState(AgentState.Patrol);
            }
            else if (currentState != AgentState.Idle)
            {
                EnterState(AgentState.Idle);
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        AgentState newState = currentState;
        bool shouldStayInState = false;

        switch (currentState)
        {
            case AgentState.Evade:
                if (distance > evadeExitDistance)
                {
                    shouldStayInState = false;
                    if (distance < detectionRange)
                    {
                        bool hasFlockNeighbors = flockComponent != null &&
                                                flockComponent.neighbors != null &&
                                                flockComponent.neighbors.Count > 3;
                        newState = hasFlockNeighbors && distance > evadeRange * 1.5f ? AgentState.Flock : AgentState.Chase;
                    }
                    else if (waypoints != null && waypoints.Length > 0)
                    {
                        newState = AgentState.Patrol;
                    }
                    else
                    {
                        newState = AgentState.Idle;
                    }
                }
                else
                {
                    shouldStayInState = true;
                }
                break;

            case AgentState.Chase:
                if (distance < evadeRange * 0.8f)
                {
                    shouldStayInState = false;
                    newState = AgentState.Evade;
                }
                else if (distance > chaseExitDistance)
                {
                    shouldStayInState = false;
                    if (waypoints != null && waypoints.Length > 0)
                        newState = AgentState.Patrol;
                    else
                        newState = AgentState.Idle;
                }
                else
                {
                    bool hasFlockNeighbors = flockComponent != null &&
                                            flockComponent.neighbors != null &&
                                            flockComponent.neighbors.Count > 3;

                    if (hasFlockNeighbors && distance > evadeRange * 1.5f && distance < detectionRange * 0.7f)
                    {
                        shouldStayInState = false;
                        newState = AgentState.Flock;
                    }
                    else
                    {
                        shouldStayInState = true;
                    }
                }
                break;

            case AgentState.Flock:
                bool hasNeighbors = flockComponent != null &&
                                   flockComponent.neighbors != null &&
                                   flockComponent.neighbors.Count > 2;

                if (!hasNeighbors || distance < evadeRange)
                {
                    shouldStayInState = false;
                    newState = distance < evadeRange ? AgentState.Evade : AgentState.Chase;
                }
                else
                {
                    shouldStayInState = true;
                }
                break;

            case AgentState.Patrol:
                if (distance < detectionRange)
                {
                    shouldStayInState = false;
                    if (distance < evadeRange)
                        newState = AgentState.Evade;
                    else
                        newState = AgentState.Chase;
                }
                else
                {
                    shouldStayInState = true;
                }
                break;

            case AgentState.Idle:
                if (distance < detectionRange)
                {
                    shouldStayInState = false;
                    if (distance < evadeRange)
                        newState = AgentState.Evade;
                    else
                        newState = AgentState.Chase;
                }
                else if (stateTimer > idleTime && waypoints != null && waypoints.Length > 0)
                {
                    shouldStayInState = false;
                    newState = AgentState.Patrol;
                }
                else
                {
                    shouldStayInState = true;
                }
                break;
        }

        if (shouldStayInState)
        {
            return;
        }

        if (newState != currentState && timeSinceLastTransition > minTimeInState)
        {
            EnterState(newState);
            transitionCooldownTimer = transitionCooldown;
        }
    }

    void EnterState(AgentState newState)
    {
        if (!isInitialized) return;
        if (isTransitioning) return;

        isTransitioning = true;

        ExitState(currentState);

        AgentState oldState = currentState;
        currentState = newState;
        stateTimer = 0f;
        timeSinceLastTransition = 0f;
        isStuck = false;
        stuckTimer = 0f;

        switch (currentState)
        {
            case AgentState.Idle: OnEnterIdle(); break;
            case AgentState.Patrol: OnEnterPatrol(); break;
            case AgentState.Chase: OnEnterChase(); break;
            case AgentState.Evade: OnEnterEvade(); break;
            case AgentState.Flock: OnEnterFlock(); break;
        }

        if (showDebugLogs && oldState != newState)
        {
            Debug.Log($"[FSM_Agent] {gameObject.name} changed state: {oldState} -> {newState}");
        }

        Invoke(nameof(ResetTransitionFlag), 0.1f);
    }

    void ResetTransitionFlag()
    {
        isTransitioning = false;
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

    // ===== ESTADO: IDLE =====
    void OnEnterIdle()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
            navAgent.autoBraking = true;
        }
    }

    void OnExitIdle() { }

    void UpdateIdle()
    {
        if (stateTimer > idleTime && waypoints != null && waypoints.Length > 0)
        {
            EnterState(AgentState.Patrol);
        }
    }

    // ===== ESTADO: PATROL =====
    void OnEnterPatrol()
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;

        currentWaypoint = 0;
        navAgent.autoBraking = false;
        navAgent.stoppingDistance = 0.1f;
        GoToNextWaypoint();
    }

    void OnExitPatrol()
    {
        if (navAgent != null)
        {
            navAgent.autoBraking = true;
            navAgent.stoppingDistance = 0.5f;
        }
    }

    void UpdatePatrol()
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;

        if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
        {
            GoToNextWaypoint();
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (navAgent == null || !navAgent.isOnNavMesh) return;

        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        if (waypoints[currentWaypoint] != null)
        {
            navAgent.SetDestination(waypoints[currentWaypoint].position);
        }
    }

    // ===== ESTADO: CHASE =====
    void OnEnterChase()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.autoBraking = true;
            navAgent.stoppingDistance = 0.8f;
            navAgent.speed = 3.5f;
        }
        lastPosition = transform.position;
    }

    void OnExitChase() { }

    void UpdateChase()
    {
        if (playerTarget == null || navAgent == null || !navAgent.isOnNavMesh) return;

        navAgent.SetDestination(playerTarget.position);
    }

    // ===== ESTADO: EVADE =====
    void OnEnterEvade()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.autoBraking = true;
            navAgent.stoppingDistance = 0.1f;
            navAgent.speed = 5f;
            FindEscapeDestination();
        }
        lastPosition = transform.position;
    }

    void FindEscapeDestination()
    {
        if (playerTarget == null || navAgent == null) return;

        Vector3 fleeDirection = (transform.position - playerTarget.position).normalized;
        Vector3 fleePosition = transform.position + fleeDirection * 15f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleePosition, out hit, 15f, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
        }
    }

    void OnExitEvade()
    {
        if (navAgent != null)
        {
            navAgent.speed = 3.5f;
        }
    }

    void UpdateEvade()
    {
        if (playerTarget == null || navAgent == null || !navAgent.isOnNavMesh) return;

        if (!navAgent.pathPending && navAgent.remainingDistance < 0.5f)
        {
            FindEscapeDestination();
        }
    }

    // ===== ESTADO: FLOCK =====
    void OnEnterFlock()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.enabled = false;
        }

        if (flockComponent != null && !flockComponent.enabled)
        {
            flockComponent.enabled = true;
        }
    }

    void OnExitFlock()
    {
        if (navAgent != null)
        {
            navAgent.enabled = true;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
            {
                navAgent.Warp(hit.position);
            }
            navAgent.ResetPath();
        }

        if (flockComponent != null && flockComponent.enabled)
        {
            flockComponent.enabled = false;
        }
    }

    void UpdateFlock()
    {
        if (flockComponent == null) return;

        if (navAgent != null && !navAgent.enabled)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 1f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }
    }

    // ===== MÉTODOS PÚBLICOS =====
    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    public void AddWaypoint(Transform waypoint)
    {
        if (waypoints == null)
        {
            waypoints = new Transform[] { waypoint };
        }
        else
        {
            List<Transform> list = new List<Transform>(waypoints);
            list.Add(waypoint);
            waypoints = list.ToArray();
        }
    }

    public AgentState GetCurrentState()
    {
        return currentState;
    }

    // ===== VISUALIZACIÓN =====
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showGizmos) return;

        if (playerTarget != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, evadeRange);

            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, evadeExitDistance);
        }

        switch (currentState)
        {
            case AgentState.Idle: Gizmos.color = Color.gray; break;
            case AgentState.Patrol: Gizmos.color = Color.blue; break;
            case AgentState.Chase: Gizmos.color = Color.red; break;
            case AgentState.Evade: Gizmos.color = Color.magenta; break;
            case AgentState.Flock: Gizmos.color = Color.cyan; break;
        }

        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (isStuck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
        }
    }

    void OnEnable()
    {
        if (flockComponent != null && currentState != AgentState.Flock)
        {
            flockComponent.enabled = false;
        }
    }

    void OnDisable()
    {
        if (navAgent != null && !navAgent.enabled)
        {
            navAgent.enabled = true;
        }
        CancelInvoke();
    }
}