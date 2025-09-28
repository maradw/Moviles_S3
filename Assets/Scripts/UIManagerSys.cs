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
        // Configuración básica de visibilidad

        /*if (hostStartButton != null)
        {
            // Solo el Host (server) ve el botón de Start
            hostStartButton.gameObject.SetActive(IsServer);
            hostStartButton.onClick.RemoveAllListeners();
            hostStartButton.onClick.AddListener(OnClickStart);
        }
        if (readyToggle != null)
        {
            readyToggle.onValueChanged.RemoveAllListeners();
            readyToggle.onValueChanged.AddListener(OnToggleReady);
        }

        // Primera actualización
        UpdateLobbyUI();*/
        lobbyPanel.SetActive(true);
    }
    public override void OnNetworkSpawn()
    {
        /* base.OnNetworkSpawn();
         if (IsClient && IsOwner)
         {
             if (readyToggle != null)
             {
                 readyToggle.gameObject.SetActive(true);
             }
         }
         if (IsServer)
         {
             if (hostStartButton != null)
             {
                 hostStartButton.gameObject.SetActive(true);
                 hostStartButton.onClick.RemoveAllListeners();
                 hostStartButton.onClick.AddListener(OnClickStart);
             }
         }*/
        if (hostStartButton != null)
        {
            // Solo el Host (server) ve el botón de Start
            hostStartButton.gameObject.SetActive(IsServer);
            hostStartButton.onClick.RemoveAllListeners();
            hostStartButton.onClick.AddListener(OnClickStart);
        }
        if (readyToggle != null)
        {
            readyToggle.onValueChanged.RemoveAllListeners();
            readyToggle.onValueChanged.AddListener(OnToggleReady);
        }

        // Primera actualización
        UpdateLobbyUI();
        lobbyPanel.SetActive(false);
    }

    void Update()
    {
        // Actualizaciones simples de UI (contador e interactuabilidad del Start)
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

    // UI -> Network RPCs
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
