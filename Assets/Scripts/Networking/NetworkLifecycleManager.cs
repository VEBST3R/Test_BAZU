using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
public class NetworkLifecycleManager : MonoBehaviour
{
    private readonly Dictionary<ulong, NetworkObject> m_ClientPlayerObjects = new Dictionary<ulong, NetworkObject>();
    private void Start()
    {
        StartCoroutine(RegisterCallbacksWhenReady());
    }
    private IEnumerator RegisterCallbacksWhenReady()
    {
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            StartCoroutine(TrackPlayerObject(clientId));
        }
    }
    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (m_ClientPlayerObjects.TryGetValue(clientId, out NetworkObject playerObjectToDespawn) && playerObjectToDespawn != null)
            {
                if (playerObjectToDespawn.IsSpawned)
                {
                    playerObjectToDespawn.Despawn(true);
                }
            }
            m_ClientPlayerObjects.Remove(clientId);
        }
    }
    private IEnumerator TrackPlayerObject(ulong clientId)
    {
        const float timeout = 3.0f;
        var endTime = Time.time + timeout;
        NetworkObject playerObject = null;
        while (Time.time < endTime)
        {
            if (NetworkManager.Singleton.SpawnManager != null)
            {
                playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            }
            if (playerObject != null)
            {
                m_ClientPlayerObjects[clientId] = playerObject;
                yield break;
            }
            yield return null;
        }
    }
}
