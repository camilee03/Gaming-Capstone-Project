using UnityEngine;
using UnityEngine.InputSystem;

public class NewPlayerController : MonoBehaviour
{
    PlayerInput playerInput;
    Rigidbody rgd;
    float velocity = 10;
    float x;
    float z;
    float speedH = 10;
    float speedV = 10;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rgd = gameObject.GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    // Update is called once per frame
    void Update()
    {
        yaw += speedH * Input.GetAxis("Mouse X");
        pitch -= speedV * Input.GetAxis("Mouse Y");

        transform.eulerAngles = new Vector3(pitch, yaw, 0);
        Vector3 linearVelocity = transform.forward * z * velocity + transform.right * x * velocity;
        linearVelocity = linearVelocity - rgd.linearVelocity;

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
