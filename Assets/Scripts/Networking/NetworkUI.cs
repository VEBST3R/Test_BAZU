using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
public class NetworkUI : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button clientButton;
    public Button disconnectButton;
    public Button exitButton;
    public GameObject menuPanel;
    public TMP_Text statusText;
    [Header("Dependencies")]
    public NetworkConnectionManager connectionManager;
    private bool wasNetworkActive = false;
    private bool isMenuPanelVisible = true;
    private void Start()
    {
        if (hostButton != null) hostButton.onClick.AddListener(() => connectionManager.StartHost());
        if (clientButton != null) clientButton.onClick.AddListener(() => connectionManager.StartClient());
        if (disconnectButton != null) disconnectButton.onClick.AddListener(() => connectionManager.Disconnect());
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);

        if (connectionManager != null)
        {
            connectionManager.OnStatusUpdate += HandleStatusUpdate;
            connectionManager.OnConnectionStateChanged += HandleConnectionStateChanged;
        }

        UpdateUI(false, true);
        UpdateCursorState(true);
    }

    private void OnDestroy()
    {
        if (connectionManager != null)
        {
            connectionManager.OnStatusUpdate -= HandleStatusUpdate;
            connectionManager.OnConnectionStateChanged -= HandleConnectionStateChanged;
        }
    }

    private void Update()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient))
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleMenu();
            }
        }
    }

    private void HandleStatusUpdate(string status)
    {
        if (statusText != null) statusText.text = status;
    }

    private void HandleConnectionStateChanged(bool isConnected, bool isHost)
    {
        isMenuPanelVisible = !isConnected;
        UpdateUI(isConnected, isHost);
        UpdateCursorState(isMenuPanelVisible);
    }

    private void UpdateUI(bool isConnected, bool isHost)
    {
        if (menuPanel != null) menuPanel.SetActive(isMenuPanelVisible);

        if (!isConnected)
        {
            hostButton?.gameObject.SetActive(isMenuPanelVisible);
            clientButton?.gameObject.SetActive(isMenuPanelVisible);
            disconnectButton?.gameObject.SetActive(false);
            exitButton?.gameObject.SetActive(isMenuPanelVisible);
            statusText.text = "Disconnected";
        }
        else if (isMenuPanelVisible)
        {
            hostButton?.gameObject.SetActive(false);
            clientButton?.gameObject.SetActive(false);
            disconnectButton?.gameObject.SetActive(!isHost);
            exitButton?.gameObject.SetActive(true);
        }
        else
        {
            hostButton?.gameObject.SetActive(false);
            clientButton?.gameObject.SetActive(false);
            disconnectButton?.gameObject.SetActive(false);
            exitButton?.gameObject.SetActive(false);
        }
    }

    private void ToggleMenu()
    {
        isMenuPanelVisible = !isMenuPanelVisible;
        UpdateUI(NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsHost, NetworkManager.Singleton.IsHost);
        UpdateCursorState(isMenuPanelVisible);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (hostButton != null) hostButton.interactable = interactable;
        if (clientButton != null) clientButton.interactable = interactable;
    }

    private void UpdateCursorState(bool isVisible)
    {
        Cursor.visible = isVisible;
        Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void ExitGame()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient))
        {
            NetworkManager.Singleton.Shutdown();
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
