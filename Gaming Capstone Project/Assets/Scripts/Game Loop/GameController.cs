using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class GameController : NetworkBehaviour
{
    public static GameController Instance { get; private set; }

    public Dictionary<ulong, GameObject> Players = new Dictionary<ulong, GameObject>();
    public int numPlayers = 1;

    [Header("Spawn Points")]
    public Transform LobbySpawnPoint;
    public Transform GameSpawnPoint;


    public List<Transform> Spawnpoints = new List<Transform>();
    // Number of Doppleganger players to assign
    private int numberOfDopples = 1;
    public GameObject LobbyCanvas;
    // -------------------------------------------------------
    // Initialization / Singleton
    // -------------------------------------------------------
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

    #region Listeners
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
    #endregion

    // -------------------------------------------------------
    // Client (Player) Connect / Disconnect
    // -------------------------------------------------------
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        // Grab the spawned PlayerObject (the default from Netcode)
        GameObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
        Players[clientId] = playerObj;

        Debug.Log($"[Server] Player connected => ClientId {clientId}, {playerObj.name}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (Players.ContainsKey(clientId))
        {
            Debug.Log($"[Server] Player disconnected => ClientId {clientId}, {Players[clientId].name}");
            Players.Remove(clientId);
        }
    }

    // -------------------------------------------------------
    // Team Logic
    // -------------------------------------------------------
    public bool CanIncreaseDopples(int num)
    {
        return num < Players.Count;
    }

    public void SetNumberOfDopples(int newCount)
    {
        if (!IsServer) return;
        // At most #players -1 can be dopples, or do your own logic
        numberOfDopples = Mathf.Min(newCount, Players.Count - 1);
    }

    public int GetNumberOfDopples()
    {
        return numberOfDopples;
    }

    private void AssignTeams()
    {
        // Reset all to Scientist
        foreach (var kvp in Players)
        {
            var pc = kvp.Value.GetComponent<PlayerController>();
            pc.SetDoppleClientRpc(false);
        }

        // Shuffle the players
        var shuffledPlayers = Players.Values.OrderBy(p => Random.value).ToList();

        // Assign the first 'numberOfDopples' as Dopples
        for (int i = 0; i < numberOfDopples && i < shuffledPlayers.Count; i++)
        {
            var pc = shuffledPlayers[i].GetComponent<PlayerController>();
            pc.SetDoppleClientRpc(true);
        }
    }


    // -------------------------------------------------------
    // Start of Game
    // -------------------------------------------------------
    // Called ONLY by the server/host to assign teams and spawn players in the game
    public void HostSelectsStart()
    {
        if (!IsServer) return; // only the server/host does team assignment

        Debug.Log("[Server] HostSelectsStart() => AssignTeams()");
        SpawnPlayersAtRandomPoints();
        AssignTeams();
        DisableLobbyCanvasClientRpc();

    }





    // Example: a single player respawn method
    public void RespawnInLobby(GameObject player)
    {
        var netTransform = player.GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(
                LobbySpawnPoint.position,
                LobbySpawnPoint.rotation,
                new Vector3(0.75f, 0.75f, 0.75f) // example scale
            );
        }
        else
        {
            player.transform.position = LobbySpawnPoint.position;
            player.transform.rotation = LobbySpawnPoint.rotation;
        }
    }

    private void SpawnPlayersAtRandomPoints()
    {
        if (Spawnpoints.Count == 0)
        {
            Debug.LogWarning("No spawn points available.");
            return;
        }

        var shuffledSpawnpoints = Spawnpoints.OrderBy(x => Random.value).ToList();
        int i = 0;

        foreach (var kvp in Players)
        {
            GameObject player = kvp.Value;
            Transform spawn = shuffledSpawnpoints[i % shuffledSpawnpoints.Count];
            i++;

            var pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TeleportClientRpc(spawn.position, spawn.rotation);
            }

            Debug.Log($"[Server] Assigned {player.name} to spawn at {spawn.position}");
        }
    }



    public int GetNumberOfPlayers()
    {
        return Players.Count;
    }

    private void Update()
    {

    }


    public void RegisterSpawnPoint(Transform t)
    {
        if (!Spawnpoints.Contains(t))
            Spawnpoints.Add(t);
    }
    #region Lobby

    public void setLobby(GameObject obj)
    {
        LobbyCanvas = obj;

    }

    [ClientRpc]
    public void DisableLobbyCanvasClientRpc()
    {
            LobbyCanvas.SetActive(false);
        
        
        Debug.Log($"[ClientRpc] Player {OwnerClientId} => Successfully deleted canvas");

    }
    #endregion

}