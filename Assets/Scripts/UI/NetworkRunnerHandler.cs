using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunnerHandler Instance { get; private set; }

    [SerializeField] private NetworkRunner _runnerPrefab;
    private NetworkRunner _runner;
    public bool IsRunning => _runner != null && _runner.IsRunning;
    public NetworkRunner Runner => _runner;
    private bool _sceneReady = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public async void ConnectToSession(string roomName, GameMode mode)
    {
        _runner = Instantiate(_runnerPrefab);
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        StartGameArgs args = new StartGameArgs
        {
            GameMode = mode,
            SessionName = string.IsNullOrEmpty(roomName) ? null : roomName,
            Scene = SceneRef.FromIndex(1),
            SceneManager = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        StartGameResult result = await _runner.StartGame(args);

        if (result.Ok)
        {
            UIManager.Instance.ShowGameplay();
        }
        else
        {
            UIManager.Instance.ShowMenu();
            UIManager.Instance.SetStatus($"Failed: {result.ShutdownReason}");
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && _sceneReady)
        {
            GameManager.Instance.SpawnPlayer(runner, player);
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        _sceneReady = true;
    }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Check if InputBuffer is initialized
        if (InputBuffer.Instance == null)
        {
            Debug.LogWarning("⚠️ InputBuffer.Instance is null in OnInput()");
            return;
        }

        // Gather input from InputBuffer and send to Fusion input system
        NetworkInputData inputData = new NetworkInputData
        {
            Horizontal = InputBuffer.Instance.Horizontal,
            Vertical = InputBuffer.Instance.Vertical
        };

        input.Set(inputData);
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }
}
