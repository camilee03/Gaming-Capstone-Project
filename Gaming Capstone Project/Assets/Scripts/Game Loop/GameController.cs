using System.Collections;
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

    // Number of Doppleganger players to assign
    private int numberOfDopples = 1;

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
    private void TeleportPlayerToLobby(ulong clientId)
    {
        if (!Players.ContainsKey(clientId) || LobbySpawnPoint == null)
        {
            Debug.LogWarning($"[Server] Could not teleport Client {clientId} — Missing player or spawn point.");
            return;
        }

        GameObject playerObj = Players[clientId];

        var netTransform = playerObj.GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(
                LobbySpawnPoint.position,
                LobbySpawnPoint.rotation,
                Vector3.one
            );
        }
        else
        {
            playerObj.transform.SetPositionAndRotation(
                LobbySpawnPoint.position,
                LobbySpawnPoint.rotation
            );
        }

        Debug.Log($"[Server] Teleported {playerObj.name} to lobby.");
    }

    // -------------------------------------------------------
    // Client (Player) Connect / Disconnect
    // -------------------------------------------------------
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObject == null)
        {
            Debug.LogWarning($"[Server] PlayerObject for ClientId {clientId} is null.");
            return;
        }

        GameObject playerObj = playerObject.gameObject;
        Players[clientId] = playerObj;

        Debug.Log($"[Server] Player connected => ClientId {clientId}, {playerObj.name}");

        // Move the player using a ClientRpc
        var pc = playerObj.GetComponent<PlayerController>();
        if (pc != null && LobbySpawnPoint != null)
        {
            pc.MoveToLobbyClientRpc(LobbySpawnPoint.position, LobbySpawnPoint.rotation);
        }
        else
        {
            Debug.LogWarning("[Server] PlayerController or LobbySpawnPoint is null. Player will not be moved.");
        }
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
        AssignTeams();
    }

    // Teleports players back to the Lobby
    public void SpawnInLobby()
    {
        SpawnAtPoints(LobbySpawnPoint);
    }

    // Teleports players to the game spawn point
    private void SpawnInGame()
    {
        SpawnAtPoints(GameSpawnPoint);
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

    // -------------------------------------------------------
    // Helper Functions
    // -------------------------------------------------------
    private void SpawnAtPoints(Transform spawnPoint)
    {
        // Teleport every player in the dictionary
        foreach (var player in Players.Values)
        {
            Debug.Log($"Teleporting: {player.name} to {spawnPoint.name}");

            var netTransform = player.GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.Teleport(spawnPoint.position, spawnPoint.rotation, new Vector3(0.75f, 0.75f, 0.75f));
            }
            else
            {
                // fallback
                player.transform.position = spawnPoint.position;
                player.transform.rotation = spawnPoint.rotation;
            }
        }
    }

    public int GetNumberOfPlayers()
    {
        return Players.Count;
    }

    private void Update()
    {
        if (IsServer && Input.GetKeyDown(KeyCode.Return))
        {
            HostSelectsStart();
        }
    }

}