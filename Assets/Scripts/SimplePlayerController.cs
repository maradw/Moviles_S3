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

   // private InputSystem_Actions action;

    /* public void OnEnable()
     {

         action.Enable();
         action.Player.Move.performed += OnMovePerformed;
         action.Player.Move.canceled += OnMoveCanceled;
         action.Player.Jump.performed += OnJump;
     }*/
    Vector2 position;
    public void OnClick(InputAction.CallbackContext click)
    {

        if (click.performed)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 targetPoint = hit.point;
                Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

                ShootRpc(shootDirection);
                Debug.Log("a: " + shootDirection);
            }

            Debug.Log("has clicked");
        }

    }
    void Start()
    {
        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
        }
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        
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
        proj.GetComponent<NetworkObject>().Spawn(true);

        //  proj.GetComponent<Rigidbody>().AddForce(Vector3.forward * 5, ForceMode.Impulse);
        Debug.DrawRay(proj.transform.position, proj.transform.forward * 5, Color.red, 2f);
    }

}
