using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Unity.Collections.Unicode;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField _roomInput;
    [SerializeField] private Button _createButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private GameObject _roomButtonPrefab;
    [SerializeField] private Transform _roomListParent;
    [SerializeField] private TextMeshProUGUI _statusText;

    [Header("Game Setup")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private int _gameSceneIndex = 1;

    private NetworkRunner _runner;

    private async void Start()
    {
        // Update UI to show lobby connection status
        if (_statusText != null)
            _statusText.text = "Connecting to Lobby...";

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(gameObject.AddComponent<PlayerInputHandler>());

        await _runner.JoinSessionLobby(SessionLobby.ClientServer);

        if (_statusText != null)
            _statusText.text = "Connected to Lobby!";

        // Attach button click events
        _createButton?.onClick.AddListener(() => StartGame(_roomInput.text, GameMode.Host));
        _joinButton?.onClick.AddListener(() => StartGame(_roomInput.text, GameMode.Client));
    }

    public async void StartGame(string roomName, GameMode mode)
    {
        // Display current joining/creating status
        if (_statusText != null)
            _statusText.text = $"{(mode == GameMode.Host ? "Creating" : "Joining")} room: {roomName}...";

        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }

        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(_gameSceneIndex));

        await _runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = roomName,
            Scene = sceneInfo,
            PlayerCount = 4,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (_statusText != null)
            _statusText.text = $"Found {sessionList.Count} rooms.";

        // Clear current room list
        foreach (Transform child in _roomListParent)
        {
            Destroy(child.gameObject);
        }

        // Populate room list with updated sessions
        foreach (var session in sessionList)
        {
            GameObject buttonObject = Instantiate(_roomButtonPrefab, _roomListParent);
            TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();

            buttonText.text = $"{session.Name} ({session.PlayerCount}/{session.MaxPlayers})";

            buttonObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                StartGame(session.Name, GameMode.Client);
            });
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Logic này chỉ nên chạy trên Server/Host
        if (runner.IsServer)
        {
            Debug.Log($"OnPlayerJoined: Spawning player for connection {player.PlayerId}");

            // Tạo một instance của player prefab
            // Sử dụng Vector3.zero để spawn tại gốc tọa độ, chúng ta sẽ cải thiện sau
            NetworkObject networkPlayerObject = runner.Spawn(
                _playerPrefab,
                Vector3.zero,
                Quaternion.identity,
                player // Gán quyền điều khiển object này cho player vừa vào phòng
            );

            // Gán object vừa tạo cho player đó để Fusion có thể quản lý
            runner.SetPlayerObject(player, networkPlayerObject);
        }
        else
        {
            Debug.Log("OnPlayerJoined: Client instance, waiting for server to spawn player.");
        }
    }

    #region Fusion Callbacks
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        if (_statusText != null)
            _statusText.text = $"Connection failed: {reason}";
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (_statusText != null)
            _statusText.text = $"Disconnected: {reason}";
    }


    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (_statusText != null)
            _statusText.text = "Scene loaded successfully.";
    }


    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
  
    }
    #endregion
}
