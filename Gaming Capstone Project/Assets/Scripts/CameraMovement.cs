using Unity.Netcode;
using UnityEngine;

public class CameraMovement : NetworkBehaviour
{
    public Transform Anchor;
    public float HorizontalSensitivity = 10f;
    public float VerticalSensitivity = 10f;

    public float yaw = 0.0f;
    public  float pitch = 180.0f; // Initial downward facing angle
    public PlayerController playerController;
    public bool canMove = true;

    // Start is called before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        if (canMove)
        {
            // Sync camera position and rotation with anchor
            transform.position = Anchor.position;
            transform.rotation = Anchor.rotation;

            // Get mouse input
            yaw += HorizontalSensitivity * Input.GetAxis("Mouse X");
            pitch -= VerticalSensitivity * Input.GetAxis("Mouse Y");

            // Clamp pitch to prevent unnatural flipping
            pitch = Mathf.Clamp(pitch, 90f, 270f);

            // Apply rotation to the anchor
            Anchor.eulerAngles = new Vector3(pitch, yaw, 180);
        }
    }

    public void SetPlayerController(PlayerController controller)
    {
        playerController = controller;
    }
}
