using System.Linq;
using Unity.Netcode;
using System;
using UnityEngine;

public class Enemy : NetworkBehaviour
{

    [SerializeField] NetworkVariable<int> life = new NetworkVariable<int>(3);
    [SerializeField] Rigidbody rb;
    void Update()
    {
        if (!IsServer) return;

        FollowPlayer();
        if (life.Value <= 0)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
    private void OnEnable()
    {
        Projectile.OnEnemyCollision += ReduceLife;
    }
    private void OnDisable()
    {
        Projectile.OnEnemyCollision -= ReduceLife;
    }
    void ReduceLife()
    {
        life.Value -= 1;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (collision.gameObject.tag == "Player")
        {
            
        }
    }
    void FollowPlayer()
    {
        if (GameManager.Instance.Players == null || GameManager.Instance.Players.Count == 0) return;
        var closest = GameManager.Instance.Players
            .OrderBy(p => (p.transform.position - transform.position).sqrMagnitude) 
            .FirstOrDefault();

        if (closest != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                closest.transform.position,
                5f * Time.deltaTime
            );
        }
    }
}

