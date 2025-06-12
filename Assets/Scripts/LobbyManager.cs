using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks; // Cần thiết cho các hàm bất đồng bộ (async/await)
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField _roomNameInput;
    [SerializeField] private Transform _roomListContent; // Content object của ScrollView
    [SerializeField] private GameObject _roomItemPrefab;
    [SerializeField] private GameObject _statusTextObject; // UI Text để hiển thị thông báo (ví dụ: "Đang kết nối...")
    [SerializeField] private GameObject _refreshButton;    // Nút Refresh để gán vào đây

    [Header("Game Scene")]
    [Tooltip("Chỉ số của Scene Game trong Build Settings")]
    [SerializeField] private int _gameSceneIndex = 1;

    private NetworkRunner _runner;
    private bool _isRefreshing = false; // Cờ để ngăn người dùng nhấn refresh liên tục

    private async void Start()
    {
        // Giữ lại GameObject chứa script này khi chuyển scene, để NetworkRunner không bị hủy
        DontDestroyOnLoad(gameObject);

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true; // Cần thiết để xử lý input trong game
        _runner.AddCallbacks(this);

        // Bắt đầu kết nối vào sảnh chờ
        await JoinLobby();
    }

    /// <summary>
    /// Kết nối vào sảnh chờ chung để lấy danh sách các phòng.
    /// </summary>
    private async Task JoinLobby()
    {
        if (_statusTextObject) _statusTextObject.GetComponent<TMP_Text>().text = "Đang kết nối vào sảnh...";
        if (_refreshButton) _refreshButton.SetActive(false); // Ẩn nút refresh trong khi kết nối  

        // Sử dụng SessionLobby.ClientServer thay vì SessionLobby.Default  
        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);

        if (result.Ok)
        {
            if (_statusTextObject) _statusTextObject.GetComponent<TMP_Text>().text = "Đã kết nối! Hãy chọn một phòng.";
        }
        else
        {
            if (_statusTextObject) _statusTextObject.GetComponent<TMP_Text>().text = $"Lỗi kết nối sảnh: {result.ShutdownReason}";
        }

        // Sau khi hoàn tất, hiện lại nút refresh và đặt lại cờ  
        if (_refreshButton) _refreshButton.SetActive(true);
        _isRefreshing = false;
    }

    /// <summary>
    /// HÀM MỚI: Được gọi bởi Button Refresh trên UI.
    /// </summary>
    public async void OnRefreshButtonPressed()
    {
        if (_isRefreshing) return; // Nếu đang refresh thì không làm gì cả

        _isRefreshing = true;

        // Rời sảnh hiện tại và kết nối lại để lấy danh sách mới
        await JoinLobby();
    }

    /// <summary>
    /// Được gọi bởi Button UI để tạo một phòng mới.
    /// </summary>
    public async void CreateRoom()
    {
        string roomName = _roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("Tên phòng không được để trống!");
            return;
        }

        if (_statusTextObject) _statusTextObject.GetComponent<TMP_Text>().text = $"Đang tạo phòng '{roomName}'...";

        // Correctly set the Scene property using NetworkSceneInfo
        var activeSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        var sceneRef = SceneRef.FromIndex(activeSceneIndex); // Create SceneRef from index
        var sceneInfo = new NetworkSceneInfo(); // Create a new NetworkSceneInfo instance
        sceneInfo.AddSceneRef(sceneRef); // Use AddSceneRef method to add the scene
        var result = await _runner.StartGame(new StartGameArgs()

        {
            GameMode = GameMode.Host,       // Tạo phòng mới
            SessionName = roomName,         // Tên phòng
            Scene = sceneInfo,              // Correctly set the scene
            PlayerCount = 4,                // Số người chơi tối đa
        });

        if (!result.Ok)
        {
            Debug.LogError($"Tạo phòng thất bại: {result.ShutdownReason}");
            if (_statusTextObject) _statusTextObject.GetComponent<TMP_Text>().text = $"Tạo phòng thất bại: {result.ShutdownReason}";
        }
    }

    /// <summary>
    /// Được gọi bởi Button UI của mỗi item trong danh sách phòng.
    /// </summary>
    private async void JoinRoom(SessionInfo session)
    {
        if (_statusTextObject) _statusTextObject.GetComponent<TMP_Text>().text = $"Đang vào phòng '{session.Name}'...";
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = session.Name
        });
    }

    /// <summary>
    /// Callback được Fusion gọi mỗi khi có sự thay đổi trong danh sách phòng của sảnh.
    /// </summary>
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // Xóa các item phòng cũ trên UI
        foreach (Transform child in _roomListContent)
        {
            Destroy(child.gameObject);
        }

        // Cập nhật thông báo
        if (_statusTextObject)
        {
            _statusTextObject.GetComponent<TMP_Text>().text = sessionList.Count == 0 ? "Không có phòng nào. Hãy tạo một phòng mới!" : "Chọn một phòng để vào:";
        }

        // Vẽ lại danh sách phòng mới
        foreach (var session in sessionList)
        {
            if (session.IsOpen)
            {
                GameObject roomItemGO = Instantiate(_roomItemPrefab, _roomListContent);
                // Tìm đối tượng Text con một cách an toàn hơn, tránh lỗi nếu đổi tên
                var roomNameText = roomItemGO.GetComponentInChildren<TMP_Text>();
                if (roomNameText) roomNameText.text = session.Name;

                roomItemGO.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => JoinRoom(session));
            }
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // Đọc input từ bàn phím/gamepad
        data.moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (Input.GetButtonDown("Jump"))
        {
            data.buttons.Set(InvectorButtons.Jump, true);
        }

        // Đưa dữ liệu cho Fusion
        input.Set(data);
    }

    #region Unused Callbacks (Để trống)
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        
    }
    #endregion
}
