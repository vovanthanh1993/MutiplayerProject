using UnityEngine;

/// <summary>
/// Singleton buffer for collecting player input from Unity each frame.
/// </summary>
public class InputBuffer : MonoBehaviour
{
    public static InputBuffer Instance { get; private set; }

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }

    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        // Collect input every Unity frame
        Horizontal = Input.GetAxis("Horizontal");
        Vertical = Input.GetAxis("Vertical");
    }
}
