using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Vivox;
using Unity.VisualScripting;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); }
        else { Instance = this; }
    }

    public List<Room> rooms;
    public List<Vector3> spawnPoints;

    public void InitializeSpawnPoints()
    {
        foreach (Room room in rooms)
        {
            spawnPoints.Add(room.parent.transform.position);
        }
    }
}
