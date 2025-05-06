using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class RoomTypeJSON : MonoBehaviour
{
    [SerializeField] TextAsset jsonFile;
    public MapRooms rooms;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rooms = JsonUtility.FromJson<MapRooms>(jsonFile.text);
    }

    [System.Serializable]
    public class MapRooms
    {
        public List<MapRoom> room;
    }

    [System.Serializable]
    public class MapRoom
    {
        public string name;
        public string[] objects;
    }
}
