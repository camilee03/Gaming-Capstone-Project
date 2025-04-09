using UnityEngine;
using System.Collections.Generic;

public class RoomTypeJSON : MonoBehaviour
{
    [SerializeField] TextAsset jsonFile;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Rooms rooms = JsonUtility.FromJson<Rooms>(jsonFile.text);

        foreach (ObjectStructure room in rooms.room)
        {
            Debug.Log(room);
        }
    }

    public struct Rooms
    {
        public List<ObjectStructure> room;
    }

    public struct ObjectStructure
    {
        public string name;
        public List<char> objects;
    }
}
