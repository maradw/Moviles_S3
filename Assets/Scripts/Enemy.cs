//using System.Linq;
//using Unity.Netcode;
//using UnityEngine;
//using static UnityEngine.GraphicsBuffer;
//public class Enemy : MonoBehaviour
//{

//    void Start()
//    {

//    }
//    void Update()
//    {
//        FollowPlayer();

//    }
//    private void OnTriggerEnter(Collider other)
//    {
//        if(other.gameObject.tag== "Player")
//        {
//            AddEnemyPlayerRpc(NetworkManager.Singleton.LocalClientId);
//        }
//    }
//    [Rpc(SendTo.Server)]
//    private void AddEnemyPlayerRpc(ulong playerID)
//    {
//        GetComponent<NetworkObject>().Despawn(true);
//    }
//    /*void FollowPLayer()
//    {
//        transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, 4 * Time.deltaTime);
//    }*/
//    /*  public void SetTarget(Transform t)
//      {
//          targetPlayer = t;
//      }*/
//    void FollowPlayer()
//    {
//        if (GameManager.Instance.Players.Count == 0) return;

//        // Obt�n el jugador m�s cercano sin foreach ni Find
//        var closest = GameManager.Instance.Players
//            .OrderBy(p => Vector3.Distance(transform.position, p.transform.position))
//            .FirstOrDefault();

//        if (closest != null)
//        {
//            transform.position = Vector3.MoveTowards(
//                transform.position,
//                closest.transform.position,
//               5 * Time.deltaTime
//            );
//        }
//    }

//}
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Enemy : NetworkBehaviour
{
    void Update()
    {
        // Solo el servidor mueve a los enemigos (importante para netcode)
        if (!IsServer) return;

        FollowPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            // El enemigo desaparece al colisionar con un jugador
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    void FollowPlayer()
    {
        if (GameManager.Instance.Players == null || GameManager.Instance.Players.Count == 0) return;

        // Obt�n el jugador m�s cercano usando LINQ
        var closest = GameManager.Instance.Players
            .OrderBy(p => (p.transform.position - transform.position).sqrMagnitude) // m�s r�pido que Distance
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