using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
public class NetworkConnectionManager : MonoBehaviour
{
    public event Action<string> OnStatusUpdate;
    public event Action<bool, bool> OnConnectionStateChanged;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        NetworkManager.Singleton.OnTransportFailure += HandleTransportFailure;
    }
    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        NetworkManager.Singleton.OnTransportFailure -= HandleTransportFailure;
    }
    public void StartHost()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (IsPortInUse(transport.ConnectionData.Port))
        {
            OnStatusUpdate?.Invoke("Port is already in use.");
            return;
        }
        OnStatusUpdate?.Invoke("Starting Host...");
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (!IsPortInUse(transport.ConnectionData.Port))
        {
            OnStatusUpdate?.Invoke("Host not found. Port is not in use.");
            OnConnectionStateChanged?.Invoke(false, false);
            return;
        }

        OnStatusUpdate?.Invoke("Connecting...");
        NetworkManager.Singleton.StartClient();
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            bool isHost = NetworkManager.Singleton.IsHost;
            OnStatusUpdate?.Invoke(isHost ? "Host started." : "Connected to host.");
            OnConnectionStateChanged?.Invoke(true, isHost);
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            OnStatusUpdate?.Invoke("Disconnected.");
            OnConnectionStateChanged?.Invoke(false, false);
        }
    }

    private void HandleTransportFailure()
    {
        OnStatusUpdate?.Invoke("Failed to connect. Host not found or address is incorrect.");
        OnConnectionStateChanged?.Invoke(false, false);
    }

    private bool IsPortInUse(ushort port)
    {
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
        var udpConnInfoArray = ipGlobalProperties.GetActiveUdpListeners();
        if (tcpConnInfoArray.Any(l => l.Port == port)) return true;
        if (udpConnInfoArray.Any(l => l.Port == port)) return true;
        return false;
    }
}
