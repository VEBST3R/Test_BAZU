using UnityEngine;
using Unity.Netcode;
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float interpolationSpeed = 15f;
    private CharacterController m_CharacterController;
    private PlayerInput m_PlayerInput;
    private ThirdPersonCamera m_CameraController;
    private Vector3 m_PlayerVelocity;
    private readonly NetworkVariable<Vector3> m_NetworkPosition = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<Quaternion> m_NetworkRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Vector3 m_ServerMoveDirection;
    public override void OnNetworkSpawn()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_PlayerInput = GetComponent<PlayerInput>();
        m_CameraController = GetComponent<ThirdPersonCamera>();
        if (IsServer)
        {
            m_NetworkPosition.Value = transform.position;
            m_NetworkRotation.Value = transform.rotation;
        }
        m_CharacterController.enabled = IsServer;
        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    private void Update()
    {
        if (IsOwner)
        {
            ClientInput();
        }
        if (!IsServer || !IsHost)
        {
            InterpolateToNetworkState();
        }
    }
    private void FixedUpdate()
    {
        if (IsServer)
        {
            ServerMove();
        }
    }
    private void ClientInput()
    {
        Vector2 input = m_PlayerInput.MoveInput;
        Vector3 moveDirection = Vector3.zero;
        if (input.magnitude > 0.1f)
        {
            Vector3 cameraForward = m_CameraController != null ? m_CameraController.GetCameraForward() : Vector3.forward;
            Vector3 cameraRight = m_CameraController != null ? m_CameraController.GetCameraRight() : Vector3.right;
            moveDirection = (cameraForward * input.y + cameraRight * input.x).normalized;
        }
        SubmitMovementServerRpc(moveDirection);
    }
    private void InterpolateToNetworkState()
    {
        transform.position = Vector3.Lerp(transform.position, m_NetworkPosition.Value, interpolationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, m_NetworkRotation.Value, interpolationSpeed * Time.deltaTime);
    }
    [ServerRpc]
    private void SubmitMovementServerRpc(Vector3 moveDirection)
    {
        m_ServerMoveDirection = moveDirection;
    }
    private void ServerMove()
    {
        if (m_CharacterController.isGrounded && m_PlayerVelocity.y < 0)
        {
            m_PlayerVelocity.y = -1f;
        }

        Vector3 horizontalVelocity = m_ServerMoveDirection * moveSpeed;

        m_PlayerVelocity.y += Physics.gravity.y * Time.fixedDeltaTime;

        Vector3 finalVelocity = horizontalVelocity + new Vector3(0, m_PlayerVelocity.y, 0);

        m_CharacterController.Move(finalVelocity * Time.fixedDeltaTime);

        if (m_ServerMoveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(m_ServerMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        m_NetworkPosition.Value = transform.position;
        m_NetworkRotation.Value = transform.rotation;
    }
}
