using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
public struct LobbyPlayerState : INetworkSerializable, IEquatable<LobbyPlayerState>
{
    public ulong ClientId;
    public bool Ready;

    public LobbyPlayerState(ulong clientId, bool ready)
    {
        ClientId = clientId;
        Ready = ready;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Ready);
    }

    public bool Equals(LobbyPlayerState other)
    {
        return ClientId == other.ClientId && Ready == other.Ready;
    }
}

public class GameManager : NetworkBehaviour
{
    private static GameManager instance;
    public GameObject playerprefab;
    public Action OnConnection;
    public List<GameObject> Players = new List<GameObject>();
    public Dictionary<string, PlayerData> playerStatesByAccount = new();
    [SerializeField] CinemachineCamera cameraRef;

    public const int MaxLobbyPlayers = 5;
    public NetworkList<LobbyPlayerState> LobbyPlayers = new NetworkList<LobbyPlayerState>();
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string gameplaySceneName = "GameScene";

    void Awake()
    {
        if (Instance == null)
        {
            instance = this;
            DontDestroyOnLoad(Instance);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        }

        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }

        OnConnection?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        if (LobbyPlayers.Count >= MaxLobbyPlayers)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
            Debug.Log("Lobby lleno. Cliente rechazado" + clientId);
            return;
        }
        for (int i = 0; i < LobbyPlayers.Count; i++)
        {
            if (LobbyPlayers[i].ClientId == clientId)
                return;
        }

        LobbyPlayers.Add(new LobbyPlayerState(clientId, false));
    }

    private void HandleDisconnect(ulong clientID)
    {
        print("El jugador" + clientID + "Se a desconectado");

        if (!IsServer) return;
        for (int i = 0; i < LobbyPlayers.Count; i++)
        {
            if (LobbyPlayers[i].ClientId == clientID)
            {
                LobbyPlayers.RemoveAt(i);
                break;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void RegisterPlayerServerRpc(string accountID, ulong ID)
    {
        if (!playerStatesByAccount.TryGetValue(accountID, out PlayerData data))
        {
            PlayerData NewData = new PlayerData(accountID, Vector3.zero, 100, 5);
            playerStatesByAccount[accountID] = NewData;
            SpawnPlayerServer(ID, NewData);
        }
        else
        {
            SpawnPlayerServer(ID, data);
        }
    }

    public void SetCameraTarget(Transform playerTransform)
    {
        cameraRef.Follow = playerTransform;
        cameraRef.LookAt = playerTransform;
    }

    public void SpawnPlayerServer(ulong ID, PlayerData data)
    {
        if (!IsServer) return;
        Vector3 spawnPos = data.position;
        GameObject player = Instantiate(playerprefab, spawnPos, Quaternion.identity);
        var netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(ID, true);

        player.GetComponent<SimplePlayerController>().SetData(data);
    }

    public Vector3 Respawn()
    {
        Vector3 rndRespawn = new Vector3(UnityEngine.Random.Range(-6, 6), 0.5f, UnityEngine.Random.Range(-6, 6));
        return rndRespawn;
    }
    public void StartRespawnForClient(ulong clientId, string accountID, bool isDeathRespawn = true)
    {
        StartCoroutine(RespawnCoroutine(clientId, accountID, isDeathRespawn));
    }

    private IEnumerator RespawnCoroutine(ulong clientId, string accountID, bool isDeath)
    {
        yield return new WaitForSeconds(3f);
        RespawnPlayerForClient(clientId, accountID, isDeath);
    }

    public void RespawnPlayerForClient(ulong clientId, string accountID, bool isDeath)
    {
        if (!playerStatesByAccount.TryGetValue(accountID, out PlayerData data))
        {
            data = new PlayerData(accountID, Respawn(), 100, 5);
            playerStatesByAccount[accountID] = data;
        }

        if (isDeath)
        {
            Vector3 rand = Respawn();
            data.position = rand;
            data.health = 100;
        }

        GameObject player = Instantiate(playerprefab, data.position, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
        player.GetComponent<SimplePlayerController>().SetData(data);
    }

    [Rpc(SendTo.Server)]
    public void SetReadyRpc(bool ready, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong sender = rpcParams.Receive.SenderClientId;
        for (int i = 0; i < LobbyPlayers.Count; i++)
        {
            if (LobbyPlayers[i].ClientId == sender)
            {
                var s = LobbyPlayers[i];
                s.Ready = ready;
                LobbyPlayers[i] = s;
                break;
            }
        }
    }

    private bool AreAllPlayersReady()
    {
        if (LobbyPlayers == null || LobbyPlayers.Count == 0) return false;
        for (int i = 0; i < LobbyPlayers.Count; i++)
        {
            if (!LobbyPlayers[i].Ready) return false;
        }
        return true;
    }

    [Rpc(SendTo.Server)]
    public void RequestStartGameRpc(RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
        {
            Debug.Log("Solo el Host puede iniciar la partida.");
            return;
        }

        if (!AreAllPlayersReady())
        {
            Debug.Log("No todos los jugadores están listos.");
            return;
        }

        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogError("Gameplay Scene Name no asignado en GameManager.");
            return;
        }
        NetworkManager.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }
    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;
        if (!string.Equals(sceneName, gameplaySceneName, StringComparison.Ordinal)) return;

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            string accId = clientId.ToString();
            if (!playerStatesByAccount.TryGetValue(accId, out var data))
            {
                data = new PlayerData(accId, Respawn(), 100, 5);
                playerStatesByAccount[accId] = data;
            }

            SpawnPlayerServer(clientId, data);
        }
    }

    public static GameManager Instance => instance;
}
