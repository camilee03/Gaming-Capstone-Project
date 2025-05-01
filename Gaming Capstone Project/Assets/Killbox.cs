using Unity.Netcode;
using UnityEngine;

public class Killbox : NetworkBehaviour
{
    public RoomManager roomManager;

    private void OnCollisionEnter(Collision other)
    {
        Debug.Log("&& Collided with " +  other.gameObject);
        if (!IsServer) return;

        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("&& Collided with actual player");

            if (roomManager.spawnPoints.Count <= 0)
            {
                Debug.Log("No Spawn Points");
                roomManager.InitializeSpawnPoints();
            }
            int randomnum = Random.Range(0, roomManager.spawnPoints.Count);
            Debug.Log(roomManager.spawnPoints.Count + " " + randomnum);

            var spawnpoint = roomManager.spawnPoints[randomnum].transform;
            var playerController = other.gameObject.GetComponent<PlayerController>();

            if (playerController != null)
            {
                Debug.Log("&& IS VALID");
                playerController.TeleportServerRpc(spawnpoint.position, spawnpoint.rotation);
            }
        }
    }
}
