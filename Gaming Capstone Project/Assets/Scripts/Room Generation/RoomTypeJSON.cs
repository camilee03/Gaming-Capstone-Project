using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class RoomTypeJSON : MonoBehaviour
{
    [SerializeField] TextAsset jsonFile;
    public List<string[]> objectList = new List<string[]>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Rooms rooms = JsonUtility.FromJson<Rooms>(jsonFile.text);

        foreach (Room room in rooms.room)
        {
            objectList.Add(room.objects);
        }

    }

    [System.Serializable]
    public class Rooms
    {
        public Room[] room;
    }

    [System.Serializable]
    public class Room
    {
        public string name;
        public string[] objects;
    }
}
