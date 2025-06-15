using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -7);
    [SerializeField] private float smoothSpeed = 10f;

    private Transform target;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.LookAt(target);
    }
}
