
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    //public NetworkVariable<ulong> PlayerID;
    private static GameManager instance;
    public Transform playerprefab;
    [SerializeField] GameObject randomBuff;
    float currentBuffCount;
    float BuffSpawnCount = 4;

    float currentEnemy;
    float enemyCount = 4;
    [SerializeField] GameObject enemyRandom;


    public List<GameObject> Players = new List<GameObject>();
    void Start()
    {
        
    }

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
    
    //
    public override void OnNetworkSpawn()
    {
        print("CurrentPlayer" + NetworkManager.Singleton.ConnectedClients.Count);
        print(NetworkManager.Singleton.LocalClientId);
        
        InstancePLayerRPC(NetworkManager.Singleton.LocalClientId);
        
    }

    [Rpc(SendTo.Server)]
   public void InstancePLayerRPC(ulong ownerID)
    {
        Transform player = Instantiate(playerprefab);
        player.GetComponent<NetworkObject>().SpawnWithOwnership(ownerID, true);
        RegisterPlayer(this.gameObject);

    }
    void Update()
    {
        if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            currentEnemy += Time.deltaTime;

            if (currentEnemy > enemyCount)
            {

                Vector3 randomPos = new Vector3(Random.Range(-8, 8), 0.5f, Random.Range(-8, 8));
                GameObject buff = Instantiate(enemyRandom, randomPos, Quaternion.identity);
                
                buff.GetComponent<NetworkObject>().Spawn(true);
                currentEnemy = 0;
            }
        }

        if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            currentBuffCount += Time.deltaTime;

            if (currentBuffCount > BuffSpawnCount)
            {

                Vector3 randomPos = new Vector3(Random.Range(-8, 8), 0.5f, Random.Range(-8, 8));
                GameObject buff = Instantiate(randomBuff, randomPos, Quaternion.identity);

                buff.GetComponent<NetworkObject>().Spawn(true);
                currentBuffCount = 0;
            }
        }
    }

    public void RegisterPlayer(GameObject player)
    {
        Players.Add(player);
    }

    
    public static GameManager Instance => instance;
}
