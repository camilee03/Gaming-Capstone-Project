using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public class GameController : NetworkBehaviour
{
    public static GameController Instance { get; private set; }

    public Dictionary<ulong, GameObject> Players = new Dictionary<ulong, GameObject>();
    public int numPlayers = 1;

    [Header("Spawn Points")]
    public Transform LobbySpawnPoint;
    public Transform GameSpawnPoint;
    public NetworkList<int> usedColors = new NetworkList<int>();
    public NetworkList<int> votesCasted = new NetworkList<int>();


    public int TimeLeftInVoting = 0;
    public float votingTime = 60f;

    public List<Transform> Spawnpoints = new List<Transform>();
    // Number of Doppleganger players to assign
    private int numberOfDopples = 1;
    public GameObject LobbyCanvas;
    [Header("Voting")]
    private float voteStartDelay = 120f; // Total time until voting starts
    public int secondsRemainingUntilVote;      // Countdown shown publicly
    public Canvas VotingCanvas;
    public GameObject playerObj;


    private Dictionary<ulong, int> playerVotes = new Dictionary<ulong, int>(); // clientId -> colorIndex
    private bool votingInProgress = false;

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
        if (IsClient)
        {
            usedColors.OnListChanged += OnUsedColorsChanged;
        }
        playerObj = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

    }

    public override void OnNetworkDespawn()
    {
        usedColors.OnListChanged -= OnUsedColorsChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

    }
    #endregion

    private void OnUsedColorsChanged(NetworkListEvent<int> change)
    {
        // Refresh UI if needed
        var uiManager = FindFirstObjectByType<ColorSelectionUIManager>();
        if (uiManager != null)
            uiManager.RefreshAll();
    }

    // -------------------------------------------------------
    // Client (Player) Connect / Disconnect
    // -------------------------------------------------------
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        // Grab the spawned PlayerObject (the default from Netcode)
        GameObject newplayerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
        Players[clientId] = newplayerObj;

        Debug.Log($"[Server] Player connected => ClientId {clientId}, {newplayerObj.name}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (Players.ContainsKey(clientId))
        {
            Debug.Log($"[Server] Player disconnected => ClientId {clientId}, {Players[clientId].name}");
            Players.Remove(clientId);
        }
        if (Players.TryGetValue(clientId, out var player))
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc != null && pc.ColorID.Value >= 1)
            {
                UnlockColor(pc.ColorID.Value);
            }
        }

        Players.Remove(clientId);
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
        AssignRandomColorsToUnpickedPlayers();
        AssignTeams();
        DisableLobbyCanvasClientRpc();
        StartVoteInitTimerClientRpc();
    }
    [ClientRpc]
    private void StartVoteInitTimerClientRpc()
    {
        votesCasted.Clear();
        secondsRemainingUntilVote = Mathf.CeilToInt(voteStartDelay);
        StartCoroutine(StartVoteCountdown());
    }

    private IEnumerator StartVoteCountdown()
    {
        while (secondsRemainingUntilVote > 0)
        {
            yield return new WaitForSeconds(1f);
            secondsRemainingUntilVote--;
        }
        StartVote();
        StartVoteClientRpc();
    }
    [ClientRpc]
    private void StartVoteClientRpc()
    {
        Debug.Log("[ClientRpc] Vote started!");

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        localPlayer.StartVote();

    }
    public void StartVote()
    {
        playerVotes.Clear();
        votingInProgress = true;

        StartVoteClientRpc();
        TimeLeftInVoting = Mathf.CeilToInt(votingTime);

        // End vote in 10 seconds
        StartCoroutine(EndVoteAfterDelay());
    }

    private IEnumerator EndVoteAfterDelay()
    {
        while (TimeLeftInVoting > 0)
        {
            yield return new WaitForSeconds(1f);
            TimeLeftInVoting--;
        }
        votingInProgress = false;
        VotingComplete();
    }



    private void VotingComplete()
    {
        votingInProgress = false;

        if (playerVotes.Count == 0)
        {
            Debug.Log("No votes were cast.");
            return;
        }

        var groupedVotes = playerVotes
            .GroupBy(kv => kv.Value) // group by colorIndex
            .Select(g => new { Color = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        int highestCount = groupedVotes.First().Count;
        var topColors = groupedVotes.Where(g => g.Count == highestCount).ToList();

        if (topColors.Count > 1)
        {
            Debug.Log("Vote tied. No one is eliminated.");
            return;
        }

        int winningColor = topColors.First().Color;
        Debug.Log($"Color {winningColor} wins the vote!");

        // Eliminate the player with that color
        foreach (var kvp in Players)
        {
            var pc = kvp.Value.GetComponent<PlayerController>();
            usedColors.Remove(winningColor);
            if (pc.ColorID.Value == winningColor)
            {
                pc.KillClientRpc();
                break;
            }
        }
        playerObj.GetComponent<PlayerController>().EndVote();
        secondsRemainingUntilVote = Mathf.CeilToInt(voteStartDelay);
        StartVoteInitTimerClientRpc();
    }




    public void ReceiveVote(ulong clientId, int colorIndex)
    {
        if (!votingInProgress) return;

        if (playerVotes.ContainsKey(clientId))
        {
            Debug.Log($"[Server] Player {clientId} changed vote to color {colorIndex}");
            playerVotes[clientId] = colorIndex;
        }
        else
        {
            Debug.Log($"[Server] Player {clientId} voted for color {colorIndex}");
            playerVotes.Add(clientId, colorIndex);
        }
    }




    public bool IsColorAvailable(int colorIndex)
    {
        Debug.Log($"Checking color availability for {colorIndex}");

        return !usedColors.Contains(colorIndex);
    }

    public void LockColor(int colorIndex)
    {
        if (!usedColors.Contains(colorIndex))
        {
            usedColors.Add(colorIndex);
        }
    }


    public void UnlockColor(int colorIndex)
    {
        usedColors.Remove(colorIndex);
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

    private void AssignRandomColorsToUnpickedPlayers()
    {
        List<int> availableColors = Enumerable.Range(1, 12)
            .Where(c => IsColorAvailable(c))
            .ToList();

        foreach (var kvp in Players)
        {
            var player = kvp.Value.GetComponent<PlayerController>();
            if (player.ColorID.Value < 1)
            {
                if (availableColors.Count == 0)
                {
                    Debug.LogWarning("No colors left to assign!");
                    return;
                }

                int chosen = availableColors[Random.Range(0, availableColors.Count)];
                player.ForceSetColorServerRpc(chosen); // <--- Let the RPC handle LockColor()
                availableColors.Remove(chosen);
            }
        }
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

    public void Start()
    {
        initColors();
    }

    #region ColorVariables
    Dictionary<int, Color> ColorLibrary = new Dictionary<int, Color>();
    private void initColors()
    {
        ColorLibrary.Add(1, Color.HSVToRGB(0 / 360f, 1, 1)); //red
        ColorLibrary.Add(2, Color.HSVToRGB(25 / 360f, 1, 1));//orange
        ColorLibrary.Add(3, Color.HSVToRGB(50 / 360f, 1, 1));//yellow
        ColorLibrary.Add(4, Color.HSVToRGB(110 / 360f, 1, 1));//green
        ColorLibrary.Add(5, Color.HSVToRGB(180 / 360f, 1, 1));//teal
        ColorLibrary.Add(6, Color.HSVToRGB(210 / 360f, 1, 1));//blue
        ColorLibrary.Add(7, Color.HSVToRGB(280 / 360f, 1, 1));//purple
        ColorLibrary.Add(8, Color.HSVToRGB(310 / 360f, 1, 1));//pink
        ColorLibrary.Add(9, Color.HSVToRGB(0, 0, 1));//white
        ColorLibrary.Add(10, Color.HSVToRGB(0, 0, 0.5f));//gray
        ColorLibrary.Add(11, Color.HSVToRGB(0, 0, 0.1f));//black
        ColorLibrary.Add(12, Color.HSVToRGB(30 / 360f, 0.9f, 4f));//brown
    }

    /// <summary>
    /// ColorIndex ranges from 1 - 12;
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Color getColorByIndex(int index)
    {
        return ColorLibrary[index];
    }
    #endregion
}