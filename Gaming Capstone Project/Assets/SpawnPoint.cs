using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public bool IsLobbySpawnPoint = false;

    private void Start()
    {
        if (IsLobbySpawnPoint)
            GameController.Instance.LobbySpawnPoints.Add(transform);
        else
            GameController.Instance.GameSpawnPoints.Add(transform);
    }
}
