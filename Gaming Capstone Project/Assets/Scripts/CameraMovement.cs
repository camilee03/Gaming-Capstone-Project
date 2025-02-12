using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform Anchor;
    public float HorizontalSensitivity = 10;
    public float VerticalSensitivity = 10;

    public float MinPitch = -80f; // Minimum looking down angle
    public float MaxPitch = 80f;  // Maximum looking up angle

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Synchronize position and rotation with the anchor
        transform.position = Anchor.position;
        transform.rotation = Anchor.rotation;

        // Get mouse input
        yaw += HorizontalSensitivity * Input.GetAxis("Mouse X");
        pitch -= VerticalSensitivity * Input.GetAxis("Mouse Y");

        // Clamp the pitch to prevent excessive up/down looking
        pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);

        // Apply rotation to the anchor
        Anchor.eulerAngles = new Vector3(pitch, yaw, 0);

        // Unlock cursor if ESC is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
