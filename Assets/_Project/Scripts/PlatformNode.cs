using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlatformNode : NetworkBehaviour
{
    public enum PlatformState { Idle, Warning, Falling }
    
    [Header("Configuración Visual")]
    [SerializeField] private Renderer platformRenderer;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private Color[] platformColors;
    private Color originalColor;

    [Header("Configuración de Físicas")]
    [SerializeField] private Rigidbody rb;
    
    // Sincroniza el estado en la red. Cuando cambie en el servidor, cambiará en todos los clientes de forma automática.
    private NetworkVariable<PlatformState> currentState = new NetworkVariable<PlatformState>(PlatformState.Idle);

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (platformRenderer == null) platformRenderer = GetComponent<Renderer>();

        // Guardamos el color original (ej. el material de madera desgastada)
        if (platformRenderer != null)
        {
            // Si pusimos colores en la lista, escoge uno al azar
            if (platformColors.Length > 0)
            {
                originalColor = platformColors[Random.Range(0, platformColors.Length)];
                platformRenderer.material.color = originalColor;
            }
            else
            {
                // Si la lista está vacía, guarda el color que ya tenía
                originalColor = platformRenderer.material.color;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        // Nos suscribimos al evento de cambio de valor para actualizar lo visual en los clientes
        currentState.OnValueChanged += OnStateChanged;
        ResetPlatform();
    }

    public override void OnNetworkDespawn()
    {
        // Limpieza de eventos al destruir el objeto
        currentState.OnValueChanged -= OnStateChanged;
    }

    // Este método se ejecuta en todos los jugadores (Servidor y Clientes) cuando cambia currentState
    private void OnStateChanged(PlatformState oldState, PlatformState newState)
    {
        switch (newState)
        {
            case PlatformState.Idle:
                platformRenderer.material.color = originalColor;
                break;
            case PlatformState.Warning:
                // Aquí cambia al color de alerta. Puedes expandir esto para activar un sistema de partículas de chispas.
                platformRenderer.material.color = warningColor;
                break;
            case PlatformState.Falling:
                // Visualmente no requiere cambios drásticos, las físicas del servidor se sincronizan solas.
                break;
        }
    }

    // Este método SOLO puede ser ejecutado por el Servidor/Host
    public void TriggerCollapse(float warningDuration)
    {
        if (!IsServer) return;
        StartCoroutine(CollapseSequence(warningDuration));
    }

    private IEnumerator CollapseSequence(float warningDuration)
{
    // 1. Cambiar a estado de advertencia
    currentState.Value = PlatformState.Warning;
    yield return new WaitForSeconds(warningDuration);

    // 2. Cambiar a estado de caída
    currentState.Value = PlatformState.Falling;
    
    // OJO: Agrega esta línea para soltar la plataforma en la raíz de la escena
    transform.SetParent(null); 
    
    // Ahora las físicas funcionarán sin restricciones del padre
    rb.isKinematic = false; 
}

    private void ResetPlatform()
    {
        if (IsServer)
        {
            rb.isKinematic = true;
            transform.rotation = Quaternion.identity;
            currentState.Value = PlatformState.Idle;
        }
    }
}
