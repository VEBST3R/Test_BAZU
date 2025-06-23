using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyTargeting))]
public abstract class BaseEnemy : NetworkBehaviour
{
    [Header("Base Settings")]
    public float detectionRange = 15f;
    public float patrolRadius = 10f;
    public float patrolWaitTime = 3f;
    protected NetworkVariable<EnemyState> currentState = new NetworkVariable<EnemyState>(EnemyState.Patrolling);
    public enum EnemyState
    {
        Patrolling,
        Chasing, Attacking
    }
    protected Transform currentTarget;
    private EnemyMovement enemyMovement;

    public abstract float AttackRange { get; }
    protected abstract void HandleAttackingState();
    protected virtual void ServerOnNetworkSpawn() { }

    private void Awake()
    {
        enemyMovement = GetComponent<EnemyMovement>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (IsServer)
        {
            currentState.Value = EnemyState.Patrolling;
            ServerOnNetworkSpawn();
        }
        currentState.OnValueChanged += OnStateChanged;
        OnStateChanged(EnemyState.Patrolling, currentState.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentState.OnValueChanged -= OnStateChanged;
    }

    void Update()
    {
        if (!IsServer) return;
        if (currentState.Value == EnemyState.Attacking)
        {
            HandleAttackingState();
        }
    }

    public void SetState(EnemyState newState)
    {
        if (!IsServer || currentState.Value == newState) return;
        currentState.Value = newState;
        if (newState == EnemyState.Patrolling)
        {
            SetTarget(null);
            enemyMovement?.SetNewPatrolDestination();
        }
    }

    public void SetTarget(Transform newTarget)
    {
        if (!IsServer) return;
        currentTarget = newTarget;
    }

    public EnemyState GetCurrentState()
    {
        return currentState.Value;
    }

    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }

    private void OnStateChanged(EnemyState oldState, EnemyState newState)
    {
        if (!IsClient) return;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;
        switch (newState)
        {
            case EnemyState.Patrolling:
                renderer.material.color = Color.green;
                break;
            case EnemyState.Chasing:
                renderer.material.color = Color.yellow;
                break;
            case EnemyState.Attacking:
                renderer.material.color = Color.red;
                break;
        }
    }
}
