using Unity.Netcode;
using UnityEngine;

public class Killbox : NetworkBehaviour
{
    public RoomManager roomManager;

    private void OnCollisionStay(Collision collision)
    {
        Debug.Log("&& Collided with " +  collision.gameObject);
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("&& Collided with actual player");

            if (roomManager.spawnPoints.Count <= 0)
                roomManager.InitializeSpawnPoints();

            var spawnpoint = roomManager.spawnPoints[Random.Range(0, roomManager.spawnPoints.Count)].transform;
            var playerController = collision.gameObject.GetComponent<PlayerController>();

            if (playerController != null)
            {
                Debug.Log("&& IS VALID");
                playerController.TeleportServerRpc(spawnpoint.position, spawnpoint.rotation);
            }
        }
    }
}
