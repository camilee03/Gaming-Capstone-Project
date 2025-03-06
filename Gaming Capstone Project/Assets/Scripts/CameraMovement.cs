using Unity.Netcode;
using UnityEngine;

public class CameraMovement : NetworkBehaviour
{
    public Transform Anchor;
    public float HorizontalSensitivity = 10f;
    public float VerticalSensitivity = 10f;

    private float yaw = 180.0f;  // Start looking backwards (0,180,0)
    private float pitch = 0.0f;  // Start with a neutral pitch

    public PlayerController playerController;
    public bool canMove = true;
    public bool debugOffline = false;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        if (playerController != null)
        {
            debugOffline = playerController.debugOffline;
        }
    }

    private void Update()
    {
        if (!debugOffline && (!IsOwner || !IsSpawned)) return;
        if (!canMove) return;

        // Get raw mouse input for instant responsiveness
        float mouseX = Input.GetAxisRaw("Mouse X") * HorizontalSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * VerticalSensitivity;

        // Apply rotation
        yaw += mouseX;
        pitch -= mouseY;

        // Clamp pitch to prevent unnatural flipping
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        // Apply rotation to the anchor
        Anchor.rotation = Quaternion.Euler(pitch, yaw, 0);

        // Keep camera position locked to anchor
        if (transform.position != Anchor.position)
        {
            transform.position = Anchor.position;
        }
    }

    public void SetPlayerController(PlayerController controller)
    {
        playerController = controller;
    }
}
