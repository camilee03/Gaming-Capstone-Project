using Unity.Netcode;
using UnityEngine;

public class CameraMovement : NetworkBehaviour
{
    public Transform Anchor;
    public float HorizontalSensitivity = 10;
    public float VerticalSensitivity = 10;
    public Camera playerCamera;

    private float yaw = 0.0f;
    private float pitch = -90.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //player = GameObject.Find("Player");
    }

        public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            playerCamera.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.position = Anchor.transform.position;
        gameObject.transform.rotation = Anchor.transform.rotation;
        yaw += HorizontalSensitivity * Input.GetAxis("Mouse X");
        pitch -= VerticalSensitivity * Input.GetAxis("Mouse Y");

        Anchor.transform.eulerAngles = new Vector3(pitch, yaw, 180);
    }
}
