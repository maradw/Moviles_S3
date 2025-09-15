
using System;
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
       GameObject player = Instantiate(playerprefab);
        player.GetComponent<NetworkObject>().SpawnWithOwnership(ID, true);
       

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
        if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            /* currentEnemy += Time.deltaTime;

             if (currentEnemy > enemyCount)
             {

                 Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-8, 8), 0.5f, UnityEngine.Random.Range(-8, 8));
                 GameObject buff = Instantiate(enemyRandom, randomPos, Quaternion.identity);

                 buff.GetComponent<NetworkObject>().Spawn(true);
                 currentEnemy = 0;
             }*/
        }

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
    Vector3 Respawn()
    {
        Vector3 rndRespawn = new Vector3(UnityEngine.Random.Range(-5, 5), 0.5f, UnityEngine.Random.Range(-5, 5));
        return rndRespawn;
    }
    public void RegisterPlayer(GameObject player)
    {
        Players.Add(player);
    }
    public static GameManager Instance => instance;
}
