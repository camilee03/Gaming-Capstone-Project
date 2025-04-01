using Unity.Netcode;
using UnityEngine;

public class SpawnPoint : NetworkBehaviour
{
    public bool IsLobbySpawnPoint = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return; // Optional: only set on the local client
        if (IsLobbySpawnPoint)
            GameController.Instance.LobbySpawnPoint = transform;
        else
            GameController.Instance.GameSpawnPoint = transform;
    }
}
