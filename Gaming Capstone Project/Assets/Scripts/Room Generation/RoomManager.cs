using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class RoomManager : NetworkBehaviour
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
            Vector3 color = new Vector3(Random.Range(0.5f, 1), Random.Range(0.5f, 1), Random.Range(0.5f, 1));
            int wallPaper = Mathf.RoundToInt(Random.Range(0, wallpaperTextures.Length));
            ulong tID = room.parent.GetComponent<NetworkObject>().NetworkObjectId;
            if (IsOwner)
            {
                if (IsServer) { setWallTexturesClientRpc(color, wallPaper, tID); }
                else { setWallTexturesServerRpc(color, wallPaper, tID); }
            }
        }
    }

    [ServerRpc]
    public void setWallTexturesServerRpc(Vector3 color, int wallpaperIndex, ulong tID)
    {
        setWallTexturesClientRpc(color, wallpaperIndex, tID);
    }
    [ClientRpc]
    public void setWallTexturesClientRpc(Vector3 color, int wallpaperIndex, ulong tID)
    {
        SetWallTextures(color, wallpaperIndex, tID);
    }
    void SetWallTextures(Vector3 color, int wallpaperIndex, ulong tID)
    {
        Color c = new Color(color.x, color.y, color.z);
        Texture2D wallPaper = wallpaperTextures[wallpaperIndex];
        NetworkObject netObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[tID];
        Transform t = netObj.transform;
        foreach (Renderer r in t.GetChild(0).GetComponentsInChildren<Renderer>())
        {
            int i = Mathf.RoundToInt(Random.Range(0, wallDamageTextures.Length));
            r.materials[0].SetColor("_Color", c);
            r.materials[0].SetTexture("_Wallpaper", wallPaper);
            r.materials[0].SetTexture("_Damage", wallDamageTextures[i]);
        }
    }
}
