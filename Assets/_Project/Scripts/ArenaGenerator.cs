using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArenaGenerator : NetworkBehaviour
{
    [Header("Configuración del Prefab")]
    [SerializeField] private GameObject platformPrefab;

    [Header("Dimensiones de la Cuadrícula")]
    [SerializeField] private int columns = 8;
    [SerializeField] private int rows = 8;
    [SerializeField] private float spacing = 2.1f;

    [Header("Configuración de Altura")]
    // Nueva variable para decidir qué tan arriba se generará el mapa respecto a la lava
    [SerializeField] private float spawnHeight = 10f; 

    private List<GameObject> activePlatforms = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GenerateArena();
        }
    }

    private void GenerateArena()
    {
        float startX = -(columns - 1) * spacing / 2f;
        float startZ = -(rows - 1) * spacing / 2f;

        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                // Reemplazamos el '0f' por nuestra nueva variable 'spawnHeight'
                Vector3 spawnPosition = new Vector3(startX + (x * spacing), spawnHeight, startZ + (z * spacing));
                
                GameObject platformInstance = Instantiate(platformPrefab, spawnPosition, Quaternion.identity, transform);
                
                NetworkObject netObj = platformInstance.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                    activePlatforms.Add(platformInstance);
                }
            }
        }
    }

    [ContextMenu("Probar Colapso Aleatorio")]
    public void CollapseRandomPlatform()
    {
        if (!IsServer || activePlatforms.Count == 0) return;

        int randomIndex = Random.Range(0, activePlatforms.Count);
        GameObject platform = activePlatforms[randomIndex];
        
        if (platform != null)
        {
            PlatformNode node = platform.GetComponent<PlatformNode>();
            if (node != null)
            {
                node.TriggerCollapse(2.0f);
            }
            activePlatforms.RemoveAt(randomIndex);
        }
    }
}