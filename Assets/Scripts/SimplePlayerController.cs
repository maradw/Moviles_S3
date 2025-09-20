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
    /* [SerializeField] NetworkVariable<int> life = new NetworkVariable<int>(100);
     [SerializeField] NetworkVariable<int> attack = new NetworkVariable<int>(20);*/



    //life no se actualiza al momento de hacer respawn , lo cual ocasiona que se continue con el bucle de la corrutina
    public void OnClick(InputAction.CallbackContext click)
    {
        if (!IsOwner) return;
        if (click.performed)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 targetPoint = hit.point;
                Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

                ShootRpc(shootDirection);
                //Debug.Log("a: " + shootDirection);
            }

           // Debug.Log("has clicked");
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
        transform.position = playerData.position;
    }
    void Start()
    {
      //  health.Value = 100;
        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
        }
        animator = GetComponent<Animator>();
        string id = accountID.Value.ToString();
        if (GameManager.Instance.playerStatesByAccount.ContainsKey(id))
        {
            SetData(GameManager.Instance.playerStatesByAccount[id]);
            // Si estaba muerto, inicia respawn
            if (health.Value <= 0)
            {
                isDead = true;
                StartCoroutine(ReSpawn());
            }
        }
        else
        {
            SetData(new PlayerData(id, GetRandomSpawnPosition(), 100, 20));
        }
    }
    public void OnJump(InputAction.CallbackContext jump)
    {
        if (jump.performed && canJump == true)
        {
            JumpSetTriggerRpc("Jump");
            myRBD.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            canJump = false;
        }
    }
    public void OnMovement(InputAction.CallbackContext move)
    {
        if (!IsOwner) return;
        direction = move.ReadValue<Vector2>();
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
        }
        
    }
    public void DamageRecieved(int damage)
    {
        health.Value -= damage;
    }
    void BoostAttack(int buffAttack)
    {//////////////////
        attack.Value += buffAttack;
        Debug .Log("current" + attack.Value);
    }
    /* private void OnEnable()
     {
         RandomBuff.OnBuffColLision += BoostAttack;
         Projectile.OnPlayerCollision += DamageRecieved;
     }
     private void OnDisable()
     {
         RandomBuff.OnBuffColLision -= BoostAttack;
         Projectile.OnPlayerCollision -= DamageRecieved;
     }*/
    private void OnTriggerEnter(Collider other)
    {
        RandomBuff buff = other.GetComponent<RandomBuff>();
        if (buff != null)
        {
            BoostAttack(buff.GetAttackValue());
            buff.TakeBuff(); // método para destruir el buff
        }

        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.OnPlayerCollision += DamageRecieved;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        RandomBuff buff = other.GetComponent<RandomBuff>();
        if (buff != null)
        {
            buff.OnBuffColLision -= BoostAttack;
        }

        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.OnPlayerCollision -= DamageRecieved;
        }
    }
    private bool isDead = false;

    private void Update()
    {
        if (health.Value <= 0 && !isDead && IsOwner)
        {
            isDead = true;
            StartCoroutine(ReSpawn());
        }
    }
    //*********************************************
    IEnumerator ReSpawn()
    {
        yield return new WaitForSeconds(3f);
        RespawnServerRpc();
    }
    void SimpleDespawn()
    {
        GetComponent<NetworkObject>().Despawn(true);
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
        proj.GetComponent<Projectile>().attackFromPlayer = attack.Value;
        proj.GetComponent<Projectile>().shooter = this.gameObject;
        proj.GetComponent<NetworkObject>().Spawn(true);
        Debug.DrawRay(proj.transform.position, proj.transform.forward * 5, Color.red, 2f);
    }

    public override void OnNetworkDespawn()
    {
        print ("desconnected" + NetworkManager.Singleton.LocalClientId);
        GameManager.Instance.playerStatesByAccount[accountID.Value.ToString()] = new PlayerData(accountID.Value.ToString(), transform.position, health.Value, attack.Value);
        print("Me e desconectado " + NetworkManager.Singleton.LocalClientId + " y se a guardado la data de" + accountID.Value);
    }

    void RespawnPLayer()
    {
        var data = new PlayerData(accountID.Value.ToString(), GetRandomSpawnPosition(), health.Value = 100, attack.Value);
        health.Value = 100;
        SetData(data);
        isDead = false;
        GameManager.Instance.playerStatesByAccount[accountID.Value.ToString()] = data;
    }
    private Vector3 GetRandomSpawnPosition()
    {
        // Cambia estos valores según el tamaño de tu mapa
        float x = UnityEngine.Random.Range(-7f, 7f);
        float z = UnityEngine.Random.Range(-7f, 7f);
        float y = 1f; // Altura adecuada para tu escenario
        return new Vector3(x, y, z);
    }
    [Rpc(SendTo.Server)]
    public void RespawnServerRpc()
    {
        health.Value = 100;
        transform.position = GetRandomSpawnPosition();
        isDead = false;
        // Si quieres, actualiza el GameManager aquí también
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
