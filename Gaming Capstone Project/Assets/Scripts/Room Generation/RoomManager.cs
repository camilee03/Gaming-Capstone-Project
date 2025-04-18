using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }
    public Texture2D [] wallpaperTextures;
    public Texture2D [] wallDamageTextures;

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

    public void ChangeWalls()
    {
        foreach(Room room in rooms)
        {
            Color color = new Color(Random.Range(0.5f, 1), Random.Range(0.5f, 1), Random.Range(0.5f, 1));
            Texture2D wallPaper = wallpaperTextures[Mathf.RoundToInt(Random.Range(0, wallpaperTextures.Length))];
            Transform t = room.parent.transform;
            foreach(Renderer r in t.GetChild(0).GetComponentsInChildren<Renderer>())
            {
                int i = Mathf.RoundToInt(Random.Range(0, wallDamageTextures.Length));
                r.material.SetColor("_Color", color);
                r.material.SetTexture("_Wallpaper", wallPaper);
                r.material.SetTexture("_Damage", wallDamageTextures[i]);
            }
        }
    }
}
