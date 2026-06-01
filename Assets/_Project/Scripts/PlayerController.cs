using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    // NetworkVariable para la puntuación (Solo escritura por parte del Servidor, lectura para todos)
    public NetworkVariable<int> Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // NUEVO: Variable para el logro "Acróbata Intocable"
    public NetworkVariable<bool> hasBeenPushed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Evento local para actualizar la UI sin depender del Update
    public static event Action OnScoreChanged;

    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Detección de Suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Mecánica de Empuje")]
    [SerializeField] private float pushForce = 15f; // Qué tan lejos vuela el rival
    [SerializeField] private float pushCooldown = 3f; // Segundos de recarga
    [SerializeField] private float pushRadius = 1.5f; // Área de impacto del golpe
    private float lastPushTime = -10f; // Control interno del tiempo

    private Rigidbody rb;
    private bool isGrounded;
    private Vector2 moveInput;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction pushAction; // Nueva acción para empujar

    private Unity.Cinemachine.CinemachineCamera playerCam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        jumpAction = new InputAction("Jump", binding: "<Gamepad>/buttonSouth");
        // Conectamos el nuevo botón de empuje
        pushAction = new InputAction("Push", binding: "<Gamepad>/buttonWest");

        jumpAction.performed += context => TryJump();
        pushAction.performed += context => TryPush(); // Suscribimos la función de empuje
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        pushAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        pushAction.Disable();
        jumpAction.performed -= context => TryJump();
        pushAction.performed -= context => TryPush();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Score.OnValueChanged += (int previousValue, int newValue) =>
        {
            OnScoreChanged?.Invoke();
        };
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Score.OnValueChanged -= (int previousValue, int newValue) =>
        {
            OnScoreChanged?.Invoke();
        };
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (playerCam == null)
        {
            playerCam = UnityEngine.Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
            if (playerCam != null)
            {
                playerCam.Follow = this.transform;
            }
        }

        moveInput = moveAction.ReadValue<Vector2>();
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        Vector3 targetVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, 15f * Time.fixedDeltaTime);

        Vector3 lookDirection = new Vector3(moveInput.x, 0, moveInput.y);
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.fixedDeltaTime);
        }
    }

    private void TryJump()
    {
        if (!IsOwner) return;
        if (isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    // --- LÓGICA DE EMPUJE ---
    private void TryPush()
    {
        if (!IsOwner) return;

        // Comprobamos si ya pasaron los 3 segundos de recarga
        if (Time.time - lastPushTime < pushCooldown)
        {
            Debug.Log("Empuje recargando... faltan: " + (pushCooldown - (Time.time - lastPushTime)).ToString("F1") + "s");
            return;
        }

        // Reiniciamos el cronómetro
        lastPushTime = Time.time;

        // Creamos una esfera invisible frente al jugador para detectar a quién golpeamos
        Vector3 pushCenter = transform.position + transform.forward * 1f;
        Collider[] hits = Physics.OverlapSphere(pushCenter, pushRadius);

        foreach (var hit in hits)
        {
            // Verificamos que sea un jugador y que no seamos nosotros mismos
            if (hit.CompareTag("Player") && hit.gameObject != this.gameObject)
            {
                if (hit.TryGetComponent<NetworkObject>(out NetworkObject targetNetObj))
                {
                    // 1. Calculamos la dirección (desde mí hacia él)
                    Vector3 pushDirection = (hit.transform.position - transform.position);

                    // 2. Anulamos por completo el eje Y (vertical) para que no lo levante
                    pushDirection.y = 0f;

                    // 3. Normalizamos el vector (esto asegura que el empuje sea igual de fuerte sin importar la distancia)
                    pushDirection = pushDirection.normalized;

                    // Le enviamos la orden al servidor
                    RequestPushServerRpc(targetNetObj.NetworkObjectId, pushDirection);
                    break; // Solo empujamos a la primera persona que toquemos por clic
                }
            }
        }
    }

    // 1. El Cliente le avisa al Servidor
    [ServerRpc]
    private void RequestPushServerRpc(ulong targetNetworkObjectId, Vector3 direction)
    {
        // El Servidor busca al jugador que recibió el golpe
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject))
        {
            if (targetObject.TryGetComponent<PlayerController>(out PlayerController targetController))
            {
                // Configuramos el mensaje para que SOLO le llegue al jugador afectado
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetObject.OwnerClientId } }
                };

                // El servidor le da la orden a ese celular de salir volando
                targetController.ApplyPushClientRpc(direction, clientRpcParams);

                // NUEVO: Puntuación por empuje exitoso
                Score.Value += 5;
            }
        }
    }

    // Método de utilidad para sumar puntos por supervivencia
    public void AddSurvivalPoints(int points)
    {
        if (IsServer)
        {
            Score.Value += points;
        }
    }

    // 2. El celular de la víctima recibe la orden y aplica la fuerza
    [ClientRpc]
    private void ApplyPushClientRpc(Vector3 direction, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return; // Verificamos por seguridad que sea mi propio personaje

        // Registramos que el jugador recibió un impacto para anular el logro
        hasBeenPushed.Value = true;

        // Detenemos cualquier movimiento previo para que el golpe se sienta en seco
        rb.linearVelocity = Vector3.zero;

        // ¡Salimos volando!
        rb.AddForce(direction * pushForce, ForceMode.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Dibuja una esfera roja en el editor de Unity para que veas hasta dónde llega tu golpe
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1f, pushRadius);
    }
}