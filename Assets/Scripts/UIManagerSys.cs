using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
public class UIManagerSys : NetworkBehaviour
{
    [Header("Lobby UI")]
    [SerializeField] private Toggle readyToggle;
    [SerializeField] private Button hostStartButton;
    [SerializeField] private TextMeshProUGUI playersCountText;
    [SerializeField] private GameObject lobbyPanel;

    void Start()
    {
        lobbyPanel.SetActive(true);
    }
    public override void OnNetworkSpawn()
    {
        if (hostStartButton != null)
        {
            hostStartButton.gameObject.SetActive(IsServer);
            hostStartButton.onClick.RemoveAllListeners();
            hostStartButton.onClick.AddListener(OnClickStart);
        }
        if (readyToggle != null)
        {
            readyToggle.onValueChanged.RemoveAllListeners();
            readyToggle.onValueChanged.AddListener(OnToggleReady);
        }
        UpdateLobbyUI();
        lobbyPanel.SetActive(false);
    }

    void Update()
    {     
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.LobbyPlayers == null) return;

        if (playersCountText != null)
        {
            playersCountText.text = $"{gm.LobbyPlayers.Count}/{GameManager.MaxLobbyPlayers}";
        }

        if (hostStartButton != null)
        {
            bool allReady = true;
            if (gm.LobbyPlayers.Count == 0) allReady = false;
            else
            {
                for (int i = 0; i < gm.LobbyPlayers.Count; i++)
                {
                    if (!gm.LobbyPlayers[i].Ready)
                    {
                        allReady = false;
                        break;
                    }
                }
            }

            hostStartButton.interactable = IsServer && allReady;
        }
    }
    private void OnToggleReady(bool isOn)
    {
        if (!IsClient) return;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetReadyRpc(isOn);
        }
    }

    private void OnClickStart()
    {
        if (!IsClient) return;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.RequestStartGameRpc();
        }
    }
}
