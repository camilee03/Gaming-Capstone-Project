using Unity.Netcode;
using UnityEngine;

public class STOPCOLLIDINGWITHDOOR : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        GameObject parent = collision.gameObject;
        while (parent.transform.parent != null)
        {
            parent = parent.transform.parent.gameObject;
        }

        if (parent.tag != "Player")
        {
            NetworkObject obj = collision.gameObject.GetComponent<NetworkObject>();
            if (obj != null) { obj.Despawn(true); }
            else { GameObject.Destroy(collision.gameObject); }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        GameObject parent = collision.gameObject;
        while (parent.transform.parent != null)
        {
            parent = parent.transform.parent.gameObject;
        }

        if (parent.tag != "Player")
        {
            NetworkObject obj = collision.gameObject.GetComponent<NetworkObject>();
            if (obj != null) { obj.Despawn(true); }
            else { GameObject.Destroy(collision.gameObject); }
        }
    }
}
