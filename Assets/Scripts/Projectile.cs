using System;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    Rigidbody rb;
    float velocity = 10;
    public static event Action OnEnemyCollision;
   public int attackFromPlayer;
    public static event Action<int> OnPlayerCollision;
    void Start()
    {
        if (IsServer)
        {
            rb.AddForce(transform.forward * velocity, ForceMode.Impulse);
            
        }    
        Debug.Log("nose" + attackFromPlayer);
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    public void SimpleDespawn()
    {

        GetComponent<NetworkObject>().Despawn(true);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (other.gameObject.tag == "Wall"|| other.gameObject.tag == "buff" || other.gameObject.tag == "Ground")
        {
            SimpleDespawn();
        }
        else if(other.gameObject.tag == "Player")
        {
            OnPlayerCollision?.Invoke(attackFromPlayer);
        }
    }
}
