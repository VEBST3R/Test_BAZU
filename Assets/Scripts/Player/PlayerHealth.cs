using UnityEngine;
using Unity.Netcode;
public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private readonly NetworkVariable<int> m_CurrentHealth = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public event System.Action<int, int> OnHealthChanged;
    public event System.Action<int> OnDamageTaken;
    public event System.Action OnPlayerDied;
    public event System.Action OnPlayerRespawned;
    public int CurrentHealth => m_CurrentHealth.Value;
    public bool IsAlive => m_CurrentHealth.Value > 0;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            m_CurrentHealth.Value = maxHealth;
        }
        m_CurrentHealth.OnValueChanged += OnHealthValueChanged;
        OnHealthChanged?.Invoke(m_CurrentHealth.Value, maxHealth);
    }
    public override void OnNetworkDespawn()
    {
        m_CurrentHealth.OnValueChanged -= OnHealthValueChanged;
    }
    private void OnHealthValueChanged(int previousValue, int newValue)
    {
        OnHealthChanged?.Invoke(newValue, maxHealth);
        if (previousValue > 0 && newValue <= 0)
        {
            HandlePlayerDeath();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (!IsServer || !IsAlive) return;
        int newHealth = Mathf.Max(0, m_CurrentHealth.Value - damage);
        m_CurrentHealth.Value = newHealth;
        ShowDamageEffectClientRpc(damage, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } });
    }
    [ClientRpc]
    private void ShowDamageEffectClientRpc(int damage, ClientRpcParams clientRpcParams = default)
    {
        OnDamageTaken?.Invoke(damage);
    }
    private void HandlePlayerDeath()
    {
        OnPlayerDied?.Invoke();
        if (IsOwner)
        {
            var playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement != null) playerMovement.enabled = false;
            StartCoroutine(RespawnAfterDelay(5f));
        }
        HandleDeathEffectsClientRpc();
    }
    [ClientRpc]
    private void HandleDeathEffectsClientRpc()
    {
    }
    private System.Collections.IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (IsOwner)
        {
            RespawnPlayerServerRpc();
        }
    }
    [ServerRpc]
    private void RespawnPlayerServerRpc()
    {
        if (!IsServer) return;

        Transform spawnPoint = PlayerSpawnManager.Instance.GetNextSpawnPoint();
        var characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
            characterController.enabled = true;
        }
        else
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        m_CurrentHealth.Value = maxHealth;
        ResetPlayerStateClientRpc();
    }
    [ClientRpc]
    private void ResetPlayerStateClientRpc()
    {
        if (IsOwner)
        {
            var playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement != null) playerMovement.enabled = true;
        }
        OnPlayerRespawned?.Invoke();
    }
    public float GetHealthPercentage()
    {
        return (float)m_CurrentHealth.Value / maxHealth;
    }
}
