using UnityEngine;
using Unity.Netcode;
public class MeleeEnemy : BaseEnemy
{
    [Header("Melee Settings")]
    [SerializeField] private int damage = 25;
    [SerializeField] private float attackCooldown = 1.5f;
    private float lastAttackTime;
    public override float AttackRange => 2.5f;
    protected override void HandleAttackingState()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
                if (distanceToTarget <= AttackRange)
                {
                    AttackTarget();
                    lastAttackTime = Time.time;
                }
            }
        }
    }
    private void AttackTarget()
    {
        if (currentTarget == null) return;
        PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>(); if (playerHealth != null)
        {
            playerHealth.TakeDamageServerRpc(damage);
        }
    }
}
