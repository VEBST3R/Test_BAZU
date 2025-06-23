using UnityEngine;
using Unity.Netcode;
public class RangedEnemy : BaseEnemy
{
    [Header("Ranged Attack Settings")]
    [SerializeField] private float rangedAttackRange = 10f;
    [SerializeField] private float fireRate = 2.5f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    private float fireCooldown = 0f;
    public override float AttackRange => rangedAttackRange;
    protected override void ServerOnNetworkSpawn()
    {
        base.ServerOnNetworkSpawn();
    }
    protected override void HandleAttackingState()
    {
        if (currentTarget == null)
        {
            SetState(EnemyState.Chasing);
            return;
        }
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget <= AttackRange)
        {
            fireCooldown -= Time.deltaTime;
            if (fireCooldown <= 0f)
            {
                Fire();
                fireCooldown = fireRate;
            }
        }
        else
        {
            SetState(EnemyState.Chasing);
        }
    }
    private void Fire()
    {
        if (projectilePrefab == null || firePoint == null) return;
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        projectile.GetComponent<NetworkObject>().Spawn();
    }
}
