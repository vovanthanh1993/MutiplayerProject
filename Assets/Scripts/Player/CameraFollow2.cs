using UnityEngine;

public class CameraFollow555 : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -7);
    public float smooth = 5f;

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smooth * Time.deltaTime);
        transform.LookAt(target);
    }
}
