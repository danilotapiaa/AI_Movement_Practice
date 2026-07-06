using UnityEngine;
using UnityEngine.UI;

public class StateDebugger : MonoBehaviour
{
    public Text stateText;
    public FSM_Agent agent;

    void Update()
    {
        if (agent != null && stateText != null)
        {
            // Muestra el estado actual del agente en la interfaz
            stateText.text = $"State: {agent.currentState}";
        }
    }
}