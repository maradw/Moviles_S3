using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    //public NetworkVariable<ulong> PlayerID;
    private static GameManager instance;
    public GameObject playerprefab;
    [SerializeField] GameObject randomBuff;
    float currentBuffCount;
    float BuffSpawnCount = 4;

    float currentEnemy;
    float enemyCount = 4;
    //[SerializeField] GameObject enemyRandom;


    public Action OnConnection;

    public List<GameObject> Players = new List<GameObject>();


    public Dictionary<string, PlayerData> playerStatesByAccount = new();
    void Awake()
    {
        if(Instance == null)
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
        /*   print("CurrentPlayer" + NetworkManager.Singleton.ConnectedClients.Count);
           print(NetworkManager.Singleton.LocalClientId);

           InstancePLayerRPC(NetworkManager.Singleton.LocalClientId);*/
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;

        OnConnection?.Invoke();
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;

    }

    private void HandleDisconnect(ulong clientID)
    {
        // throw new NotImplementedException();
        print("El jugador" + clientID + "Se a desconectado");
    }
    [Rpc(SendTo.Server)]
    public void RegisterPlayerServerRpc(string accountID, ulong ID)
    {
        if(!playerStatesByAccount.TryGetValue(accountID, out PlayerData data))
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

    public void SpawnPlayerServer(ulong ID, PlayerData data)
    {
        if (!IsServer) return;

        Vector3 spawnPos = data.position;
        Debug.Log($"SpawnPlayerServer -> Instanciando jugador {ID} en {spawnPos}");

        // Instanciar en la posición deseada ANTES de hacer SpawnWithOwnership
        GameObject player = Instantiate(playerprefab, spawnPos, Quaternion.identity);
        var netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(ID, true);

        player.GetComponent<SimplePlayerController>().SetData(data);
    }

    /* [Rpc(SendTo.Server)]
    public void InstancePLayerRPC(ulong ownerID)
    {
         Transform player = Instantiate(playerprefab);
         player.GetComponent<NetworkObject>().SpawnWithOwnership(ownerID, true);
         RegisterPlayer(player.gameObject);

     }*/
    void Update()
    {
        /*if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            /* currentEnemy += Time.deltaTime;

             if (currentEnemy > enemyCount)
             {

                 Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-8, 8), 0.5f, UnityEngine.Random.Range(-8, 8));
                 GameObject buff = Instantiate(enemyRandom, randomPos, Quaternion.identity);

                 buff.GetComponent<NetworkObject>().Spawn(true);
                 currentEnemy = 0;
             }*/
    

        //waza
        if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            currentBuffCount += Time.deltaTime;

            if (currentBuffCount > BuffSpawnCount)
            {

                Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-8, 8), 0.5f, UnityEngine.Random.Range(-8, 8));
                GameObject buff = Instantiate(randomBuff, randomPos, Quaternion.identity);

                buff.GetComponent<NetworkObject>().Spawn(true);
                currentBuffCount = 0;
            }
        }
    }
    public Vector3 Respawn()
    {
        Vector3 rndRespawn = new Vector3(UnityEngine.Random.Range(-6, 6), 0.5f, UnityEngine.Random.Range(-6, 6));
        return rndRespawn;
    }
    public void RegisterPlayer(GameObject player)
    {
        Players.Add(player);
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

    public void RespawnPlayerForClient(ulong clientId, string accountID, bool porMuerte)
    {
        Debug.Log($"RespawnPlayerForClient llamado para {accountID} (clientId: {clientId}) porMuerte={porMuerte}");

        // Asegurarnos de tener data (si no existe, crear con posición random)
        if (!playerStatesByAccount.TryGetValue(accountID, out PlayerData data))
        {
            data = new PlayerData(accountID, Respawn(), 100, 5);
            playerStatesByAccount[accountID] = data;
            Debug.Log($"No existía data -> creada con posición inicial {data.position}");
        }

        if (porMuerte)
        {
            Vector3 rand = Respawn();
            data.position = rand;
            data.health = 100;
            Debug.Log($"Por muerte: asignada posición random {rand}");
        }
        else
        {
            Debug.Log($"Por reconexión: usando posición guardada {data.position}");
        }

        // Instanciar EN la posición final que queremos que tenga el jugador
        GameObject player = Instantiate(playerprefab, data.position, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
        player.GetComponent<SimplePlayerController>().SetData(data);
    }
    public static GameManager Instance => instance;
}
