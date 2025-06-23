using UnityEngine;
using Unity.Netcode;
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public int damage = 15;
    public float lifeTime = 3f;
    private Rigidbody rb;
    private bool hasHit = false;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        rb = GetComponent<Rigidbody>();
        if (IsServer)
        {
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * speed;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            Invoke(nameof(DestroyProjectile), lifeTime);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || hasHit) return; if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamageServerRpc(damage);
                hasHit = true;
                DestroyProjectile();
                return;
            }
        }
        if (other.CompareTag("Enemy") || other.GetComponent<RangedEnemy>() != null)
        {
            return;
        }
        if (other.GetComponent<Projectile>() != null)
        {
            return;
        }
        hasHit = true;
        DestroyProjectile();
    }
    private void DestroyProjectile()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}
