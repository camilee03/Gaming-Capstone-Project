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
    private void OnDestroy()
    {
        Debug.Log("Follower Destroyed.");
    }
    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(target.position.x, 20, target.position.z);
    }
}
