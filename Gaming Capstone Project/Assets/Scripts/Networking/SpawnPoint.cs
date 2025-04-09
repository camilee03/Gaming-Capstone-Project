using Unity.Netcode;
using UnityEngine;

public class SpawnPoint : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GameController.Instance.RegisterSpawnPoint(this.transform);
        }
    }
}
