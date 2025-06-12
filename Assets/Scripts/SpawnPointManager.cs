using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] _spawnPoints; // Private with [SerializeField] for Inspector access

    private int _lastIndex = -1;

    private void Awake()
    {
        // Set the singleton instance
        Instance = this;
    }

    /// <summary>
    /// Get the next available spawn point in round-robin order
    /// </summary>
    /// <returns>Transform of the next spawn point</returns>
    public Transform GetNextSpawnPoint()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned in SpawnPointManager.");
            return null;
        }

        _lastIndex = (_lastIndex + 1) % _spawnPoints.Length;
        return _spawnPoints[_lastIndex];
    }
}
