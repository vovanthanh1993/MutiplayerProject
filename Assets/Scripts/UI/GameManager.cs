using Fusion;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Settings")]
    [SerializeField] private NetworkPrefabRef _playerPrefabRef;
    [Header("Spawn Points")]
    [SerializeField] private Transform[] _spawnPoints;

    private void Awake()
    {
        // Ensure Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Spawns a player at a predefined spawn point.
    /// Only the server should call this.
    /// </summary>
    /// <param name="runner">The active NetworkRunner</param>
    /// <param name="player">The PlayerRef to assign input authority</param>
    public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        // Choose a spawn point based on player ID
        int index = player.RawEncoded % _spawnPoints.Length;
        Vector3 spawnPosition = _spawnPoints[index].position;

        // Spawn the player using NetworkPrefabRef and assign input authority
        var playerObj = runner.Spawn(_playerPrefabRef, spawnPosition, Quaternion.identity, player);
    }
}
