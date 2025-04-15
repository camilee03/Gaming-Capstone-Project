using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class RoomGeneration : NetworkBehaviour
{
    // Syntax:
    // '' = undefined
    // d = defined
    // w = wall
    // f = floor

    [Header("Debug Values")]
    [SerializeField] int numPlayers = 1;
    public bool debug;


    [Header("Spawn Data")]
    public float scale = 10; // how many tiles apart are different objects
    public int seed = -1; // set to -1 if no seed wanted
    public int numRooms = 4;
    [SerializeField] GameObject tiles;
    [SerializeField] GameObject walls;
    [SerializeField] GameObject doors;
    [SerializeField] GameObject roomObject;
    [SerializeField] GameObject roomParentObject;
    List<(GameObject door, int pos, GameObject room)> doorAndRoom;
    ObjectGeneration objectGen;
    TaskManager taskManager;


    [Header("Collision Data")]
    bool collided;
    bool coroutineRunning;

    private void Start()
    {
        if (IsServer)
        {
            objectGen = GetComponent<ObjectGeneration>();
            taskManager = GetComponent<TaskManager>();
            StartGeneration();
        }
        else
        {
            Debug.Log("Not generated");
        }
    }


    // -- ROOM GENERATION -- //

    public void StartGeneration()
    {
        if (debug) { Random.InitState(DebugGen.Instance.seed); }
        else { if (seed != -1) { Random.InitState(seed); } }

        GenerateMultipleRooms();
        RoomManager.Instance.InitializeSpawnPoints();
        taskManager.CreateTasks();
    }

    /// <summary> Use the room procedure to create multiple rooms </summary>
    void GenerateMultipleRooms()
    {
        ClearMap(); // only neccessary if regenerating the entire map

        // Spawn first room
        Room newRoom = new(scale, tiles, walls, roomObject, roomParentObject);
        newRoom.RoomProcedure(0, objectGen);
        RoomManager.Instance.rooms.Add(newRoom);
        GameObject room1 = newRoom.parent;

        // Find and spawn lobby
        //GameObject room1 = GameObject.Find("Room0");


        int index = 1;

        while (numRooms > 0)
        {
            // choose a random room from room1 to edit
            GameObject chosenRoom = RoomFunctions.GetRootChild(room1, "WallParent", 0, true)[0];
            //bool isLobby = chosenRoom.name == "Room0";
            bool isLobby = false;

            // Spawn next room
            Room room2 = new(scale, tiles, walls, roomObject, roomParentObject);
            room2.RoomProcedure(index, objectGen);
            RoomManager.Instance.rooms.Add(room2);
            numRooms--;

            // Get new doors
            doorAndRoom = new();
            ReplaceWallWithDoor(chosenRoom.transform.GetChild(0).gameObject, room1, isLobby);
            ReplaceWallWithDoor(room2.wallParent, room2.parent, false);

            // Link two rooms together
            room1 = RotateRooms();

            if (room1 == null) { Debug.Log("ERROR: Rooms cannot connect"); }
            index++;
        }


        // Spawn Objects for each room
        foreach (Room room in RoomManager.Instance.rooms)
        {
            objectGen.GenerationProcedure(room);
        }

    }


    /// <summary> Deletes any stray objects </summary>
    void ClearMap()
    {
        GameObject[] spawnRooms = GameObject.FindGameObjectsWithTag("Room");

        foreach (GameObject room in spawnRooms)
        {
            if (room != null && room.name != "Room0") { GameObject.Destroy(room); }
        }

        RoomManager.Instance.rooms = new List<Room>();
    }
   



    // -- DOOR GENERATION -- //

    /// <summary> Chooses random walls to replace with a door </summary>
    void ReplaceWallWithDoor(GameObject wallParent, GameObject room, bool isLobby)
    {
        bool findWall = true;
        int index = 0;

        // Go one step deeper if lobby
        if (isLobby)
        {
            index = Random.Range(0, wallParent.transform.childCount - 1); // find random wall edge
            wallParent = wallParent.transform.GetChild(index).gameObject;
        }


        findWall = true;
        while (findWall)
        {
            index = Random.Range(0, wallParent.transform.childCount - 1); // find random wall
            findWall = wallParent.transform.GetChild(index).gameObject.name[0] != 'W'; // while isn't a wall
        }

        // set door rotation as same as wall
        Transform wallTransform = wallParent.transform.GetChild(index);
        Vector3 wallPosition = wallTransform.position;
        Quaternion wallRotation = wallTransform.rotation;

        // find direction based on name
        string name = wallParent.transform.GetChild(index).gameObject.name;
        char lastChar = name[name.Length - 1];
        int direction = (int)char.GetNumericValue(lastChar);

        // replace wall with door
        Destroy(wallParent.transform.GetChild(index).gameObject);
        GameObject newDoor = SpawnNetworkedObject(wallParent.transform, doors, wallPosition, wallRotation);
        newDoor.name = "Door" + direction;

        doorAndRoom.Add((newDoor, direction, room));
    }

    /// <summary> Take spawned rooms and doors, connect together, and return the list of paired rooms </summary>
    GameObject RotateRooms()
    {
        (GameObject door, int pos, GameObject room) dar1 = doorAndRoom[0]; // door1
        dar1.pos = FindDoorDirection(doorAndRoom[0].door, doorAndRoom[0].room);
        (GameObject door, int pos, GameObject room) dar2 = doorAndRoom[1]; // door2
        dar2.pos = FindDoorDirection(doorAndRoom[1].door, doorAndRoom[1].room);

        // try to pair doors
        GameObject newRoomParent = PairDoors(dar1, dar2);

        if (newRoomParent != null) { return newRoomParent; }
        else
        {
            // rerotate dar to attempt to fit
            Debug.Log("Rotating room");

            if (dar1.pos > 1) { dar1.pos -= 2; }
            else { dar1.pos += 2; }
            dar1.room.transform.Rotate(new Vector3(0, 180, 0));

            // try to pair doors again
            newRoomParent = PairDoors(dar1, dar2);
            if (newRoomParent != null)
            {
                return newRoomParent;
            }
            else { Debug.Log("ERROR: no combination found"); return null; }

        }
    }

    int FindDoorDirection(GameObject door, GameObject room)
    {
        if (door.transform.rotation.eulerAngles.y % 180 == 90)
        {
            // Direction 0 (left)
            if (door.transform.position.x < room.transform.position.x) { return 0; }

            // Direction 2 (right)
            return 2;
        }
        else
        {
            // Direction 1 (below)
            if (door.transform.position.z < room.transform.position.z) { return 1; }

            // Direction 3 (above)
            return 3;
        }
    }


    /// <summary> Assigns old rooms and hallway to new room and returns new room </summary>
    GameObject PairDoors((GameObject door, int pos, GameObject room) dar1, (GameObject door, int pos, GameObject room) dar2)
    {
        float roomDistance = scale;
        GameObject newRoomParent = null;

        if (dar1.pos != dar2.pos) // if the doors face different directions
        {
            // Create a hallway
            GameObject newHallway = LinkDoors(dar1, dar2, roomDistance);
            if (newHallway == null) { Debug.Log("Couldn't find a path"); return null; }

            // change room parent to new gameobject
            newRoomParent = SpawnNetworkedObject(null, roomParentObject, Vector3.zero, Quaternion.identity);
            newRoomParent.name = "Room" + dar1.room.name.Remove(0, 4) + dar2.room.name.Remove(0, 4);
            newRoomParent.tag = "Room";

            dar1.room.transform.GetComponent<NetworkObject>().TrySetParent(newRoomParent.transform);
            dar2.room.transform.GetComponent<NetworkObject>().TrySetParent(newRoomParent.transform);
            newHallway.transform.parent = newRoomParent.transform;
        }

        return newRoomParent;
    }


    /// <summary> Take two doors and tie them together </summary>
    GameObject LinkDoors((GameObject door, int pos, GameObject room) dar1, (GameObject door, int pos, GameObject room) dar2, float roomDistance)
    {
        Vector3[] directions = { Vector3.left, Vector3.back, Vector3.right, Vector3.forward };

        // Find tile next to door
        Vector3 tile1 = dar1.door.transform.position + dar1.room.transform.TransformDirection(directions[dar1.pos]) * (scale / 2);
        Vector3 tile2 = dar2.door.transform.position + dar2.room.transform.TransformDirection(directions[dar2.pos]) * (scale / 2);
        Vector3 doorDisplacement = tile1 - tile2; // start at same place


        // move room in certain direction
        Vector3 directionDisplacement = dar1.room.transform.TransformDirection(directions[dar1.pos]) * roomDistance;
        if (dar1.pos + 2 != dar2.pos && dar1.pos - 2 != dar2.pos)
        {
            directionDisplacement -= dar2.room.transform.TransformDirection(directions[dar2.pos]) * roomDistance;
        }

        // move room if too close
        int debugInt = 0;
        collided = true;
        doorDisplacement += directionDisplacement;

        while (collided && debugInt < 100)
        {
            // Move room
            //Debug.Log($"Moved {dar2.room.name} by {doorDisplacement}");
            dar2.room.transform.Translate(doorDisplacement);
            doorDisplacement = directionDisplacement;

            // Check for collisions
            collided = RoomFunctions.CheckForCollisions(dar2.room, scale);

            debugInt++;
        }

        // Debug.Log("Dir: " + dar1.pos + " Pos: " + dar1.door.transform.position);
        // Debug.Log("Dir: " + dar2.pos + " Pos: " + dar2.door.transform.position);

        // recheck tile positions
        tile1 = dar1.door.transform.position + dar1.room.transform.TransformDirection(directions[dar1.pos]) * (scale / 2);
        tile2 = dar2.door.transform.position + dar1.room.transform.TransformDirection(directions[dar2.pos]) * (scale / 2);

        // Create a path in between doors
        List<Vector3> hallwayPath = GeneralFunctions.FindShortestAvoidingTiles(tile1, tile2, scale);

        if (hallwayPath != null) { hallwayPath.Insert(0, tile1); hallwayPath.Insert(hallwayPath.Count - 1, tile2); }
        else { Debug.Log("A* couldn't find a path"); if (debug) { DebugGen.Instance.seed++; SceneManager.LoadScene(0); return null; } }

        // Spawn hallway
        GameObject hallwayParent = SpawnNetworkedObject(null, roomParentObject, Vector3.zero, Quaternion.identity);
        hallwayParent.name = "Hallway";

        Vector3 prevPos = dar1.door.transform.position;
        for (int i = 0; i < hallwayPath.Count - 1; i++)
        {
            SpawnNetworkedObject(hallwayParent.transform, tiles, hallwayPath[i], Quaternion.identity);
            DrawWallsAroundDoors(prevPos, hallwayPath[i], hallwayPath[i + 1], hallwayParent, new Vector3[] { dar1.door.transform.position, dar2.door.transform.position});
            prevPos = hallwayPath[i];
        }

        return hallwayParent;
    } 
    
    /// <summary> Outlines a hallway with walls </summary>
    void DrawWallsAroundDoors(Vector3 prevPos, Vector3 pos, Vector3 nextPos, GameObject parent, Vector3[] doors)
    {
        List<Vector3> positions = new List<Vector3>{pos+Vector3.right*scale, pos+Vector3.forward*scale, 
            pos+Vector3.left*scale,  pos+Vector3.back*scale};

        Vector3 spawnLeft = new Vector3(pos.x + scale / 2, 2.5f, pos.z);
        Vector3 spawnRight = new Vector3(pos.x - scale / 2, 2.5f, pos.z);
        Vector3 spawnAbove = new Vector3(pos.x, 2.5f, pos.z + scale / 2);
        Vector3 spawnBelow = new Vector3(pos.x, 2.5f, pos.z - scale / 2);
        Vector3[] spawnPos = new Vector3[4] { spawnLeft, spawnAbove, spawnRight, spawnBelow };

        for (int i=0; i<positions.Count; i++)
        {
            if (!doors.Contains(spawnPos[i]) && positions[i] != prevPos && positions[i] != nextPos)
            {
                GameObject newObject = SpawnNetworkedObject(parent.transform, walls, Vector3.zero, Quaternion.identity);
                newObject.transform.position = spawnPos[i];
                newObject.transform.localRotation = Quaternion.Euler(-90, ((i + 1) % 2) * 90, 0);
            }
        }
    }

    /// <summary> Spawns a network object </summary>
    
    GameObject SpawnNetworkedObject(Transform parent, GameObject child, Vector3 position, Quaternion rotation)
    {
        GameObject instance = null;

        instance = Instantiate(child, position, rotation);
        NetworkObject instanceNetworkObject = instance.GetComponent<NetworkObject>();
        if (instanceNetworkObject == null) { Debug.LogError(child.name + " needs a NetworkObject"); }
        instanceNetworkObject.Spawn(true);

        if (parent != null) { instanceNetworkObject.TrySetParent(parent); }

        return instance;
    }
}
