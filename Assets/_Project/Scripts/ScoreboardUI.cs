using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Linq;

public class ScoreboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreboardText;

    private void OnEnable()
    {
        // Nos suscribimos a los cambios de puntuación
        PlayerController.OnScoreChanged += UpdateScoreboardUI;
    }

    private void OnDisable()
    {
        PlayerController.OnScoreChanged -= UpdateScoreboardUI;
    }

    // Este método se llamará automáticamente cada vez que la NetworkVariable de cualquier jugador cambie
    private void UpdateScoreboardUI()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        string newScoreboardText = "<b>TABLERO DE PUNTUACIONES</b>\n\n";

        // Creamos una lista temporal para ordenar a los jugadores por puntuación
        List<PlayerController> players = new List<PlayerController>();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                players.Add(client.PlayerObject.GetComponent<PlayerController>());
            }
        }

        // Ordenamos la lista de mayor a menor puntuación (LINQ)
        players = players.OrderByDescending(p => p.Score.Value).ToList();

        // Construimos el texto
        foreach (var player in players)
        {
            string playerLabel = player.IsLocalPlayer ? "<color=#FFD700>Tú</color>" : $"Jugador {player.OwnerClientId}";
            newScoreboardText += $"{playerLabel}: {player.Score.Value} pts\n";
        }

        // Actualizamos el componente TextMeshPro
        scoreboardText.text = newScoreboardText;
    }
}
