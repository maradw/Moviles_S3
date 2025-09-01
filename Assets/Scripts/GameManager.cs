
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    //public NetworkVariable<ulong> PlayerID;
    private static GameManager instance;
    [SerializeField] Transform playerprefab;
    [SerializeField] GameObject randomBuff;
    float currentBuffCount;
        float BuffSpawnCount = 4;
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
       // player.GetComponent<SimplePlayerController>().PlayerID.Value = id;
        player.GetComponent<NetworkObject>().SpawnWithOwnership(ownerID, true);

    }
    void Update()
    {
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
    public static GameManager Instance => instance;
}
