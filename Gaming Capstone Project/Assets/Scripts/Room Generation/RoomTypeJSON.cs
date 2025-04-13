using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class RoomTypeJSON : MonoBehaviour
{
    [SerializeField] TextAsset jsonFile;
    public Rooms rooms;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rooms = JsonUtility.FromJson<Rooms>(jsonFile.text);
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
