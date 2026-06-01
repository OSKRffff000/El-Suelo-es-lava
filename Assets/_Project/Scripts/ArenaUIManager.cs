using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ArenaUIManager : NetworkBehaviour
{
    public static ArenaUIManager Instance;

    [Header("Elementos de UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    
    [Header("Logros")]
    [SerializeField] private GameObject achievementPanel;

    private void Awake()
    {
        Instance = this;
    }

    // Ahora recibimos el ID del ganador y si desbloqueó el logro
    [ClientRpc]
    public void ShowGameOverClientRpc(ulong winnerId, bool showUntouchableAchievement = false)
    {
        gameOverPanel.SetActive(true);

        // Comparamos el ID para saber qué mensaje mostrarle al jugador de esta pantalla
        if (winnerId == 9999) // 9999 es nuestro código secreto para "Empate"
        {
            winnerText.text = "¡Empate! Todos cayeron.";
        }
        else if (NetworkManager.Singleton.LocalClientId == winnerId)
        {
            // Si mi ID local es igual al ID del ganador
            winnerText.text = "¡Sobreviviste!";
            winnerText.color = Color.green; // Opcional: ponerlo verde
            
            // Mostrar logro si se cumplió la condición
            if (showUntouchableAchievement && achievementPanel != null)
            {
                achievementPanel.SetActive(true);
            }
        }
        else
        {
            // Si mi ID es diferente
            winnerText.text = "¡Te fundiste en la lava!";
            winnerText.color = Color.red; // Opcional: ponerlo rojo
        }
    }

    public void DisconnectAndReturn()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(0);
    }
}