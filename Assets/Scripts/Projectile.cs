using System;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    Rigidbody rb;
    float velocity = 10;
    public static event Action OnEnemyCollision;
    void Start()
    {
        if (IsServer)
        {
            rb.AddForce(transform.forward * velocity, ForceMode.Impulse);
            
        }     
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
        if (other.gameObject.tag == "enemy")
        {
            OnEnemyCollision?.Invoke();
        }
    }
}
