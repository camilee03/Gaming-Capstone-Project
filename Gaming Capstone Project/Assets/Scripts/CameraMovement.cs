using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform Anchor;
    public float HorizontalSensitivity = 10;
    public float VerticalSensitivity = 10;

    private float yaw = 0.0f;
    private float pitch = -90.0f;
    public PlayerController playerController;
    public bool canMove = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        //player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (canMove)
        {
            gameObject.transform.position = Anchor.transform.position;
            gameObject.transform.rotation = Anchor.transform.rotation;
            yaw += HorizontalSensitivity * Input.GetAxis("Mouse X");
            pitch -= VerticalSensitivity * Input.GetAxis("Mouse Y");

            Anchor.transform.eulerAngles = new Vector3(pitch, yaw, 180);
        }
        
    }

    public void SetPlayerController(PlayerController controller)
    {
        playerController = controller;
    }
}
