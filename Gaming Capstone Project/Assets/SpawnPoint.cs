using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public bool IsLobbySpawnPoint = false;

    private void Start()
    {
        if (IsLobbySpawnPoint)
            GameController.Instance.LobbySpawnPoint = transform;
        else
            GameController.Instance.GameSpawnPoint = transform;
    }
}
