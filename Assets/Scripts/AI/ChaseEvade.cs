using UnityEngine;
using UnityEngine.AI;

public class ChaseEvade : MonoBehaviour
{
    [Header("Behavior Parameters")]
    public Transform playerTarget;
    public float chaseRange = 15f;
    public float evadeRange = 5f;
    public float escapeDistance = 10f;
    public float updateInterval = 0.3f;

    private NavMeshAgent agent;
    private float timer;

    void Start() 
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (playerTarget == null) 
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }
    }

    void Update() 
    {
        if (playerTarget == null) return;

        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance < evadeRange) 
        {
            // Comportamiento de evasión: huir del jugador si está muy cerca
            Evade();
        } 
        else if (distance < chaseRange) 
        {
            // Comportamiento de persecución estándar
            agent.SetDestination(playerTarget.position);
        }
        else
        {
            // Si está fuera de alcance, se detiene
            if (agent.hasPath)
                agent.ResetPath();
        }
    }

    void Evade() 
    {
        // Dirección de escape: posición actual menos la posición del jugador (opuesto)
        Vector3 fleeDirection = (transform.position - playerTarget.position).normalized;
        Vector3 fleePosition = transform.position + fleeDirection * escapeDistance;

        // Proyectar de forma segura la posición de escape al NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleePosition, out hit, escapeDistance, NavMesh.AllAreas)) 
        {
            agent.SetDestination(hit.position);
        }
    }

    void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, evadeRange);
    }
}