using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public class RoomGeneration : NetworkBehaviour
{
    // Syntax:
    // '' = undefined
    // d = defined
    // w = wall
    // f = floor

    [Header("Debug Values")]
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
    public MapCam mcam;


    [Header("Collision Data")]
    bool collided;
    bool coroutineRunning;

    private void Start()
    {
        if (IsServer)
        {
            objectGen = GetComponent<ObjectGeneration>();
            taskManager = GetComponent<TaskManager>();
        }
    }


    // -- ROOM GENERATION -- //

    public void StartGeneration(int numPlayers)
    {
        numRooms = numPlayers * 2;
        if (seed != -1) { Random.InitState(seed); }

        GenerateMultipleRooms();
        RoomManager.Instance.InitializeSpawnPoints();
        taskManager.CreateTasks();
        mcam.Setup();
        RoomManager.Instance.ChangeWalls();
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

        int index = 1;

        while (numRooms > 0)
        {
            // choose a random room from room1 to edit
            GameObject chosenRoom = RoomFunctions.GetRootChild(room1, "WallParent", 0, true)[0];

            // Spawn next room
            Room room2 = new(scale, tiles, walls, roomObject, roomParentObject);
            room2.RoomProcedure(index, objectGen);
            RoomManager.Instance.rooms.Add(room2);
            numRooms--;

            // Get new doors
            doorAndRoom = new();
            ReplaceWallWithDoor(chosenRoom.transform.GetChild(0).gameObject, room1);
            ReplaceWallWithDoor(room2.wallParent, room2.parent);

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
    void ReplaceWallWithDoor(GameObject wallParent, GameObject room)
    {
        bool findWall = true;
        int index = 0;

        findWall = true;
        while (findWall)
        {
            index = Random.Range(0, wallParent.transform.childCount - 1); // find random wall
            findWall = wallParent.transform.GetChild(index).gameObject.name[0] != 'W'; // while isn't a wall
        }

        // set door positon as same as wall
        Transform wallTransform = wallParent.transform.GetChild(index);
        Vector3 wallPosition = wallTransform.position;

        // set door rotation as same as wall
        Vector3 outwardDir = -wallTransform.right; // Wall's right vector
        Quaternion wallRotation = Quaternion.LookRotation(outwardDir, Vector3.up) * Quaternion.Euler(-90, 0, 90);

        // find direction based on name
        string name = wallParent.transform.GetChild(index).gameObject.name;
        char lastChar = name[name.Length - 1];
        int direction = System.Convert.ToInt16(lastChar);

        // replace wall with door
        wallParent.transform.GetChild(index).GetComponent<NetworkObject>().Despawn(true);
        GameObject newDoor = SpawnNetworkedObject(wallParent.transform, doors, wallPosition, wallRotation);
        newDoor.name = "Door" + direction;

        doorAndRoom.Add((newDoor, direction, room));
    }

    /// <summary> Take spawned rooms and doors, connect together, and return the list of paired rooms </summary>
    GameObject RotateRooms()
    {
        (GameObject door, int pos, GameObject room) dar1 = doorAndRoom[0]; // door1
        (GameObject door, int pos, GameObject room) dar2 = doorAndRoom[1]; // door2

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
        // Get a normalized vector from tile2 to tile1.
        Vector3 tile1 = ComputeDoorTile(dar1.door);
        Vector3 tile2 = ComputeDoorTile(dar2.door);
        Vector3 displacement = tile1 - tile2;

        dar2.room.transform.Translate(displacement);

        // Determine each door’s direction
        Vector3 meetingDir1 = -dar1.door.transform.up.normalized;
        Vector3 meetingDir2 = -dar2.door.transform.up.normalized;

        // Compute ideal hallway direction
        Vector3 idealHallwayDir = meetingDir1 - meetingDir2;

        // See if rooms collide with each other
        int debugInt = 0;
        bool collided = true;

        while (collided && debugInt < 100)
        {
            // Move room
            dar2.room.transform.Translate(idealHallwayDir * roomDistance);

            // Check for collisions
            collided = RoomFunctions.CheckForCollisions(dar2.room, scale);

            debugInt++;
        }

        // get new tile positions
        tile1 = ComputeDoorTile(dar1.door);
        tile2 = ComputeDoorTile(dar2.door);

        //Debug.Log($"Tile1 {tile1} Tile2 {tile2}");

        // Create a path in between doors
        List<Vector3> hallwayPath = GeneralFunctions.FindShortestAvoidingTiles(tile1, tile2, scale);

        if (hallwayPath != null) { hallwayPath.Insert(0, tile1); hallwayPath.Insert(hallwayPath.Count - 1, tile2); }
        else { Debug.Log("A* couldn't find a path"); }

        // Spawn hallway
        GameObject hallwayParent = SpawnNetworkedObject(null, roomParentObject, Vector3.zero, Quaternion.identity);
        hallwayParent.name = "Hallway";

        Vector3 prevPos = dar1.door.transform.position;
        for (int i = 0; i < hallwayPath.Count - 1; i++)
        {
            SpawnNetworkedObject(hallwayParent.transform, tiles, hallwayPath[i], Quaternion.identity);
            List<Vector3> wallPositions = RoomFunctions.GetAllWallPositions();
            DrawWallsAroundDoors(prevPos, hallwayPath[i], hallwayPath[i + 1], hallwayParent, wallPositions);
            prevPos = hallwayPath[i];
        }

        return hallwayParent;
    }

    /// <summary> Compute the tile outside the given door </summary>
    Vector3 ComputeDoorTile(GameObject door)
    {
        // Assume the door's up should point away from where the floor tile should be placed.
        Vector3 doorDir = -door.transform.up.normalized;
        Vector3 tilePos = door.transform.position + doorDir * scale/2;
        return tilePos; 
    }

    /// <summary> Outlines a hallway with walls </summary>
    void DrawWallsAroundDoors(Vector3 prevPos, Vector3 pos, Vector3 nextPos, GameObject parent, List<Vector3> wallPositions)
    {
        List<Vector3> positions = new List<Vector3>{pos+Vector3.left*scale, pos+Vector3.back*scale, 
            pos+Vector3.right*scale,  pos+Vector3.forward*scale};

        // Define outward directions for walls
        Vector3[] outwardDirections = new Vector3[4] {
            Vector3.left,    // Pointing West
            Vector3.back,    // Pointing North
            Vector3.right,   // Pointing East
            Vector3.forward  // Pointing South
        };

        Vector3[] spawnPos = new Vector3[4] {
            new(pos.x - scale / 2, 2.5f, pos.z), // floor to right (spawn left)
            new(pos.x, 2.5f, pos.z - scale / 2), // floor above (spawn below)
            new(pos.x + scale / 2, 2.5f, pos.z), // floor to left (spawn right)
            new(pos.x, 2.5f, pos.z + scale / 2) // floor below (spawn up)
        };

        for (int i=0; i<positions.Count; i++)
        {
            bool collided = false;
            foreach (Vector3 wallPos in wallPositions)
            {
                if ((wallPos - spawnPos[i]).sqrMagnitude < 10) { collided = true; continue; }
            }

            if (!collided && positions[i] != prevPos && positions[i] != nextPos)
            {
                Quaternion rotation = Quaternion.LookRotation(outwardDirections[i], Vector3.up) * Quaternion.Euler(-90, 0, 0);
                GameObject newObject = SpawnNetworkedObject(parent.transform, walls, spawnPos[i], rotation);
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
