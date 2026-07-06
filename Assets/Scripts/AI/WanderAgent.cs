using UnityEngine;
using UnityEngine.AI;

public class WanderAgent : MonoBehaviour
{
    [Header("Wander Configuration")]
    public float wanderRadius = 10f;       // Radio de búsqueda para el nuevo destino
    public float wanderTimer = 3f;         // Tiempo máximo entre cambios de destino
    public float stoppingDistance = 0.5f;   // Distancia para considerar que llegó

    private NavMeshAgent agent;
    private float timer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Si el agente llegó al destino o se agotó el temporizador, busca otro punto
        if (timer >= wanderTimer || agent.remainingDistance < stoppingDistance)
        {
            SetRandomDestination();
            timer = 0f;
        }
    }

    void SetRandomDestination()
    {
        // Generar un punto aleatorio dentro del radio establecido
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        // Proyectar de forma segura el punto generado al NavMesh para comprobar si es caminable
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // Dibuja un círculo verde en el editor que representa el rango de deambulación
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}