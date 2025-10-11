using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
public class SimplePlayerController : NetworkBehaviour
{
    Animator animator;
    float _speed = 4;
    Vector2 direction;
    [SerializeField] Rigidbody myRBD;
    [SerializeField] LayerMask layerName;
    float jumpForce = 3;
    [SerializeField]  bool canJump = false;
    [SerializeField] Transform firePoint;
    [SerializeField] GameObject projectilePrefab;
    Vector2 position;

    private bool isDead = false;
    public void OnClick(InputAction.CallbackContext click)
    {
        if (!IsOwner) return;
        if (click.performed)
        {
           // animator.SetTrigger("Attack");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 targetPoint = hit.point;
                Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

                ShootRpc(shootDirection);
            }
        }

    }
    public NetworkVariable<FixedString32Bytes> accountID = new();
    public NetworkVariable<int> health = new();
    public NetworkVariable<int> attack = new();

    public void SetData(PlayerData playerData)
    {
        accountID.Value = playerData.accountID;
        health.Value = playerData.health;
        attack.Value = playerData.attack;
        Debug.Log("SetData: posicion asignada = " + playerData.position);
        transform.position = playerData.position;
    }
    void Start()
    {
        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
        }
        animator = GetComponent<Animator>();
    }
    public void OnJump(InputAction.CallbackContext jump)
    {
        if (jump.performed && canJump == true)
        {
            JumpSetTriggerRpc("JumpOneTake");
            myRBD.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            canJump = false;
        }
    }
    public void OnMovement(InputAction.CallbackContext move)
    {
        if (!IsOwner) return;
        direction = move.ReadValue<Vector2>();
        float currentSpeed = direction.magnitude;
        animator.SetFloat("Speed", currentSpeed);
    }

    [Rpc(SendTo.Server)]
    public void JumpSetTriggerRpc(string animationName)
    {
        animator.SetTrigger(animationName);
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!IsOwner) return;
            AttackSetBoolRpc("Attack", true);

        }
        if (context.canceled)
        {
            if (!IsOwner) return;
            AttackSetBoolRpc("Attack", false);
        }

    }
    [Rpc(SendTo.Server)]
    public void AttackSetBoolRpc(string animationName, bool state)
    {
        animator.SetBool(animationName, state);
    }
    public void DamageRecieved(int damage)
    {
        health.Value -= damage;
    }
   public void BoostAttack(int buffAttack)
   {
       if (IsServer)
        {
            attack.Value += buffAttack;
            Debug.Log("current" + attack.Value);
        }
        else
        {
            BoostAttackServerRpc(buffAttack);
        }
    }

    [Rpc(SendTo.Server)]
    private void BoostAttackServerRpc(int buffAttack)
    {
        attack.Value += buffAttack;
        Debug.Log("current" + attack.Value);
    }

    private void Update()
    {
        if (IsServer)
        {
            if (!isDead && health.Value <= 0)
            {
                isDead = true;
                ulong ownerId = GetComponent<NetworkObject>().OwnerClientId;
                string accId = accountID.Value.ToString();
                GameManager.Instance.StartRespawnForClient(ownerId, accId, true);
                SimpleDespawn();
            }
        }
    }

    void SimpleDespawn()
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
            
    }
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        Vector3 move = new Vector3(direction.x, 0f, direction.y) * _speed;
        myRBD.linearVelocity = new Vector3(move.x, myRBD.linearVelocity.y, move.z);
        CheckGroundRpc();
    }

    [Rpc(SendTo.Server)]//not
    public void CheckGroundRpc()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit, 1.3f, layerName))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * hit.distance, Color.yellow);

            canJump = true;
            animator.SetBool("Grounded", true);
            animator.SetBool("FreeFall", false);
        }
        else
        {
            animator.SetBool("Grounded", false);
            animator.SetBool("FreeFall", true);
        }
    }
    [Rpc(SendTo.Server)]
    public void ShootRpc(Vector3 mouseDirection) 
    {
        Quaternion lookRotation = Quaternion.LookRotation(mouseDirection);
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, lookRotation);
        proj.GetComponent<Projectile>().shooter = this.gameObject;
        proj.GetComponent<Projectile>().attackFromPlayer = attack.Value;
        proj.GetComponent<Projectile>().shooter = this.gameObject;
        proj.GetComponent<NetworkObject>().Spawn(true);
        Debug.DrawRay(proj.transform.position, proj.transform.forward * 5, Color.red, 2f);
    }

    public override void OnNetworkDespawn()
    {
        print ("desconnected" + NetworkManager.Singleton.LocalClientId);
        GameManager.Instance.playerStatesByAccount[accountID.Value.ToString()] =
            new PlayerData(accountID.Value.ToString(), transform.position, health.Value, attack.Value);
        print("Me e desconectado " + NetworkManager.Singleton.LocalClientId + " y se a guardado la data de" + accountID.Value);
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameManager.Instance.SetCameraTarget(transform);
        }
    }
}
public class PlayerData
{
    public string accountID;
    public Vector3 position;
    public int health;
    public int attack;
    public PlayerData(string ID, Vector3 pos, int heal, int attck)
    {
       accountID = ID;
       position = pos;
       health = heal;
       attack = attck;
        //
    }
}
