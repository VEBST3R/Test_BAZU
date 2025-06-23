using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
[RequireComponent(typeof(BaseEnemy))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : NetworkBehaviour
{
    private BaseEnemy enemy;
    private NavMeshAgent agent;
    private Vector3 originalPosition;
    private float patrolWaitTimer;
    private void Awake()
    {
        enemy = GetComponent<BaseEnemy>();
        agent = GetComponent<NavMeshAgent>();
    }
    public override void OnNetworkSpawn()
    {
        originalPosition = transform.position;
        if (!IsServer)
        {
            agent.enabled = false;
            enabled = false;
            return;
        }
        SetNewPatrolDestination();
    }
    private void Update()
    {
        switch (enemy.GetCurrentState())
        {
            case BaseEnemy.EnemyState.Patrolling:
                HandlePatrollingState();
                break;
            case BaseEnemy.EnemyState.Chasing:
                HandleChasingState();
                break;
            case BaseEnemy.EnemyState.Attacking:
                HandleAttackingState();
                break;
        }
    }
    private void HandlePatrollingState()
    {
        agent.isStopped = false;
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= enemy.patrolWaitTime)
            {
                SetNewPatrolDestination();
                patrolWaitTimer = 0f;
            }
        }
    }
    private void HandleChasingState()
    {
        Transform target = enemy.GetCurrentTarget();
        if (target != null)
        {
            agent.SetDestination(target.position);
            agent.isStopped = false;
            FaceTarget(target);
        }
        else
        {
            enemy.SetState(BaseEnemy.EnemyState.Patrolling);
        }
    }
    private void HandleAttackingState()
    {
        agent.isStopped = true;
        Transform target = enemy.GetCurrentTarget();
        if (target != null)
        {
            FaceTarget(target);
        }
    }
    private void FaceTarget(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            direction.y = 0;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * agent.angularSpeed);
        }
    }
    public void SetNewPatrolDestination()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        Vector3 randomDirection = Random.insideUnitSphere * enemy.patrolRadius;
        randomDirection += originalPosition;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, enemy.patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
