using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Botones de Interfaz")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [Header("Configuración de Escena")]
    [Tooltip("El nombre exacto de la escena de la arena de juego")]
    [SerializeField] private string gameSceneName = "Game Arena";

    private void Awake()
    {
        // Asignamos la función de Crear Partida al botón Host
        hostButton.onClick.AddListener(() =>
        {
            StartHost();
        });

        // Asignamos la función de Unirse a Partida al botón Client
        clientButton.onClick.AddListener(() =>
        {
            StartClient();
        });
    }

    private void StartHost()
    {
        // Iniciamos el jugador actual como Servidor y Cliente a la vez
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host iniciado correctamente. Cargando la arena...");

            // Usamos el SceneManager de NETCODE, no el de Unity estándar.
            // Esto asegura que todos los clientes viajen juntos a la arena.
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("No se pudo iniciar el Host.");
        }
    }

    private void StartClient()
    {
        // Iniciamos el jugador actual solo como Cliente para buscar un Host local
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Conectando al Host...");
            // Ocultamos los botones mientras se conecta para evitar doble clic
            hostButton.interactable = false;
            clientButton.interactable = false;
        }
        else
        {
            Debug.LogError("No se pudo iniciar el Cliente.");
        }
    }
}