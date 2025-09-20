using System;
using Unity.Netcode;
using UnityEngine;
public class RandomBuff : NetworkBehaviour
{
    public event Action<int> OnBuffColLision;
    [SerializeField] NetworkVariable<int> attackValue = new NetworkVariable<int>();

    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            attackValue.Value = UnityEngine.Random.Range(1, 3);
            Debug.Log("Servidor asigna random: " + attackValue.Value);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag== "Player")
        {
            AddBuffToPlayerRpc(NetworkManager.Singleton.LocalClientId);
            OnBuffColLision?.Invoke(attackValue.Value);
        }
    }
    [Rpc(SendTo.Server)]
    private void AddBuffToPlayerRpc(ulong playerID)
    {
        print("Aplicar buff a " + playerID);
        GetComponent<NetworkObject>().Despawn(true);
    }

    public int GetAttackValue() => attackValue.Value;
    public void TakeBuff() => GetComponent<NetworkObject>().Despawn(true);
}
