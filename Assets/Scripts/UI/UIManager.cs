using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private GameObject _connectingPanel;
    [SerializeField] private GameObject _gameplayPanel;

    [Header("Status Text")]
    [SerializeField] private TMP_Text _statusText;

    [Header("Room UI")]
    [SerializeField] private TMP_InputField _roomInput;
    [SerializeField] private Button _createButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _quickJoinButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ShowMenu();
        _createButton.onClick.AddListener(HandleCreateClicked);
        _joinButton.onClick.AddListener(HandleJoinClicked);
        _quickJoinButton.onClick.AddListener(HandleQuickJoinClicked);
    }

    private void HandleCreateClicked()
    {
        string roomName = string.IsNullOrEmpty(_roomInput.text) ? "DefaultRoom" : _roomInput.text;
        ShowConnecting("Creating Room...");
        NetworkRunnerHandler.Instance.ConnectToSession(roomName, GameMode.Host);
    }

    private void HandleJoinClicked()
    {
        string roomName = string.IsNullOrEmpty(_roomInput.text) ? "DefaultRoom" : _roomInput.text;
        ShowConnecting("Joining Room...");
        NetworkRunnerHandler.Instance.ConnectToSession(roomName, GameMode.Client);
    }

    private void HandleQuickJoinClicked()
    {
        ShowConnecting("Searching Room...");
        NetworkRunnerHandler.Instance.ConnectToSession("", GameMode.AutoHostOrClient);
    }

    public void ShowMenu()
    {
        _menuPanel.SetActive(true);
        _connectingPanel.SetActive(false);
        _gameplayPanel.SetActive(false);
        SetStatus("In Menu");
    }

    public void ShowConnecting(string message = "Connecting...")
    {
        _menuPanel.SetActive(false);
        _connectingPanel.SetActive(true);
        _gameplayPanel.SetActive(false);
        SetStatus(message);
    }

    public void ShowGameplay()
    {
        _menuPanel.SetActive(false);
        _connectingPanel.SetActive(false);
        _gameplayPanel.SetActive(true);
        SetStatus("Connected!");
    }

    public void SetStatus(string message)
    {
        if (_statusText != null)
            _statusText.text = message;
    }
}
