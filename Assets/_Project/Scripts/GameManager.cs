using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance; // Singleton para conectarlo rápido con el Trigger

    [Header("Referencias")]
    [SerializeField] private ArenaGenerator arenaGenerator;

    [Header("Configuración del Colapso")]
    [SerializeField] private float initialDelay = 5f;
    [SerializeField] private float timeBetweenCollapses = 2.5f;
    [SerializeField] private float minimumTime = 0.5f;
    [SerializeField] private float difficultyMultiplier = 0.1f;

    private bool isMatchActive = false;
    private int playersAlive = 0;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Contamos cuántos jugadores hay en la red al iniciar
            playersAlive = NetworkManager.Singleton.ConnectedClients.Count;
            StartCoroutine(MatchRoutine());
        }
    }

    private IEnumerator MatchRoutine()
    {
        Debug.Log("Esperando a que se una el segundo jugador...");

        // 1. Fase de Sala de Espera: El bucle se repite cada segundo hasta que detecta 2 o más jugadores
        while (NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            yield return new WaitForSeconds(1f);
        }

        // 2. ¡Ya llegaron! Actualizamos nuestra variable de vivos
        playersAlive = NetworkManager.Singleton.ConnectedClients.Count;

        Debug.Log("¡Jugadores completos! La partida comenzará en " + initialDelay + " segundos...");

        // 3. Fase de Preparación (Cuenta regresiva)
        yield return new WaitForSeconds(initialDelay);

        isMatchActive = true;
        Debug.Log("¡El suelo es lava!");

        float currentWaitTime = timeBetweenCollapses;

        // 4. Bucle principal del juego
        while (isMatchActive && playersAlive > 1)
        {
            yield return new WaitForSeconds(currentWaitTime);

            if (arenaGenerator != null)
            {
                arenaGenerator.CollapseRandomPlatform();
            }

            if (currentWaitTime > minimumTime)
            {
                currentWaitTime -= difficultyMultiplier;
            }
        }
    }

    // El LavaTrigger llamará a esta función cuando alguien caiga
    public void PlayerEliminated(ulong clientId)
    {
        if (!IsServer) return;

        playersAlive--;
        Debug.Log($"Jugador eliminado. Quedan {playersAlive} vivos.");

        if (playersAlive <= 1 && isMatchActive)
        {
            // NUEVO: Le pasamos a EndMatch el ID del jugador que acaba de morir
            EndMatch(clientId);
        }
    }

    // NUEVO: Ahora la función recibe el ID del perdedor
    public void EndMatch(ulong loserId)
    {
        if (!IsServer) return;

        isMatchActive = false;
        ulong winnerId = 9999;

        if (playersAlive == 1)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                // NUEVO: Si este cliente de la lista NO es el perdedor, entonces es el ganador
                if (client.ClientId != loserId)
                {
                    winnerId = client.ClientId;
                    break;
                }
            }
        }

        if (ArenaUIManager.Instance != null)
        {
            ArenaUIManager.Instance.ShowGameOverClientRpc(winnerId);
        }
    }
}
    
