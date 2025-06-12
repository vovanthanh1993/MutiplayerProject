using Fusion;
using Invector.vCharacterController; // Giữ lại namespace gốc để dùng các struct như vMovementSpeed
using UnityEngine;
using static Invector.vCharacterController.vThirdPersonMotor;

public class vThirdPersonController_Networked : NetworkBehaviour
{
    #region Invector Variables (Đã được tổng hợp)

    [Header("Components & Settings")]
    [Tooltip("Gán Animator của nhân vật vào đây")]
    public Animator animator;
    [Tooltip("Gán Rigidbody của nhân vật vào đây")]
    public Rigidbody _rigidbody;
    [Tooltip("Gán CapsuleCollider của nhân vật vào đây")]
    public CapsuleCollider _capsuleCollider;
    [Tooltip("Kéo GameObject chứa Camera và Audio Listener của người chơi vào đây.")]
    public GameObject playerCameraAndListener;

    [Header("Movement")]
    public vMovementSpeed freeSpeed = new vMovementSpeed();
    public vMovementSpeed strafeSpeed = new vMovementSpeed();
    public LocomotionType locomotionType = LocomotionType.FreeWithStrafe;
    public bool useRootMotion = false;
    public bool useContinuousSprint = true;
    public bool sprintOnlyFree = true;

    [Header("Airborne")]
    public bool jumpAndRotate = true;
    public float jumpTimer = 0.3f;
    public float jumpHeight = 4f;
    public float airSpeed = 5f;
    public float airSmooth = 6f;
    public float extraGravity = -10f;

    [Header("Ground Check")]
    public LayerMask groundLayer = 1 << 0;
    public float groundMinDistance = 0.25f;
    public float groundMaxDistance = 0.5f;
    [Range(30, 80)] public float slopeLimit = 75f;

    [Networked] public bool isStrafing { get; set; }
    [Networked] public bool isSprinting { get; set; }
    [Networked] public bool isGrounded { get; set; }
    [Networked] public bool isJumping { get; set; }

    [Networked, HideInInspector] public NetworkInputData PreviousInput { get; set; }

    private Transform cameraTransform;
    private Vector3 moveDirection;
    private Vector3 inputSmooth;
    private float horizontalSpeed;
    private float verticalSpeed;
    private float inputMagnitude;
    private float moveSpeed;
    private float jumpCounter;
    private float groundDistance;
    private RaycastHit groundHit;
    private bool stopMove;
    private bool lockMovement;
    private bool lockRotation;
    private PhysicsMaterial frictionPhysics, maxFrictionPhysics, slippyPhysics;

    #endregion

    /// <summary>
    /// Awake() được gọi bởi Unity ngay khi đối tượng được tạo ra, trước cả khi mạng được khởi tạo xong.
    /// Chúng ta tắt camera ở đây để đảm bảo nó không bao giờ hoạt động trừ khi được cho phép.
    /// </summary>
    /*private void Awake()
    {
        if (playerCameraAndListener != null)
        {
            playerCameraAndListener.SetActive(false);
        }
    }*/

