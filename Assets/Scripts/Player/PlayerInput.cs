using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
public class PlayerInput : NetworkBehaviour
{
    private InputSystem_Actions m_InputActions;
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsCursorLocked { get; private set; } = true;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        m_InputActions = new InputSystem_Actions();
        m_InputActions.Player.Enable();
        m_InputActions.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        m_InputActions.Player.Move.canceled += ctx => MoveInput = Vector2.zero;
        m_InputActions.Player.Look.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
        m_InputActions.Player.Look.canceled += ctx => LookInput = Vector2.zero;
        SetCursorLock(true);
    }
    private void Update()
    {
        if (!IsOwner) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLock(!IsCursorLocked);
        }
    }
    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        IsCursorLocked = locked;
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            SetCursorLock(false);
        }
        if (m_InputActions != null)
        {
            m_InputActions.Player.Disable();
            m_InputActions.Dispose();
        }
        base.OnNetworkDespawn();
    }
}
