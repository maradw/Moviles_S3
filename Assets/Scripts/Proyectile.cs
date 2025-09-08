using System;
using Unity.Netcode;
using UnityEngine;

public class Proyectile : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    Rigidbody rb;
    float velocity = 10;
    void Start()
    {
        if (IsServer)
        {

            rb.AddForce(transform.forward * velocity, ForceMode.Impulse);
            Invoke("SimpleDespawn", 5);
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
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag== "enemy")
        {

        }
    }

    //[SerializeField] Rigidbody2D bullRigid;
    
 
    void Update()
    {
        
    }
}
