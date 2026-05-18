using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Detección de Suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector2 moveInput;

    // Referencias directas al nuevo Input System
    private InputAction moveAction;
    private InputAction jumpAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Creamos las acciones programáticamente para conectarlas directo a los botones de la pantalla
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        jumpAction = new InputAction("Jump", binding: "<Gamepad>/buttonSouth");

        // Nos suscribimos al evento de presionar el botón de salto
        jumpAction.performed += context => TryJump();
    }

    public override void OnNetworkSpawn()
    {
        // LA MAGIA: Verificamos si este jugador nos pertenece a nosotros (nuestra pantalla)
        if (IsOwner)
        {
            // 1. Buscamos la cámara de Cinemachine 3.x que pusimos en la escena
            var cinemachineCamera = Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
            
            if (cinemachineCamera != null)
            {
                // 2. Le asignamos nuestro propio cuerpo (transform) como el objetivo a seguir
                cinemachineCamera.Follow = this.transform;
                
                // Opcional: También podemos hacer que lo mire directamente
                cinemachineCamera.LookAt = this.transform;
            }
            else
            {
                Debug.LogWarning("¡El jugador no encontró la cámara de Cinemachine en la escena!");
            }
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        jumpAction.performed -= context => TryJump();
    }

    private void Update()
    {
        // LA REGLA DE ORO: Solo procesamos inputs si este jugador nos pertenece en la red
        if (!IsOwner) return;

        // Leer el valor del joystick en pantalla
        moveInput = moveAction.ReadValue<Vector2>();

        // Crear una pequeña esfera invisible en los pies para verificar si tocamos la capa "Suelo"
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        // Movimiento físico: Modificamos la velocidad directamente para un control ágil
        // Mantenemos la velocidad en Y intacta para que la gravedad y los saltos funcionen
        Vector3 targetVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);
        
        // Interpolación suave para que el personaje no arranque/pare de golpe de forma robótica
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, 15f * Time.fixedDeltaTime);

        // Opcional: Hacer que el personaje mire hacia donde camina
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
            // Aplicamos un impulso hacia arriba
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // Resetea velocidad Y para saltos consistentes
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    // Para visualizar la esfera del GroundCheck en el editor de Unity
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}