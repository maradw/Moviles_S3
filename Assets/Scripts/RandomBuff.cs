using Unity.Netcode;
using UnityEngine;
public class RandomBuff : MonoBehaviour
{
    void Start()
    {
       
    }
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
