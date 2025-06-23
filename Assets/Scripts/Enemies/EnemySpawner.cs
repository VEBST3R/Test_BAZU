using UnityEngine;
using Unity.Netcode;
using System.Collections;
public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public GameObject rangedEnemyPrefab;
    public int maxEnemies = 10;
    public float spawnInterval = 3f;
    public float spawnRadius = 20f;
    [Header("Spawn Sequence")]
    [Tooltip("How many normal enemies to spawn before one ranged enemy.")]
    public int normalEnemiesPerRanged = 3;
    [Header("Spawn Area")]
    public Transform[] spawnPoints;
    public LayerMask groundLayer = 1;
    private NetworkVariable<int> networkEnemyCount = new NetworkVariable<int>(0);
    private int spawnSequenceCounter = 0;
    private Coroutine spawnCoroutine;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            StartSpawning();
        }
    }
    void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnEnemiesRoutine());
        }
    }
    void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
    IEnumerator SpawnEnemiesRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (networkEnemyCount.Value < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }
    void SpawnEnemy()
    {
        GameObject prefabToSpawn; if (spawnSequenceCounter < normalEnemiesPerRanged)
        {
            prefabToSpawn = enemyPrefab;
        }
        else
        {
            prefabToSpawn = rangedEnemyPrefab;
        }
        if (prefabToSpawn == null)
        {
            prefabToSpawn = enemyPrefab;
            if (prefabToSpawn == null)
            {
                return;
            }
        }
        Vector3 spawnPosition = GetRandomSpawnPosition();
        if (spawnPosition != Vector3.zero)
        {
            GameObject enemyInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            NetworkObject networkObject = enemyInstance.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn(true);
                networkEnemyCount.Value++;
                StartCoroutine(MonitorEnemyDestruction(networkObject));
                if (prefabToSpawn == rangedEnemyPrefab)
                {
                    spawnSequenceCounter = 0;
                }
                else
                {
                    spawnSequenceCounter++;
                }
            }
            else
            {
                Destroy(enemyInstance);
            }
        }
    }
    System.Collections.IEnumerator MonitorEnemyDestruction(NetworkObject enemyNetworkObject)
    {
        while (enemyNetworkObject != null && enemyNetworkObject.IsSpawned)
        {
            yield return new WaitForSeconds(0.5f);
        }
        if (IsServer)
        {
            networkEnemyCount.Value = Mathf.Max(0, networkEnemyCount.Value - 1);
        }
    }
    Vector3 GetRandomSpawnPosition()
    {
        Vector3 spawnPosition = Vector3.zero; if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPosition = randomSpawnPoint.position;
        }
        else
        {
            Vector3 randomDirection = Random.insideUnitSphere * spawnRadius;
            randomDirection.y = 0;
            Vector3 targetPosition = transform.position + randomDirection;
            RaycastHit hit;
            if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayer))
            {
                spawnPosition = hit.point + Vector3.up * 0.5f;
            }
            else
            {
                spawnPosition = transform.position;
            }
        }
        if (IsPositionValidForSpawn(spawnPosition))
        {
            return spawnPosition;
        }
        return Vector3.zero;
    }
    bool IsPositionValidForSpawn(Vector3 position)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.PlayerObject != null)
            {
                if (Vector3.Distance(position, client.PlayerObject.transform.position) < 3f)
                {
                    return false;
                }
            }
        }
        return true;
    }
    [ServerRpc(RequireOwnership = false)]
    public void SpawnEnemyManuallyServerRpc()
    {
        if (IsServer && networkEnemyCount.Value < maxEnemies)
        {
            SpawnEnemy();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void ClearAllEnemiesServerRpc()
    {
        if (!IsServer) return;
        var enemies = FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.IsSpawned)
            {
                enemy.NetworkObject.Despawn(true);
            }
        }
        spawnSequenceCounter = 0;
        networkEnemyCount.Value = 0;
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            StopSpawning();
        }
        base.OnNetworkDespawn();
    }
}
