using UnityEngine;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 2f, -4f);
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _smoothSpeed = 10f;

    private Transform _target;
    private float _mouseX;
    private float _mouseY;

    private const float Y_MIN_ANGLE = -35f;
    private const float Y_MAX_ANGLE = 60f;
    private const float LOOK_AT_HEIGHT = 1.5f;

    private void LateUpdate()
    {
        if (_target == null)
            return;

        UpdateRotation();
        UpdatePosition();
        LookAtTarget();
    }

    /// <summary>
    /// Set the player transform that this camera will follow.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        _target = newTarget;
        _mouseX = newTarget.eulerAngles.y;
    }

    private void UpdateRotation()
    {
        _mouseX += Input.GetAxis("Mouse X") * _rotationSpeed;
        _mouseY -= Input.GetAxis("Mouse Y") * _rotationSpeed;
        _mouseY = Mathf.Clamp(_mouseY, Y_MIN_ANGLE, Y_MAX_ANGLE);
    }

    private void UpdatePosition()
    {
        Quaternion rotation = Quaternion.Euler(_mouseY, _mouseX, 0f);
        Vector3 desiredPosition = _target.position + rotation * _offset;
        Vector3 smoothPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
        transform.position = smoothPosition;
    }

    private void LookAtTarget()
    {
        Vector3 lookAtPoint = _target.position + Vector3.up * LOOK_AT_HEIGHT;
        transform.LookAt(lookAtPoint);
    }
}
