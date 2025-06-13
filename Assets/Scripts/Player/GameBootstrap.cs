using Fusion;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Prevent this object from being destroyed across scenes
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        // Đăng ký GameSpawner là callback cho NetworkRunner
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        GameSpawner spawner = GetComponent<GameSpawner>();

        if (runner != null && spawner != null)
        {
            runner.AddCallbacks(spawner);
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] Runner hoặc Spawner không tìm thấy khi khởi động.");
        }
    }
}
