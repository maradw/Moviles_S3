using UnityEngine;
using Unity.Netcode;
public class RandomBuff : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag== "Player")
        {
            AddBuffToPlayerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }
    [Rpc(SendTo.Server)]
    private void AddBuffToPlayerRpc(ulong playerID)
    {
        print("Aplicar buff a " + playerID);
        GetComponent<NetworkObject>().Despawn(true);
    }

    
}
