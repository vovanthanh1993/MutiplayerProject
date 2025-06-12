using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    void Start()
    {
        // Chúng ta không cần spawn người chơi ở đây nữa.
        // Thay vào đó, chúng ta chỉ cần đảm bảo rằng spawner này
        // đã đăng ký lắng nghe các sự kiện từ NetworkRunner.
        // Việc spawn sẽ được xử lý hoàn toàn bởi OnPlayerJoined.
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            // Chỉ cần đăng ký callback, không cần vòng lặp foreach
            runner.AddCallbacks(this);
            Debug.Log("Game Spawner đã đăng ký callback với NetworkRunner.");
        }
        else
        {
            Debug.LogError("Không tìm thấy NetworkRunner!");
        }
    }

    // Hàm này được Fusion gọi trên HOST cho MỌI người chơi khi scene được tải
    // và cho những người chơi MỚI kết nối sau đó.
    // Đây là nơi duy nhất và đúng đắn để spawn nhân vật.
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined được gọi cho Player {player.PlayerId}. Bắt đầu spawn.");
        // Chỉ Host mới có quyền spawn
        if (runner.IsServer)
        {
            SpawnCharacter(runner, player);
        }
    }

    private async void SpawnCharacter(NetworkRunner runner, PlayerRef player)
    {
        // Kiểm tra này bây giờ sẽ hoạt động hoàn hảo vì chỉ có một luồng gọi vào hàm này
        // cho mỗi người chơi tại một thời điểm.
        if (_spawnedCharacters.ContainsKey(player))
        {
            Debug.LogWarning($"Nhân vật cho Player {player.PlayerId} đã tồn tại. Bỏ qua việc spawn.");
            return;
        }

        Vector3 spawnPosition = Vector3.zero;
        if (_spawnPoints.Count > 0)
        {
            int spawnIndex = UnityEngine.Random.Range(0, _spawnPoints.Count);
            spawnPosition = _spawnPoints[spawnIndex].position;
        }
        else
        {
            Debug.LogWarning("Không có điểm spawn nào được gán. Spawn tại vị trí (0,1,0).");
            spawnPosition = new Vector3(0, 1, 0);
        }

        spawnPosition += Vector3.up * 0.2f;

        NetworkObject networkPlayerObject = await runner.SpawnAsync(
            _playerPrefab,
            spawnPosition,
            Quaternion.identity,
            player
        );

        _spawnedCharacters.Add(player, networkPlayerObject);
        Debug.Log($"Đã spawn thành công nhân vật cho Player {player.PlayerId} tại {spawnPosition}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} left. Despawning character.");
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    #region Unused Callbacks
    // Các callbacks không sử dụng giữ nguyên...
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    #endregion
}