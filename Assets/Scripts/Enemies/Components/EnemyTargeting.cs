using UnityEngine;
using Unity.Netcode;
[RequireComponent(typeof(BaseEnemy))]
public class EnemyTargeting : NetworkBehaviour
{
    private BaseEnemy enemy;
    private float targetSearchCooldown;
    private const float TARGET_SEARCH_INTERVAL = 0.5f;
    private void Awake()
    {
        enemy = GetComponent<BaseEnemy>();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
        }
    }
    private void Update()
    {
        targetSearchCooldown -= Time.deltaTime;
        if (targetSearchCooldown <= 0f)
        {
            CheckForPlayers();
            targetSearchCooldown = TARGET_SEARCH_INTERVAL;
        }
    }
    private void CheckForPlayers()
    {
        Transform closestPlayer = FindClosestPlayer();
        if (closestPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, closestPlayer.position);
            enemy.SetTarget(closestPlayer);
            if (distanceToPlayer <= enemy.AttackRange)
            {
                enemy.SetState(BaseEnemy.EnemyState.Attacking);
            }
            else
            {
                enemy.SetState(BaseEnemy.EnemyState.Chasing);
            }
        }
        else
        {
            if (enemy.GetCurrentState() != BaseEnemy.EnemyState.Patrolling)
            {
                enemy.SetTarget(null);
                enemy.SetState(BaseEnemy.EnemyState.Patrolling);
            }
        }
    }
    private Transform FindClosestPlayer()
    {
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        Transform closestTarget = null;
        float closestDistance = float.MaxValue;
        foreach (var player in players)
        {
            if (player.IsAlive)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = player.transform;
                }
            }
        }
        if (closestTarget != null && closestDistance <= enemy.detectionRange)
        {
            return closestTarget;
        }
        return null;
    }
}
