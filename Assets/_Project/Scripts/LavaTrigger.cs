using Unity.Netcode;
using UnityEngine;

public class LavaTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Al ser MonoBehaviour, usamos el Singleton global para verificar si somos el Servidor/Host
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        // Comprobamos si el objeto que cruzó el trigger tiene un NetworkObject
        if (other.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            // Verificamos si es una de nuestras plataformas
            if (other.GetComponent<PlatformNode>() != null)
            {
                Debug.Log($"Plataforma {other.name} eliminada por la lava de forma segura.");
                netObj.Despawn(); // Despawn elimina el objeto de la red en todos los clientes y lo destruye
            }
            // Verificamos si es un jugador
            else if (other.CompareTag("Player"))
            {
                Debug.Log($"El jugador {netObj.OwnerClientId} ha tocado la lava.");

                // Le avisamos al GameManager que reste a este jugador (Solo en el servidor)
                if (NetworkManager.Singleton.IsServer && GameManager.Instance != null)
                {
                    GameManager.Instance.PlayerEliminated(netObj.OwnerClientId);
                }

                netObj.Despawn();
            }
        }
    }
}