using Fusion;
using UnityEngine;
using Invector.vCharacterController;

[RequireComponent(typeof(NetworkObject))]
public class FusionThirdPersonNetwork : NetworkBehaviour
{
    [SerializeField] private vThirdPersonInput _input;
    [SerializeField] private vThirdPersonController _controller;
    [SerializeField] private vThirdPersonCamera _cameraScript;

    public override void Spawned()
    {
        _input = GetComponent<vThirdPersonInput>();
        _controller = GetComponent<vThirdPersonController>();
        _cameraScript = GetComponentInChildren<vThirdPersonCamera>(true);

        if (Object.HasInputAuthority)
        {
            if (_cameraScript != null)
            {
                _cameraScript.gameObject.SetActive(true);
                _cameraScript.SetMainTarget(transform);

                Camera cam = _cameraScript.GetComponent<Camera>();
                if (cam != null)
                    cam.tag = "MainCamera";
            }
        }
        else
        {
            if (_input != null)
                _input.enabled = false; // Tắt input thay vì Destroy

            if (_controller != null)
                _controller.enabled = false; // Tắt input thay vì Destroy

            if (_cameraScript != null)
                _cameraScript.gameObject.SetActive(false);
        }
    }

}
