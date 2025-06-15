using Fusion;
using UnityEngine;
using TMPro;

public class PlayerController : NetworkBehaviour
{
    [Networked] public string PlayerName { get; set; }

    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Camera _cameraRoot;

    [SerializeField] private NetworkCharacterController _characterController;
    [SerializeField] private float moveSpeed = 5f;

    public override void Spawned()
    {
        // Enable camera only for the local player
        _cameraRoot.gameObject.SetActive(Object.HasInputAuthority);
        Debug.Log($"[{Runner.LocalPlayer.PlayerId}] Spawned at: {transform.position}, Authority: {HasStateAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        Debug.Log($"Spawn {transform.position.x}, {transform.position.y}, {transform.position.z}");

        if (GetInput(out NetworkInputData input))
        {
            Vector3 inputDir = new Vector3(input.Horizontal, 0f, input.Vertical);

            if (inputDir.sqrMagnitude > 0.01f)
            {
                inputDir.Normalize();
            }

            // Gọi hàm Move từ NetworkCharacterControllerPrototype (nó đã xử lý gravity + grounded)
            _characterController.Move(inputDir * moveSpeed);

            /*if (input.Jump)
                _characterController.Jump();*/
        }
    }
}
