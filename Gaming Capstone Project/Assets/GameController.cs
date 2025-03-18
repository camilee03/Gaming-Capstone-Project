using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    Dictionary<int, GameObject> Players;


    public Transform[] LobbySpawnPoints;
    public Transform[] GameSpawnPoints;

    private int numberOfDopples = 1;

    #region Team Logic

    public void SetNumberOfDopples(int num)
    {
        if(num > Players.Count-1)
        {
            num = Players.Count-1;
        }
        numberOfDopples = num;
    }
     private void AssignTeams()
    {
        //pick numberOfDopples amount of players randomly, set them to dopples.
        for (int i = 0; i < numberOfDopples; i++) {
            int indexToChange = Random.Range(0, Players.Count);
            if(Players[indexToChange].GetComponent<PlayerController>().isDopple )
            {
                i--;
                continue;
            }
            else
            {
                Players[indexToChange].GetComponent<PlayerController>().isDopple = true;   
            }

        }
    }

    #endregion

    #region Task Logic

    #endregion

    #region Voting







    #endregion

    #region Start of Game


    /// <summary>
    /// Spawn players in start area.
    /// 
    /// Have host (or anybody) start game
    /// 
    /// assign teams
    /// assn tasks
    /// 
    /// teleport to spawn points in room
    /// </summary>

    private void SpawnInLobby()
    {
        SpawnAtPoints(LobbySpawnPoints);
    }
    private void SpawnInGame()
    {
        SpawnAtPoints(LobbySpawnPoints);
    }


    public void HostSelectsStart()//called from button
    {


        AssignTeams();
        SpawnInGame();

    }


    #endregion


    #region Helper Functions


    private void SpawnAtPoints(Transform[] spawnPoints)
    {
        
        for (int i = 0; i < Players.Keys.Count; i++)
        {
            if (i > spawnPoints.Length)
            {
                if (Players.TryGetValue(i, out GameObject Overlap))
                {
                    Overlap.transform.position = spawnPoints[i - spawnPoints.Length].position;
                    Overlap.transform.rotation = spawnPoints[i - spawnPoints.Length].rotation;
                    continue;
                }
            }

           if(Players.TryGetValue(i,out GameObject temp))
            {
                temp.transform.position = spawnPoints[i].position;
                temp.transform.rotation = spawnPoints[i].rotation;

            }

        }
    }

    #endregion
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
