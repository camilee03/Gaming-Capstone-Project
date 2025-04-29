using System.Globalization;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Killbox : NetworkBehaviour
{

    public RoomManager roomManager;
    private void Start()
    {
    }
    private void OnCollisionEnter(Collision collision)
    {

        if(collision.gameObject.tag == "Player")
        {
            if (roomManager.spawnPoints.Count == 0) roomManager.InitializeSpawnPoints();
                GameObject obj = collision.gameObject;
            Transform spawnpoint = roomManager.spawnPoints[Random.Range(0, roomManager.spawnPoints.Count)].transform;
                 if (IsOwner)
                {
                var netTransform = obj.GetComponent<NetworkTransform>();
                if (netTransform != null)
                {
                    netTransform.Teleport(spawnpoint.position, spawnpoint.rotation, Vector3.one * 0.75f);
                }
                else
                {
                    obj.transform.position = spawnpoint.position;
                    obj.transform.rotation = spawnpoint.rotation;
                }

            }
        }

    }
}
