using Unity.Netcode;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    PlayerInput playerInput;
    Animator animator;
    Rigidbody rgd;
    float velocity = 10;
    float x;
    float z;
    public Camera cam;

    CameraMovement camMovement;

    public float HorizontalSensitivity = 10;
    public float VerticalSensitivity = 10;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    public bool canMove = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rgd = gameObject.GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        camMovement = cam.GetComponent<CameraMovement>();
        camMovement.SetPlayerController(this);
    }

    // Update is called once per frame
    void Update()
    {
                    if (!IsOwner || !IsSpawned) return;

        if (canMove)
        {
            Debug.Log("MOVING");

            Vector3 linearVelocity = cam.gameObject.transform.forward * z * velocity + cam.gameObject.transform.right * x * velocity;
            linearVelocity = linearVelocity - rgd.linearVelocity;

            animator.SetBool("isWalking", linearVelocity.x > 0 || linearVelocity.z > 0); // start/stop walk cycle

            rgd.linearVelocity += new Vector3(linearVelocity.x, 0, linearVelocity.z);

            // Move body based on mouse movement
            yaw += HorizontalSensitivity * Input.GetAxis("Mouse X");
            //pitch -= VerticalSensitivity * Input.GetAxis("Mouse Y");

            gameObject.transform.eulerAngles = new Vector3(0, yaw, 0);
        }
        else
        {
            rgd.linearVelocity = Vector3.zero;
            rgd.angularVelocity = Vector3.zero;
        }
    }
    public void ToggleMovement(bool input)
    {
        canMove = input;
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
