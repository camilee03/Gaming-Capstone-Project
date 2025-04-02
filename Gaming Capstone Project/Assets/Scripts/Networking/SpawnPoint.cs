using Unity.Netcode;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public bool IsLobbySpawnPoint = false;

    public void Start()
    {
        if (IsLobbySpawnPoint)
        {
            GameController.Instance.LobbySpawnPoint = transform;
        }
        else
            GameController.Instance.GameSpawnPoint = transform;
    }
}
