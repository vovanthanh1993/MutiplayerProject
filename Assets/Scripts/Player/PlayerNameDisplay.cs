using TMPro;
using UnityEngine;

public class PlayerNameDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Transform _targetToFollow;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 2.2f, 0f);

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_targetToFollow == null || _mainCamera == null) return;

        transform.position = _targetToFollow.position + _offset;
        transform.forward = _mainCamera.transform.forward;
    }

    public void SetPlayerName(string playerName)
    {
        _nameText.text = playerName;
    }

    public void SetTarget(Transform target)
    {
        _targetToFollow = target;
    }
}
