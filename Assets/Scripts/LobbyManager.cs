using UnityEngine;
using UnityEngine.UI;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Collections.Generic;
using TMPro;

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
            _statusText.text = "🔄 Connecting to Lobby...";

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        await _runner.JoinSessionLobby(SessionLobby.ClientServer);

        if (_statusText != null)
            _statusText.text = "🟢 Connected to Lobby!";

        // Attach button click events
        _createButton?.onClick.AddListener(() => StartGame(_roomInput.text, GameMode.Host));
        _joinButton?.onClick.AddListener(() => StartGame(_roomInput.text, GameMode.Client));
    }

    public async void StartGame(string roomName, GameMode mode)
    {
        // Display current joining/creating status
        if (_statusText != null)
            _statusText.text = $"🔄 {(mode == GameMode.Host ? "Creating" : "Joining")} room: {roomName}...";

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

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        /*if(runner.IsServer) {
            Transform spawnPoint = SpawnPointManager.Instance?.GetNextSpawnPoint();
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            runner.Spawn(_playerPrefab, spawnPosition, spawnRotation, player);
        }*/

        if (runner.IsServer)
        {
            // Random position in a rectangle area
            float x = 10f;
            float z = 10f;
            float y = 30f; // hoặc mặt đất tùy theo game bạn

            Vector3 spawnPosition = new Vector3(x, y, z);
            Quaternion spawnRotation = Quaternion.identity;

            Debug.Log($"[Spawn] Player {player.PlayerId} at {spawnPosition}");

            runner.Spawn(_playerPrefab, spawnPosition, spawnRotation, player);
        }
    }



    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (_statusText != null)
            _statusText.text = $"📋 Found {sessionList.Count} rooms.";

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

    #region Fusion Callbacks
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        if (_statusText != null)
            _statusText.text = $"❌ Connection failed: {reason}";
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (_statusText != null)
            _statusText.text = $"🔌 Disconnected: {reason}";
    }


    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (_statusText != null)
            _statusText.text = "✅ Scene loaded successfully.";
    }

    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    #endregion
}
