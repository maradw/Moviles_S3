using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
public class SimplePlayerController : NetworkBehaviour
{
    Animator animator;
    float _speed = 4;
    Vector2 direction;
    [SerializeField] Rigidbody myRBD;
    [SerializeField] LayerMask layerName;
    float jumpForce = 3;
    [SerializeField]  bool canJump = false;
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    public void OnJump(InputAction.CallbackContext jump)
    {
        if (jump.performed && canJump == true)
        {
            Debug.Log("a");
            JumpSetTriggerRpc("Jump");
            myRBD.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            canJump = false;
        }
    }
    public void OnMovement(InputAction.CallbackContext move)
    {
        print("A");
        if (!IsOwner) return;
        direction = move.ReadValue<Vector2>();
    }

    [Rpc(SendTo.Server)]
    public void JumpSetTriggerRpc(string animationName)
    {
        animator.SetTrigger(animationName);
    }
    private void FixedUpdate()
    {
        //if (!IsOwner) return;
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

}
