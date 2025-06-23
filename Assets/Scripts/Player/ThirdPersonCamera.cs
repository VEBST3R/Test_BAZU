using UnityEngine;
using Unity.Netcode;
[RequireComponent(typeof(PlayerInput))]
public class ThirdPersonCamera : NetworkBehaviour
{
    [Header("Camera Settings")]
    public float distance = 5f;
    public float height = 2f;
    public float mouseSensitivity = 2f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    [Header("Collision")]
    public LayerMask collisionLayers = -1;
    public float collisionBuffer = 0.3f;
    private Camera m_PlayerCamera;
    private Transform m_Target;
    private PlayerInput m_PlayerInput;
    private float m_Yaw = 0f;
    private float m_Pitch = 0f;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        m_Target = transform;
        m_PlayerInput = GetComponent<PlayerInput>();
        SetupCamera();
        m_Yaw = transform.eulerAngles.y;
        m_Pitch = 0f;
    }
    private void SetupCamera()
    {
        m_PlayerCamera = Camera.main;
        if (m_PlayerCamera == null)
        {
            m_PlayerCamera = FindFirstObjectByType<Camera>();
        }
        if (m_PlayerCamera == null)
        {
            GameObject cameraObj = new GameObject("PlayerCamera");
            m_PlayerCamera = cameraObj.AddComponent<Camera>();
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                cameraObj.AddComponent<AudioListener>();
            }
        }
        m_PlayerCamera.tag = "MainCamera";
    }
    private void LateUpdate()
    {
        if (m_Target == null || m_PlayerCamera == null || m_PlayerInput == null) return;
        Vector2 lookInput = m_PlayerInput.LookInput;
        m_Yaw += lookInput.x * mouseSensitivity;
        m_Pitch -= lookInput.y * mouseSensitivity;
        m_Pitch = Mathf.Clamp(m_Pitch, minVerticalAngle, maxVerticalAngle);
        UpdateCameraPosition();
    }
    private void UpdateCameraPosition()
    {
        Vector3 targetPos = m_Target.position + Vector3.up * height;
        Quaternion rotation = Quaternion.Euler(m_Pitch, m_Yaw, 0);
        Vector3 offset = rotation * Vector3.back * distance;
        Vector3 desiredPosition = targetPos + offset;
        Vector3 finalPosition = CheckCollision(targetPos, desiredPosition);
        m_PlayerCamera.transform.position = finalPosition;
        m_PlayerCamera.transform.LookAt(targetPos);
    }
    private Vector3 CheckCollision(Vector3 targetPos, Vector3 desiredPos)
    {
        Vector3 direction = desiredPos - targetPos;
        float desiredDistance = direction.magnitude;
        if (Physics.Raycast(targetPos, direction.normalized, out RaycastHit hit, desiredDistance, collisionLayers))
        {
            return hit.point + hit.normal * collisionBuffer;
        }
        return desiredPos;
    }
    public Vector3 GetCameraForward()
    {
        if (m_PlayerCamera == null) return Vector3.forward;
        Vector3 forward = m_PlayerCamera.transform.forward;
        forward.y = 0;
        return forward.normalized;
    }
    public Vector3 GetCameraRight()
    {
        if (m_PlayerCamera == null) return Vector3.right;
        Vector3 right = m_PlayerCamera.transform.right;
        right.y = 0;
        return right.normalized;
    }
}
