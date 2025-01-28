using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    PlayerInput playerInput;
    Rigidbody rgd;
    [SerializeField] float velocity;
    float x;
    float z;
    public float speedH = 2.0f;
    public float speedV = 2.0f;

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
        rgd.linearVelocity = transform.forward * z * velocity + transform.right * x * velocity;
    }

    public void MovePlayer(InputAction.CallbackContext context)
    {
        x = context.ReadValue<Vector2>().x;
        z = context.ReadValue<Vector2>().y;
    }

}
