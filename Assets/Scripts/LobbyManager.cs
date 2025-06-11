using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField _roomNameInput;
    [SerializeField] private Transform _roomListContent; // Content object của ScrollView
    [SerializeField] private GameObject _roomItemPrefab;

    private NetworkRunner _runner;

    // --- Vòng đời Unity ---

    private async void Start()
    {
        // Tạo một NetworkRunner để kết nối với Photon Cloud và lấy danh sách phòng
        _runner = gameObject.AddComponent<NetworkRunner>();
        gameObject.AddComponent<NetworkSceneManagerDefault>(); // Cần để tự động chuyển scene

        // Đăng ký các callback để lắng nghe sự kiện từ Fusion
        _runner.AddCallbacks(this);

        // Kết nối vào một "Lobby chung" để lấy danh sách các session
        await _runner.JoinSessionLobby(SessionLobby.Shared);
        Debug.Log("Đã kết nối vào sảnh chung (Shared Lobby)!");
    }

    // Fix for CS0029: Cannot implicitly convert type 'int' to 'Fusion.NetworkSceneInfo?'
    // The `Scene` property in `StartGameArgs` expects a `Fusion.NetworkSceneInfo?` type, not an integer.
    // Replace the problematic line with the correct conversion using `NetworkSceneInfo`.

    public async void CreateRoom()
    {
        string roomName = _roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("Tên phòng không được để trống!");
            return;
        }

        Debug.Log($"Đang tạo phòng: {roomName}");

        // Correctly set the Scene property using NetworkSceneInfo
        var activeSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        var sceneRef = SceneRef.FromIndex(activeSceneIndex); // Create SceneRef from index

        var sceneInfo = new NetworkSceneInfo(); // Create a new NetworkSceneInfo instance
        sceneInfo.AddSceneRef(sceneRef); // Use AddSceneRef method to add the scene

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,       // Tạo phòng mới
            SessionName = roomName,         // Tên phòng
            Scene = sceneInfo,              // Correctly set the scene
            PlayerCount = 4,                // Số người chơi tối đa
        });

        if (result.Ok)
        {
            Debug.Log("Tạo phòng thành công!");
            // Fusion sẽ tự động chuyển sang Game Scene
        }
        else
        {
            Debug.LogError($"Tạo phòng thất bại: {result.ShutdownReason}");
        }
    }

    private async void JoinRoom(SessionInfo session)
    {
        Debug.Log($"Đang vào phòng: {session.Name}");

        // Bắt đầu game với vai trò là Client, tham gia vào session đã có
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,     // Vào phòng đã có
            SessionName = session.Name,     // Tên phòng muốn vào
            // Scene và SceneManager được Host quản lý, client không cần chỉ định
        });

        if (result.Ok)
        {
            Debug.Log("Vào phòng thành công!");
        }
        else
        {
            Debug.LogError($"Vào phòng thất bại: {result.ShutdownReason}");
        }
    }

    // --- Callback từ Fusion (INetworkRunnerCallbacks) ---

    // Hàm này được gọi khi danh sách phòng trên sảnh chung thay đổi
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log("Cập nhật danh sách phòng...");

        // Xóa danh sách phòng cũ trên UI
        foreach (Transform child in _roomListContent)
        {
            Destroy(child.gameObject);
        }

        // Hiển thị danh sách phòng mới
        foreach (var session in sessionList)
        {
            if (session.IsOpen) // Chỉ hiển thị các phòng đang mở
            {
                GameObject roomItemGO = Instantiate(_roomItemPrefab, _roomListContent);

                // Cập nhật tên phòng
                roomItemGO.transform.Find("RoomNameText").GetComponent<TMP_Text>().text = session.Name;

                // Gán sự kiện cho nút Join
                roomItemGO.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
                {
                    JoinRoom(session);
                });
            }
        }
    }

    // Các callbacks khác không sử dụng trong ví dụ này nhưng bắt buộc phải có
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
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
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }
}