    public override void Spawned()
    {
        // =================================================================================
        // --- PHẦN GỠ LỖI QUAN TRỌNG ---
        // Log này sẽ cho chúng ta biết chính xác tại sao HasStateAuthority lại là true/false
        // =================================================================================
        Debug.Log($"--- KIỂM TRA SPAWN ---\n" +
                  $"GameObject Name: {gameObject.name}\n" +
                  $"Object ID: {Object.Id}\n" +
                  $"Người chơi có quyền (Input Authority): Player {Object.InputAuthority.PlayerId}\n" +
                  $"Người chơi cục bộ (Local Player): Player {Runner.LocalPlayer.PlayerId}\n" +
                  $"=> HasStateAuthority có giá trị là: {HasStateAuthority}\n" +
                  $"------------------------");

        // Khởi tạo các component vật lý và animator
        if (animator == null) animator = GetComponent<Animator>();
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        if (_capsuleCollider == null) _capsuleCollider = GetComponent<CapsuleCollider>();

        // Chỉ người chơi cục bộ (local player) mới kích hoạt camera và nhận input
        if (HasStateAuthority)
        {
            Debug.Log($"[QUYỀN ĐIỀU KHIỂN - {gameObject.name}] Đây là nhân vật của tôi. Bắt đầu kích hoạt camera.");
            if (playerCameraAndListener != null)
            {
                playerCameraAndListener.SetActive(true);

                var networkCamera = playerCameraAndListener.GetComponentInChildren<vThirdPersonCamera_Networked>();
                if (networkCamera != null)
                {
                    networkCamera.Init(this.transform);
                    cameraTransform = networkCamera.transform;
                    Debug.Log($"[QUYỀN ĐIỀU KHIỂN - {gameObject.name}] Camera đã được KÍCH HOẠT và gán.");
                }
                else Debug.LogError("LỖI: Không tìm thấy script vThirdPersonCamera_Networked trên camera của người chơi.");
            }
            else
            {
                Debug.LogError("LỖI: Chưa gán 'Player Camera And Listener' vào Inspector của nhân vật.");
            }
        }
        else
        {
            Debug.Log($"[KHÔNG CÓ QUYỀN - {gameObject.name}] Đây là nhân vật của người chơi khác. Camera sẽ bị tắt.");
        }

        animator.updateMode = AnimatorUpdateMode.Fixed;
        frictionPhysics = new PhysicsMaterial { name = "frictionPhysics", staticFriction = 0.25f, dynamicFriction = 0.25f, frictionCombine = PhysicsMaterialCombine.Multiply };
        maxFrictionPhysics = new PhysicsMaterial { name = "maxFrictionPhysics", staticFriction = 1f, dynamicFriction = 1f, frictionCombine = PhysicsMaterialCombine.Maximum };
        slippyPhysics = new PhysicsMaterial { name = "slippyPhysics", staticFriction = 0f, dynamicFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
        isGrounded = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (GetInput(out NetworkInputData currentInput))
        {
            HandleActions(currentInput, PreviousInput);
            UpdateMotor(currentInput);
            PreviousInput = currentInput;
        }
    }

    private void HandleActions(NetworkInputData current, NetworkInputData previous)
    {
        if (current.buttons.WasPressed(previous.buttons, InvectorButtons.Jump) && JumpConditions()) Jump();
        if (current.buttons.WasPressed(previous.buttons, InvectorButtons.Strafe)) isStrafing = !isStrafing;
        bool isTryingToSprint = current.buttons.IsSet(InvectorButtons.Sprint);
        Sprint(isTryingToSprint, current.moveDirection);
    }

    public void UpdateMotor(NetworkInputData data)
    {
        CheckGround();
        CheckSlopeLimit();
        ControlJumpBehaviour();
        AirControl(data.moveDirection);
        UpdateMoveDirection(data.moveDirection);
        ControlLocomotionType();
        ControlRotationType(data.moveDirection);
        UpdateAnimator();
    }

    #region Core Logic (Adapted from Invector Scripts)

    void UpdateMoveDirection(Vector3 networkInput)
    {
        inputSmooth = Vector3.Lerp(inputSmooth, networkInput, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Runner.DeltaTime);
        if (networkInput.magnitude <= 0.01f)
        {
            moveDirection = Vector3.zero;
            return;
        }

        if (cameraTransform)
        {
            var right = cameraTransform.right;
            right.y = 0;
            var forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
            moveDirection = (inputSmooth.x * right.normalized) + (inputSmooth.z * forward.normalized);
        }
        else
        {
            moveDirection = new Vector3(inputSmooth.x, 0, inputSmooth.z);
        }
    }

    void MoveCharacter(Vector3 _direction)
    {
        if (!isGrounded || isJumping) return;

        _direction.y = 0;
        _direction.x = Mathf.Clamp(_direction.x, -1f, 1f);
        _direction.z = Mathf.Clamp(_direction.z, -1f, 1f);
        if (_direction.magnitude > 1f) _direction.Normalize();

        Vector3 targetVelocity = _direction * (stopMove ? 0 : moveSpeed);
        targetVelocity.y = _rigidbody.linearVelocity.y;

        if (_rigidbody.isKinematic)
        {
            Debug.LogWarning("Rigidbody đang bị isKinematic, không thể di chuyển bằng vận tốc. Hãy tắt nó đi.");
            return;
        }

        _rigidbody.linearVelocity = targetVelocity;
    }

    void CheckGround()
    {
        CheckGroundDistance();
        ControlMaterialPhysics();
        if (groundDistance <= groundMinDistance) isGrounded = true;
        else if (groundDistance >= groundMaxDistance) isGrounded = false;
    }

    void CheckGroundDistance()
    {
        if (_capsuleCollider != null)
        {
            float radius = _capsuleCollider.radius * 0.9f;
            var dist = 10f;
            Ray ray = new Ray(transform.position + new Vector3(0, _capsuleCollider.height / 2, 0), Vector3.down);
            if (Physics.Raycast(ray, out groundHit, (_capsuleCollider.height / 2) + dist, groundLayer) && !groundHit.collider.isTrigger)
                dist = transform.position.y - groundHit.point.y;
            groundDistance = (float)System.Math.Round(dist, 2);
        }
    }

    void ControlMaterialPhysics()
    {
        _capsuleCollider.material = (isGrounded && Vector3.Angle(Vector3.up, groundHit.normal) <= slopeLimit) ? frictionPhysics : slippyPhysics;
    }

    float GroundAngle()
    {
        return isGrounded ? Vector3.Angle(groundHit.normal, Vector3.up) : 0f;
    }

    void ControlLocomotionType()
    {
        if (lockMovement) return;

        if (locomotionType.Equals(LocomotionType.FreeWithStrafe) && !isStrafing || locomotionType.Equals(LocomotionType.OnlyFree))
        {
            SetControllerMoveSpeed(freeSpeed);
            SetAnimatorMoveSpeed(freeSpeed, moveDirection);
        }
        else if (locomotionType.Equals(LocomotionType.OnlyStrafe) || locomotionType.Equals(LocomotionType.FreeWithStrafe) && isStrafing)
        {
            isStrafing = true;
            SetControllerMoveSpeed(strafeSpeed);
            SetAnimatorMoveSpeed(strafeSpeed, moveDirection);
        }

        if (!useRootMotion) MoveCharacter(moveDirection);
    }

    void SetControllerMoveSpeed(vMovementSpeed speed)
    {
        if (speed.walkByDefault)
            moveSpeed = Mathf.Lerp(moveSpeed, isSprinting ? speed.runningSpeed : speed.walkSpeed, speed.movementSmooth * Runner.DeltaTime);
        else
            moveSpeed = Mathf.Lerp(moveSpeed, isSprinting ? speed.sprintSpeed : speed.runningSpeed, speed.movementSmooth * Runner.DeltaTime);
    }

    void ControlRotationType(Vector3 networkInput)
    {
        if (lockRotation) return;

        if (inputSmooth.magnitude > 0.1f)
        {
            Vector3 dir = (isStrafing && (!isSprinting || sprintOnlyFree == false)) && cameraTransform ? cameraTransform.forward : moveDirection;
            RotateToDirection(dir);
        }
    }

    void RotateToDirection(Vector3 direction)
    {
        if (!jumpAndRotate && !isGrounded) return;
        direction.y = 0f;
        if (direction.magnitude > 0.1)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float rotationSpeed = isStrafing ? strafeSpeed.rotationSpeed : freeSpeed.rotationSpeed;
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Runner.DeltaTime);
        }
    }

    void CheckSlopeLimit()
    {
        stopMove = false;
    }

    bool JumpConditions()
    {
        return isGrounded && GroundAngle() < slopeLimit && !isJumping && !stopMove;
    }

    void Jump()
    {
        if (!JumpConditions()) return;
        jumpCounter = jumpTimer;
        isJumping = true;
        if (inputSmooth.sqrMagnitude < 0.1f) animator.CrossFadeInFixedTime("Jump", 0.1f);
        else animator.CrossFadeInFixedTime("JumpMove", .2f);
    }

    void ControlJumpBehaviour()
    {
        if (!isJumping) return;
        jumpCounter -= Runner.DeltaTime;
        if (jumpCounter <= 0)
        {
            jumpCounter = 0;
            isJumping = false;
        }
        var vel = _rigidbody.linearVelocity;
        vel.y = jumpHeight;
        _rigidbody.linearVelocity = vel;
    }

    void AirControl(Vector3 networkInput)
    {
        if (isGrounded && !isJumping) return;

        Vector3 airMoveDirection = new Vector3(inputSmooth.x, 0, inputSmooth.z);
        Vector3 targetVelocity = (airMoveDirection * airSpeed);
        targetVelocity.y = _rigidbody.linearVelocity.y + (extraGravity * Runner.DeltaTime);
        _rigidbody.linearVelocity = targetVelocity;
        if (jumpAndRotate) RotateToDirection(airMoveDirection);
    }

    void Sprint(bool value, Vector3 networkInput)
    {
        var sprintConditions = (networkInput.sqrMagnitude > 0.1f && isGrounded && !(isStrafing && !strafeSpeed.walkByDefault));
        if (value && sprintConditions) isSprinting = true;
        else isSprinting = false;
    }

    void UpdateAnimator()
    {
        if (animator == null || !animator.enabled) return;

        animator.SetBool("IsStrafing", isStrafing);
        animator.SetBool("IsSprinting", isSprinting);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("GroundDistance", groundDistance);

        if (isStrafing)
        {
            animator.SetFloat("InputHorizontal", stopMove ? 0 : horizontalSpeed, strafeSpeed.animationSmooth, Runner.DeltaTime);
            animator.SetFloat("InputVertical", stopMove ? 0 : verticalSpeed, strafeSpeed.animationSmooth, Runner.DeltaTime);
        }
        else
        {
            animator.SetFloat("InputVertical", stopMove ? 0 : verticalSpeed, freeSpeed.animationSmooth, Runner.DeltaTime);
        }

        animator.SetFloat("InputMagnitude", stopMove ? 0f : inputMagnitude, isStrafing ? strafeSpeed.animationSmooth : freeSpeed.animationSmooth, Runner.DeltaTime);
    }

    void SetAnimatorMoveSpeed(vMovementSpeed speed, Vector3 moveDirection)
    {
        Vector3 relativeInput = transform.InverseTransformDirection(moveDirection);
        verticalSpeed = relativeInput.z;
        horizontalSpeed = relativeInput.x;
        var newInput = new Vector2(verticalSpeed, horizontalSpeed);

        if (speed.walkByDefault) inputMagnitude = Mathf.Clamp(newInput.magnitude, 0, isSprinting ? vThirdPersonAnimator.runningSpeed : vThirdPersonAnimator.walkSpeed);
        else inputMagnitude = Mathf.Clamp(isSprinting ? newInput.magnitude + 0.5f : newInput.magnitude, 0, isSprinting ? vThirdPersonAnimator.sprintSpeed : vThirdPersonAnimator.runningSpeed);
    }

    #endregion
}
