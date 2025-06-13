using Fusion;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class FusionPlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 8f;
    [SerializeField] private float _gravity = 20f;

    [Header("Camera")]
    [SerializeField] private GameObject _cameraPrefab;

    [Header("Name Display")]
    [SerializeField] private PlayerNameDisplay _nameDisplay;

    [Networked] public string PlayerName { get; set; }

    private CharacterController _characterController;
    private Vector3 _velocity;

    private const float GROUND_CHECK_BUFFER = 0.1f;

    public override void Spawned()
    {
        InitializeComponents();

        // Local player logic
        if (HasInputAuthority)
        {
            SpawnLocalCamera();

            // Assign local player name
            string localName = $"Player_{Object.InputAuthority.PlayerId}";
            RPC_SetPlayerName(localName);
        }

        // Display name above head
        if (_nameDisplay != null)
        {
            _nameDisplay.SetTarget(transform);
            _nameDisplay.SetPlayerName(PlayerName);
        }
    }

    public override void Render()
    {
        // Continuously update name display on all clients
        if (_nameDisplay != null)
        {
            _nameDisplay.SetPlayerName(PlayerName);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerInputData inputData))
        {
            MovePlayer(inputData);
            ApplyGravityAndJump(inputData);
        }
    }

    /// <summary>
    /// Cache required components.
    /// </summary>
    private void InitializeComponents()
    {
        _characterController = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Spawn third-person camera and set target to this player.
    /// </summary>
    private void SpawnLocalCamera()
    {
        if (_cameraPrefab == null)
        {
            Debug.LogWarning("Camera prefab is not assigned.");
            return;
        }

        GameObject cameraInstance = Instantiate(_cameraPrefab);
        ThirdPersonCameraFollow cameraFollow = cameraInstance.GetComponent<ThirdPersonCameraFollow>();

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);
        }

        Camera cam = cameraInstance.GetComponent<Camera>();
        if (cam != null)
        {
            cam.tag = "MainCamera";
        }
    }

    /// <summary>
    /// Handle movement and rotation based on input.
    /// </summary>
    private void MovePlayer(PlayerInputData inputData)
    {
        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;

        if (HasInputAuthority && Camera.main != null)
        {
            cameraForward = Camera.main.transform.forward;
            cameraRight = Camera.main.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
        }

        Vector3 moveDirection = (cameraForward * inputData.MoveInput.y + cameraRight * inputData.MoveInput.x).normalized;
        Vector3 horizontalMove = moveDirection * _moveSpeed;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Runner.DeltaTime);
        }

        _characterController.Move(horizontalMove * Runner.DeltaTime);
    }

    /// <summary>
    /// Apply vertical movement including gravity and jump.
    /// </summary>
    private void ApplyGravityAndJump(PlayerInputData inputData)
    {
        if (_characterController.isGrounded)
        {
            _velocity.y = -GROUND_CHECK_BUFFER;

            if (inputData.JumpPressed)
            {
                _velocity.y = _jumpForce;
            }
        }
        else
        {
            _velocity.y -= _gravity * Runner.DeltaTime;
        }

        Vector3 verticalMove = new Vector3(0f, _velocity.y, 0f);
        _characterController.Move(verticalMove * Runner.DeltaTime);
    }

    /// <summary>
    /// RPC to sync player name from local client to server.
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string name)
    {
        PlayerName = name;
    }
}
