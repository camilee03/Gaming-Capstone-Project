using Unity.Netcode;
using UnityEngine;

public class CameraMovement : NetworkBehaviour
{
    public Transform Anchor;
    public float HorizontalSensitivity = 10f;
    public float VerticalSensitivity = 10f;

    private float yaw = 180.0f;
    private float pitch = 0.0f;

    public PlayerController playerController;
    public bool canMove = true;
    public bool debugOffline = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !debugOffline)
        {
            gameObject.SetActive(false);  // Important: disable non-owner cameras
        }
    }

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

        canMove = playerController.canMove;

        if (!canMove) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * HorizontalSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * VerticalSensitivity;

        yaw += mouseX;
        pitch -= mouseY;

        pitch = Mathf.Clamp(pitch, -60f, 60f);
        Anchor.rotation = Quaternion.Euler(pitch, yaw, Anchor.rotation.z);

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
