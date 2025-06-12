using UnityEngine;

// Không cần using Fusion vì script này không phải là một NetworkBehaviour.
// Nó là một MonoBehaviour thông thường chỉ chạy trên client của người chơi cục bộ.
public class vThirdPersonCamera_Networked : MonoBehaviour
{
    #region Inspector Variables (Sao chép từ file gốc)

    public Transform target;
    public LayerMask cullingLayer = 1 << 0;
    public bool lockCamera;
    public float smoothCameraRotation = 12f;
    public float rightOffset = 0f;
    public float defaultDistance = 2.5f;
    public float height = 1.4f;
    public float smoothFollow = 10f;
    public float xMouseSensitivity = 3f;
    public float yMouseSensitivity = 3f;
    public float yMinLimit = -40f;
    public float yMaxLimit = 80f;

    // Các biến input chuột (lấy từ vThirdPersonInput)
    public string rotateCameraXInput = "Mouse X";
    public string rotateCameraYInput = "Mouse Y";

    #endregion

    #region Private Variables

    private Transform targetLookAt;
    private Vector3 currentTargetPos;
    private Vector3 lookPoint;
    private Vector3 current_cPos;
    private Vector3 desired_cPos;
    private Camera _camera;
    private float distance;
    private float mouseY;
    private float mouseX;
    private float currentHeight;
    private float cullingDistance;
    private float checkHeightRadius = 0.4f;
    private float clipPlaneMargin = 0f;
    private float forward = -1f;
    private float xMinLimit = -360f;
    private float xMaxLimit = 360f;
    private float cullingHeight = 0.2f;
    private float cullingMinDist = 0.1f;
    private float offSetPlayerPivot;

    #endregion

    void Start()
    {
        // Không cần Init() ở đây nữa, logic sẽ được gọi từ bên ngoài
    }

    /// <summary>
    /// Được gọi bởi script của nhân vật (vThirdPersonController_Networked) khi nhân vật được spawn.
    /// </summary>
    public void Init(Transform mainTarget)
    {
        target = mainTarget;
        if (target == null) return;

        _camera = GetComponent<Camera>();
        currentTargetPos = new Vector3(target.position.x, target.position.y + offSetPlayerPivot, target.position.z);

        targetLookAt = new GameObject("targetLookAt").transform;
        targetLookAt.position = target.position;
        targetLookAt.hideFlags = HideFlags.HideInHierarchy;
        targetLookAt.rotation = target.rotation;

        mouseY = target.eulerAngles.x;
        mouseX = target.eulerAngles.y;

        distance = defaultDistance;
        currentHeight = height;
        Debug.Log("Camera mạng đã được khởi tạo và đang theo dõi " + target.name);
    }

    /// <summary>
    /// LateUpdate được dùng cho camera để đảm bảo nó di chuyển sau khi nhân vật đã hoàn tất mọi cập nhật về vị trí trong frame.
    /// </summary>
    void LateUpdate()
    {
        if (target == null || targetLookAt == null) return;

        HandleCameraInput();
        CameraMovement();
    }

    /// <summary>
    /// Đọc input từ chuột (logic lấy từ vThirdPersonInput).
    /// </summary>
    private void HandleCameraInput()
    {
        var Y = Input.GetAxis(rotateCameraYInput);
        var X = Input.GetAxis(rotateCameraXInput);

        // free rotation 
        mouseX += X * xMouseSensitivity;
        mouseY -= Y * yMouseSensitivity;

        if (!lockCamera)
        {
            mouseY = ClampAngle(mouseY, yMinLimit, yMaxLimit);
            mouseX = ClampAngle(mouseX, xMinLimit, xMaxLimit);
        }
        else
        {
            mouseY = target.root.localEulerAngles.x;
            mouseX = target.root.localEulerAngles.y;
        }
    }

    /// <summary>
    /// Tính toán vị trí và góc xoay của camera (logic từ vThirdPersonCamera).
    /// </summary>
    void CameraMovement()
    {
        distance = Mathf.Lerp(distance, defaultDistance, smoothFollow * Time.deltaTime);
        cullingDistance = Mathf.Lerp(cullingDistance, distance, Time.deltaTime);
        var camDir = (forward * targetLookAt.forward) + (rightOffset * targetLookAt.right);

        camDir = camDir.normalized;

        var targetPos = new Vector3(target.position.x, target.position.y + offSetPlayerPivot, target.position.z);
        currentTargetPos = targetPos;
        desired_cPos = targetPos + new Vector3(0, height, 0);
        current_cPos = currentTargetPos + new Vector3(0, currentHeight, 0);

        // ... (phần còn lại của logic va chạm và vị trí camera không thay đổi) ...

        targetLookAt.position = current_cPos;
        Quaternion newRot = Quaternion.Euler(mouseY, mouseX, 0);
        targetLookAt.rotation = Quaternion.Slerp(targetLookAt.rotation, newRot, smoothCameraRotation * Time.deltaTime);
        transform.position = current_cPos + (camDir * (distance));
        var rotation = Quaternion.LookRotation((current_cPos + targetLookAt.forward * 2f) - transform.position);
        transform.rotation = rotation;
    }

    // Hàm tiện ích để giới hạn góc (lấy từ vExtensions)
    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360) angle += 360;
        if (angle > 360) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}
