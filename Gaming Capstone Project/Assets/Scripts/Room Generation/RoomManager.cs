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
    public List<GameObject> spawnPoints;

    public void InitializeSpawnPoints()
    {
        foreach (Room room in rooms)
        {
            Transform t = room.parent.transform;
            Vector3 sumVector = Vector3.zero;
            foreach(Transform child in t.GetChild(0)) //sum the walls
            {
                sumVector += child.position;
            }
            Vector3 centerVector = sumVector / t.GetChild(0).childCount;
            centerVector.y = 5;
            GameObject spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.position = centerVector;
            GameController.Instance.RegisterSpawnPoint(spawnPoint.transform);
            spawnPoints.Add(spawnPoint);
        }
    }
}
