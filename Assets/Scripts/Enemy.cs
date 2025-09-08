using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Enemy : NetworkBehaviour
{
    
     [SerializeField] NetworkVariable<int> life = new NetworkVariable<int>(6);
    void Update()
    {
        if (!IsServer) return;

        FollowPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
          //  GetComponent<NetworkObject>().Despawn(true);
        }

        if (other.gameObject.tag == "bullet")
        {
            life.Value -= 1;
        }
        if (life.Value <= 0)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) { }

        
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

