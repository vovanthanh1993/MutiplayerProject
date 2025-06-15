using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private NetworkCharacterController _characterController;
    [SerializeField] private GameObject _cameraRoot;
    [SerializeField] private float moveSpeed = 5f;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            _cameraRoot.gameObject.SetActive(true);
            Camera.main.gameObject.GetComponent<PlayerCameraFollow>().SetTarget(transform);
        }
        else
        {
            //_cameraRoot.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (GetInput(out NetworkInputData input))
        {
            // Di chuyển theo trục thế giới
            Vector3 inputDir = new Vector3(input.Horizontal, 0f, input.Vertical);

            if (inputDir.sqrMagnitude > 0.01f)
            {
                inputDir.Normalize();
                _characterController.Move(inputDir * moveSpeed);
            }
            else
            {
                _characterController.Move(Vector3.zero);
            }

            // Nhảy nếu đang đứng đất
            if (input.Jump)
            {
                _characterController.Jump();
            }
        }
    }
}
