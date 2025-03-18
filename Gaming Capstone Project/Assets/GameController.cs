using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameController : NetworkBehaviour
{
    public static GameController Instance { get; private set; }

    public Dictionary<ulong, GameObject> Players = new Dictionary<ulong, GameObject>();

    public List<Transform> LobbySpawnPoints = new List<Transform>();
    public List<Transform> GameSpawnPoints = new List<Transform>();

    private int numberOfDopples = 1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        GameObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;

        Players.Add(clientId, playerObj);

        playerObj.name = $"Player{Players.Count}";

        Debug.Log($"Player connected: {playerObj.name} with ClientId: {clientId}");

        // Teleport only the newly joined player
        
        Transform spawnPoint = LobbySpawnPoints[0];
        playerObj.transform.position = spawnPoint.position;
        playerObj.transform.rotation = spawnPoint.rotation;
    }


    private void OnClientDisconnected(ulong clientId)
    {
        if (Players.ContainsKey(clientId))
        {
            Debug.Log($"Player disconnected: {Players[clientId].name}");
            Players.Remove(clientId);
        }
    }

    #region Team Logic

    public bool CanIncreaseDopples(int num)
    {
        return num < Players.Count;
    }

    public void SetNumberOfDopples(int num)
    {
        numberOfDopples = Mathf.Min(num, Players.Count - 1);
    }

    private void AssignTeams()
    {
        // First, reset all players' isDopple status
        foreach (var player in Players.Values)
        {
            player.GetComponent<PlayerController>().isDopple = false;
        }

        // Create a shuffled list of players
        var shuffledPlayers = Players.Values.OrderBy(p => Random.value).ToList();

        // Assign the first 'numberOfDopples' players as Dopples
        for (int i = 0; i < numberOfDopples && i < shuffledPlayers.Count; i++)
        {
            shuffledPlayers[i].GetComponent<PlayerController>().isDopple = true;
        }
    }


    #endregion

    #region Start of Game

    public void HostSelectsStart()
    {
        AssignTeams();
        SpawnInGame();
    }

    public void SpawnInLobby()
    {
        SpawnAtPoints(LobbySpawnPoints);
    }

    private void SpawnInGame()
    {
        SpawnAtPoints(GameSpawnPoints);
    }

    #endregion

    #region Helper Functions

    private void SpawnAtPoints(List<Transform> spawnPoints)
    {
        int i = 0;
        foreach (var player in Players.Values)
        {
            Transform targetPoint = spawnPoints[i % spawnPoints.Count];
            player.transform.position = targetPoint.position;
            player.transform.rotation = targetPoint.rotation;
            i++;
        }
    }

    public int GetNumberOfPlayers()
    {
        return Players.Count;
    }

    #endregion
}
