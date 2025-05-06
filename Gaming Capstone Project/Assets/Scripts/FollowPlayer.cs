using UnityEngine;
using Unity.Netcode;

public class FollowPlayer : NetworkBehaviour
{
    Transform target;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!IsOwner)
        {
            Destroy(gameObject);
        }
        else
        {
            target = transform.parent;
            transform.SetParent(null);
        }
    }
    void Update()
    {
        if (target != null && transform != null) { transform.position = new Vector3(target.position.x, 20, target.position.z); }
    }
}
