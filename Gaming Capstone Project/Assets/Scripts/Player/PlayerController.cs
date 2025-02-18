using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    PlayerInput playerInput;
    Animator animator;
    Rigidbody rgd;
    float velocity = 10;
    float x;
    float z;
    Camera cam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rgd = gameObject.GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 linearVelocity = cam.gameObject.transform.forward * z * velocity + cam.gameObject.transform.right * x * velocity;
        linearVelocity = linearVelocity - rgd.linearVelocity;

        animator.SetBool("isWalking", linearVelocity.x > 0 || linearVelocity.z > 0); // start/stop walk cycle

        rgd.linearVelocity += new Vector3(linearVelocity.x, 0, linearVelocity.z);
    }

    public void MovePlayer(InputAction.CallbackContext context)
    {
        x = context.ReadValue<Vector2>().x;
        z = context.ReadValue<Vector2>().y;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            rgd.AddForce(Vector3.up * 200);
        }
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.performed) { velocity = 20; }
        else { velocity = 10; }
    }

}
