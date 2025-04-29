using UnityEngine;
using Unity.Netcode;

public class NameTag : MonoBehaviour
{
    Transform playerCam;
    Vector3 offset;
    Transform target;
    private void Start()
    {
        offset = transform.localPosition;
        target = transform.parent;
        transform.SetParent(null);
        playerCam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<Camera>().transform;
    }
    // Update is called once per frame
    void Update()
    {
        transform.position = target.position + offset;
        transform.rotation = playerCam.rotation;
    }
}
