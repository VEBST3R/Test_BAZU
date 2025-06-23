using UnityEngine;
using Unity.Netcode;
using System.Collections;
[RequireComponent(typeof(PlayerHealth))]
public class PlayerFeedback : NetworkBehaviour
{
    public Color deathColor = Color.black;
    public Color originalColor = Color.gray;
    private PlayerHealth m_PlayerHealth;
    private Renderer m_Renderer;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        m_PlayerHealth = GetComponent<PlayerHealth>();
        m_Renderer = GetComponentInChildren<Renderer>();
        m_PlayerHealth.OnPlayerDied += OnPlayerDied;
        m_PlayerHealth.OnPlayerRespawned += OnPlayerRespawned;
        m_PlayerHealth.OnDamageTaken += OnDamageTaken;
        if (m_Renderer != null)
        {
            m_Renderer.material.color = originalColor;
        }
    }
    private void OnDamageTaken(int damage)
    {
        StartCoroutine(DamageFlash());
    }
    private void OnPlayerDied()
    {
        if (m_Renderer != null)
        {
            m_Renderer.material.color = deathColor;
        }
    }
    private void OnPlayerRespawned()
    {
        if (m_Renderer != null)
        {
            m_Renderer.material.color = originalColor;
        }
    }
    private IEnumerator DamageFlash()
    {
        yield return new WaitForSeconds(0.1f);
    }
    private void OnGUI()
    {
        if (!IsOwner || m_PlayerHealth == null) return;
        GUI.Label(new Rect(10, 10, 200, 30), $"Health: {m_PlayerHealth.CurrentHealth}/{m_PlayerHealth.maxHealth}");
        if (!m_PlayerHealth.IsAlive)
        {
            GUI.Label(new Rect(10, 40, 200, 30), "DEAD - Respawning...");
        }
    }
    public override void OnNetworkDespawn()
    {
        if (m_PlayerHealth != null)
        {
            m_PlayerHealth.OnPlayerDied -= OnPlayerDied;
            m_PlayerHealth.OnPlayerRespawned -= OnPlayerRespawned;
            m_PlayerHealth.OnDamageTaken -= OnDamageTaken;
        }
        base.OnNetworkDespawn();
    }
}
