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
    
    [Header("Animación de Advertencia")]
    [SerializeField] private float shakeIntensity = 0.05f;
    [SerializeField] private float blinkSpeed = 10f;
    private Vector3 originalPosition;
    private bool isWarning = false;
    // Sincroniza el estado en la red.
    private NetworkVariable<PlatformState> currentState = new NetworkVariable<PlatformState>(PlatformState.Idle);
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (platformRenderer == null) platformRenderer = GetComponent<Renderer>();
        if (platformRenderer != null)
        {
            if (platformColors.Length > 0)
            {
                originalColor = platformColors[Random.Range(0, platformColors.Length)];
                platformRenderer.material.color = originalColor;
            }
            else
            {
                originalColor = platformRenderer.material.color;
            }
        }
    }
    public override void OnNetworkSpawn()
    {
        currentState.OnValueChanged += OnStateChanged;
        ResetPlatform();
    }
    public override void OnNetworkDespawn()
    {
        currentState.OnValueChanged -= OnStateChanged;
    }
    private void Update()
    {
        if (isWarning)
        {
            // Temblor (Shake) - Pequeños desplazamientos aleatorios en esfera
            transform.position = originalPosition + Random.insideUnitSphere * shakeIntensity;
            
            // Parpadeo (Blink) - Interpolación ida y vuelta (PingPong)
            float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            platformRenderer.material.color = Color.Lerp(originalColor, warningColor, t);
        }
    }
    private void OnStateChanged(PlatformState oldState, PlatformState newState)
    {
        switch (newState)
        {
            case PlatformState.Idle:
                isWarning = false;
                if (oldState == PlatformState.Warning)
                {
                    transform.position = originalPosition;
                }
                platformRenderer.material.color = originalColor;
                break;
            case PlatformState.Warning:
                isWarning = true;
                originalPosition = transform.position;
                break;
            case PlatformState.Falling:
                isWarning = false;
                transform.position = originalPosition; // Restablecer al punto original antes de activar la gravedad
                platformRenderer.material.color = warningColor;
                break;
        }
    }
    public void TriggerCollapse(float warningDuration)
    {
        if (!IsServer) return;
        StartCoroutine(CollapseSequence(warningDuration));
    }
    private IEnumerator CollapseSequence(float warningDuration)
    {
        currentState.Value = PlatformState.Warning;
        yield return new WaitForSeconds(warningDuration);
        currentState.Value = PlatformState.Falling;
        
        transform.SetParent(null); 
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