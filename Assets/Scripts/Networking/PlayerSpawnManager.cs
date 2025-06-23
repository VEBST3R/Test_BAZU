using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class PlayerSpawnManager : NetworkBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [Header("Player Spawn Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;
    public Vector3 defaultSpawnPosition = Vector3.zero;
    private readonly HashSet<ulong> m_SpawnedPlayers = new HashSet<ulong>();
    private int m_NextSpawnPointIndex = 0;
    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!IsServer) return; if (playerPrefab == null)
        {
            enabled = false;
            return;
        }
        if (playerPrefab.GetComponent<NetworkObject>() == null)
        {
            enabled = false;
            return;
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        SpawnExistingClients();
    }
    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    private void OnClientConnected(ulong clientId)
    {
        SpawnPlayerForClient(clientId);
    }
    private void OnClientDisconnected(ulong clientId)
    {
        if (m_SpawnedPlayers.Contains(clientId))
        {
            m_SpawnedPlayers.Remove(clientId);
        }
    }
    private void SpawnExistingClients()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            SpawnPlayerForClient(client.ClientId);
        }
    }
    private void SpawnPlayerForClient(ulong clientId)
    {
        if (m_SpawnedPlayers.Contains(clientId)) return;
        Transform spawnPoint = GetNextSpawnPoint();
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        var networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);
        m_SpawnedPlayers.Add(clientId);
    }
    public Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            var defaultTransform = new GameObject("DefaultSpawnPoint").transform;
            defaultTransform.position = defaultSpawnPosition;
            defaultTransform.rotation = Quaternion.identity;
            return defaultTransform;
        }
        Transform spawnPoint = spawnPoints[m_NextSpawnPointIndex];
        m_NextSpawnPointIndex = (m_NextSpawnPointIndex + 1) % spawnPoints.Length;
        return spawnPoint;
    }
}
