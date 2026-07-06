using UnityEngine;
using UnityEngine.AI;

public class ChasePlayer : MonoBehaviour
{
    [Header("Chase Configuration")]
    public Transform playerTarget;
    public float chaseRange = 15f;
    public float attackRange = 2f;
    public float updatePathInterval = 0.5f;

    private NavMeshAgent agent;
    private float pathUpdateTimer;

    void Start() 
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (playerTarget == null) 
        {
            // Busca automáticamente un objeto con el Tag "Player" si no se asignó manualmente
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }
    }

    void Update() 
    {
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // Optimización: Actualizar la ruta periódicamente en lugar de cada frame
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= updatePathInterval)
        {
            pathUpdateTimer = 0f;

            if (distance <= chaseRange)
            {
                agent.SetDestination(playerTarget.position);
            } 
            else 
            {
                // Si el jugador escapa del rango, detiene el recorrido actual
                if (agent.hasPath)
                    agent.ResetPath();
            }
        }

        // Simulación de detección del rango de ataque
        if (distance <= attackRange) 
        {
            Debug.Log("Enemy attacking player!");
        }
    }

    // Visualizar los rangos de control (amarillo para perseguir, rojo para atacar)
    void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}