using Unity.Netcode;
using UnityEngine;

public class SpawnPoint : NetworkBehaviour
{
    public void Start()
    {
        if (IsServer)
        {
            GameController.Instance.RegisterSpawnPoint(this.transform);
        }
    }
}
