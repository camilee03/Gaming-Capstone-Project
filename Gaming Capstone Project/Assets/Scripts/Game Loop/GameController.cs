using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.Rendering.Universal;

public class GameController : NetworkBehaviour
{
    public static GameController Instance { get; private set; }
    Dictionary<int, Color> ColorLibrary = new Dictionary<int, Color>();

    public Dictionary<ulong, GameObject> Players = new Dictionary<ulong, GameObject>();
    public int numPlayers = 1;

    [Header("Spawn Points")]
    public Transform LobbySpawnPoint;
    public Transform GameSpawnPoint;
    public List<int> usedColors = new List<int>();
    public NetworkList<int> votesCasted = new NetworkList<int>();

    public NetworkVariable<int> TimeLeftInVoting = new NetworkVariable<int>();
    public float votingTime = 30f;

    public List<Transform> Spawnpoints = new List<Transform>();
    // Number of Doppleganger players to assign
    private int numberOfDopples = 1;
    public GameObject LobbyCanvas;
    [Header("Voting")]
    public float voteStartDelay = 120f; // Total time until voting starts
    public NetworkVariable<int> secondsRemainingUntilVote;      // Countdown shown publicly
    public Canvas VotingCanvas;
    public GameObject playerObj;
    public bool hostSelectedStart =false;

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
            //usedColors.OnListChanged += OnUsedColorsChanged;
        }

    }
    private System.Collections.IEnumerator WaitForLocalPlayer()
    {
        while (NetworkManager.Singleton.LocalClient == null ||
               NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            yield return null;
        }

        playerObj = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        initColors();

    }

    public override void OnNetworkDespawn()
    {
        //usedColors.OnListChanged -= OnUsedColorsChanged;

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
            if (pc != null && pc.ColorID >= 1)
            {
                UnlockColor(pc.ColorID);
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

    private void AssignTasks()
    {
        // Reset all to Scientist
        foreach (var kvp in Players)
        {
            var pc = kvp.Value.GetComponent<TaskAssigner>();
            pc.start = true;
        }
    }

    private void StartRoomGeneration()
    {
        GameObject.Find("RoomGenerationManager").GetComponent<RoomGeneration>().StartGeneration(numPlayers);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckForEndOfGameServerRpc()
    {
        Debug.Log("CheckForEndOfGameServerRpc");
        int deadDopples = 0, deadSci = 0;
        foreach(var kvp in Players)
        {
            if(kvp.Value.GetComponent<PlayerController>().isDead)
            {
                if(kvp.Value.GetComponent<PlayerController>().isDopple)
                {
                    deadDopples++;
                    Debug.Log("Plus 1 dead Dopple, now at " + deadDopples);
                }
                else
                {
                    deadSci++;
                    Debug.Log("Plus 1 dead scientsit, now at " + deadSci);

                }
            }
        }

        if(deadDopples >= numberOfDopples)
        {
            Debug.Log("Scientsits Win!");
            EndGameForScienceWinClientRpc();
            return;
        }
        if (numberOfDopples-deadDopples >= (Players.Count-numberOfDopples)-deadSci)
        {
            Debug.Log("Dopples Win!");
            EndGameForDoppleWinClientRpc();
            return;
        }
        Debug.Log("Nobody Won!");
        //Maybe change Task-based win/loss to into this script? idk
    }


    [ClientRpc]
    private void EndGameForScienceWinClientRpc()
    {
        Debug.Log("aaa");
        if (playerObj.GetComponentInChildren<PlayerDisplayFade>() == null) Debug.Log("DAmn");
        else Debug.Log("Shitt");

        playerObj.GetComponentInChildren<PlayerDisplayFade>().ScientistWin();
    }

    [ClientRpc]
    private void EndGameForDoppleWinClientRpc()
    {
        Debug.Log("aaa");
        if (playerObj.GetComponentInChildren<PlayerDisplayFade>() == null) Debug.Log("DAmn");
        else Debug.Log("Shitt");
        playerObj.GetComponentInChildren<PlayerDisplayFade>().DoppleWin();

    }



    // -------------------------------------------------------
    // Start of Game
    // -------------------------------------------------------
    // Called ONLY by the server/host to assign teams and spawn players in the game
    public void HostSelectsStart()
    {
        if (!IsServer) return; // only the server/host does team assignment

        Debug.Log("[Server] HostSelectsStart() => AssignTeams()");
        StartRoomGeneration();
        SpawnPlayersAtRandomPoints();
        AssignRandomColorsToUnpickedPlayers();
        AssignTeams();
        AssignTasks();
        AssignAllColorsAndNames();
        DisableLobbyCanvasClientRpc();
        StartVoteInitTimerServerRpc();
    }
    public void AssignAllColorsAndNames()
    {
        foreach (NetworkClient c in NetworkManager.Singleton.ConnectedClientsList)
        {
            Debug.Log("Client Found.");
            PlayerController pc = c.PlayerObject.GetComponent<PlayerController>();
            pc.ExternalSetColor();
            pc.ExternalSetName();
            pc.ExternalEnableGravity();
        }
    }

    [ServerRpc]
    private void StartVoteInitTimerServerRpc()
    {
        votesCasted.Clear();
        secondsRemainingUntilVote.Value = Mathf.CeilToInt(voteStartDelay);
        StartCoroutine(StartVoteCountdown());
    }

    private IEnumerator StartVoteCountdown()
    {
        while (secondsRemainingUntilVote.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            secondsRemainingUntilVote.Value--;
        }
        StartVote();
        StartVoteServerRpc();
    }
    [ServerRpc]
    private void StartVoteServerRpc()
    {
        Debug.Log("[ClientRpc] Vote started!");

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        localPlayer.StartVoteClientRpc();

    }
    public void StartVote()
    {
        playerVotes.Clear();
        votingInProgress = true;

        StartVoteServerRpc();
        TimeLeftInVoting.Value = Mathf.CeilToInt(votingTime);

        // End vote in 10 seconds
        if(IsServer) StartCoroutine(EndVoteAfterDelay());
    }

    private IEnumerator EndVoteAfterDelay()
    {
        while (TimeLeftInVoting.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            TimeLeftInVoting.Value--;
        }
        votingInProgress = false;
        VotingComplete();
    }


    
    private void VotingComplete()
    {
        Debug.Log("Voting competed");
        votingInProgress = false;

            if (playerVotes.Count == 0)
            {
                Debug.Log("No votes were cast.");
            secondsRemainingUntilVote.Value = Mathf.CeilToInt(voteStartDelay);
            StartVoteInitTimerServerRpc();

            Debug.Log("Voting competed3");

            playerObj.GetComponent<PlayerController>().EndVoteClientRpc();
            return;
            }
        Debug.Log("Voting competed LIST");

        var groupedVotes = playerVotes
                .GroupBy(kv => kv.Value) // group by colorIndex
                .Select(g => new { Color = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();
        Debug.Log("Voting competed LIST");

        int highestCount = groupedVotes.First().Count;
            var topColors = groupedVotes.Where(g => g.Count == highestCount).ToList();

            if (topColors.Count > 1)
            {
                Debug.Log("Vote tied. No one is eliminated.");
                return;
            }

            int winningColor = topColors.First().Color;
            Debug.Log($"Color {winningColor} wins the vote!");
            Debug.Log("Voting competed 1");

            // Eliminate the player with that color
            foreach (var kvp in Players)
            {
                var pc = kvp.Value.GetComponent<PlayerController>();
                usedColors.Remove(winningColor);
                if (pc.ColorID == winningColor)
                {
                    pc.KillClientRpc();
                    break;
                }
            }
            Debug.Log("Voting competed2");

            secondsRemainingUntilVote.Value = Mathf.CeilToInt(voteStartDelay);
            StartVoteInitTimerServerRpc();
        
        Debug.Log("Voting competed3");
        playerObj.GetComponent<PlayerController>().EndVoteClientRpc();
        CheckForEndOfGameServerRpc();

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
        //Debug.Log($"Checking color availability for {colorIndex}");

        return !usedColors.Contains(colorIndex);
    }
    public bool IsColorAvailableByColor(Color color)
    {

        return !usedColors.Contains(GetKeyByValue(ColorLibrary, color));
    }

    public TKey GetKeyByValue<TKey, TValue>(Dictionary<TKey, TValue> dict, TValue value)
    {
        foreach (var pair in dict)
        {
            if (EqualityComparer<TValue>.Default.Equals(pair.Value, value))
            {
                return pair.Key;
            }
        }
        return default; // or throw exception if not found
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
        List<int> availableColors = Enumerable.Range(0, 11)
            .Where(c => IsColorAvailable(c))
            .ToList();

        foreach (var kvp in Players)
        {
            var player = kvp.Value.GetComponent<PlayerController>();
            if (player.ColorID == -1)
            {
                Debug.Log("Color Not Added yet.");
                if (availableColors.Count == 0)
                {
                    Debug.LogWarning("No colors left to assign!");
                    return;
                }

                player.ExternalSetColor(availableColors[0]);
                availableColors.RemoveAt(0);
            }
            player.ApplyColor();
        }
    }

    public void RegisterSpawnPoint(Transform t)
    {
        t.position = new Vector3(t.position.x, 15 ,t.position.z);
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
        hostSelectedStart = true;


        Debug.Log($"[ClientRpc] Player {OwnerClientId} => Successfully deleted canvas");

    }
    #endregion

    public void Start()
    {
        StartCoroutine(WaitForLocalPlayer());

    }

    #region ColorVariables
    private void initColors()
    {
        ColorLibrary.Add(0, Color.HSVToRGB(0 / 360f, 1, 1)); //red
        ColorLibrary.Add(1, Color.HSVToRGB(25 / 360f, 1, 1));//orange
        ColorLibrary.Add(2, Color.HSVToRGB(50 / 360f, 1, 1));//yellow
        ColorLibrary.Add(3, Color.HSVToRGB(110 / 360f, 1, 1));//green
        ColorLibrary.Add(4, Color.HSVToRGB(180 / 360f, 1, 1));//teal
        ColorLibrary.Add(5, Color.HSVToRGB(210 / 360f, 1, 1));//blue
        ColorLibrary.Add(6, Color.HSVToRGB(280 / 360f, 1, 1));//purple
        ColorLibrary.Add(7, Color.HSVToRGB(310 / 360f, 1, 1));//pink
        ColorLibrary.Add(8, Color.HSVToRGB(0, 0, 1));//white
        ColorLibrary.Add(9, Color.HSVToRGB(0, 0, 0.5f));//gray
        ColorLibrary.Add(10, Color.HSVToRGB(0, 0, 0.1f));//black
        ColorLibrary.Add(11, Color.HSVToRGB(30 / 360f, 0.9f, 4f));//brown
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