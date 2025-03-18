using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class RoomGeneration : MonoBehaviour
{
    // Syntax:
    // '' = undefined
    // d = defined
    // w = wall
    // f = floor

    [Header("Debug Values")]
    [SerializeField] int numPlayers = 1;
    public bool debug;


    List<int> finishedRooms = new(); // holds rooms that have been paired

    public List<Room> rooms = new();

    [Header("Spawn Data")]
    public float scale = 10; // how many tiles apart are different objects
    public int seed = -1; // set to -1 if no seed wanted
    [SerializeField] GameObject tiles;
    [SerializeField] GameObject walls;
    [SerializeField] GameObject doors;
    List<(GameObject door, int pos, GameObject room)> doorAndRoom;
    RoomObjectType roomObjectType;

    [Header("Collision Data")]
    bool collided;
    bool coroutineRunning;

    void Start()
    {
        roomObjectType = GetComponent<RoomObjectType>();
        //StartGeneration();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) { StartGeneration(); }
    }


    // -- ROOM GENERATION -- //

    public void StartGeneration()
    {
        if (debug) { Random.InitState(DebugGen.Instance.seed); }
        else { if (seed != -1) { Random.InitState(seed); } }

        GenerateMultipleRooms();
    }

    /// <summary> Use the room procedure to create multiple rooms </summary>
    void GenerateMultipleRooms()
    {
        ClearMap(); // only neccessary if regenerating the entire map

        int numRooms = numPlayers * 2;

        // Spawn first room
        Room newRoom = new(scale, tiles, walls);
        newRoom.RoomProcedure(0);
        rooms.Add(newRoom);
        GameObject room1 = newRoom.parent;
        numRooms--;
        int index = 1;

        while (numRooms > 0)
        {
            // choose a random room from room1 to edit
            GameObject chosenRoom = RoomFunctions.GetRootChild(room1, "WallParent", 0, true)[0];

            // Spawn next room
            Room room2 = new(scale, tiles, walls);
            room2.RoomProcedure(index);
            rooms.Add(room2);
            numRooms--;

            // Get new doors
            doorAndRoom = new();
            ReplaceWallWithDoor(room2.wallParent, room2.parent);
            ReplaceWallWithDoor(chosenRoom.transform.GetChild(0).gameObject, room1);

            // Link two rooms together
            room1 = RotateRooms();

            if (room1 == null) { Debug.Log("ERROR: Rooms cannot connect"); }
            index++;
        }



        // Spawn Objects for each room
        roomObjectType.ObjectGeneration();

    }


    /// <summary> Deletes any stray objects </summary>
    void ClearMap()
    {
        GameObject[] spawnRooms = GameObject.FindGameObjectsWithTag("Room");

        foreach (GameObject room in spawnRooms)
        {
            if (room != null) { GameObject.Destroy(room); }
        }

        rooms = new();
    }
   



    // -- DOOR GENERATION -- //

    /// <summary> Chooses random walls to replace with a door </summary>
    void ReplaceWallWithDoor(GameObject wallParent, GameObject room)
    {
        bool findWall = true;
        int index = 0;

        while (findWall)
        {
            index = Random.Range(0, wallParent.transform.childCount - 1); // find random wall
            findWall = wallParent.transform.GetChild(index).gameObject.name[0] != 'W';
        }

        // set door rotation as same as wall
        Transform wallTransform = wallParent.transform.GetChild(index);
        Vector3 wallPosition = wallTransform.position;
        Quaternion wallRotation = wallTransform.rotation;

        string name = wallParent.transform.GetChild(index).gameObject.name;
        char lastChar = name[name.Length - 1];
        int direction = (int)char.GetNumericValue(lastChar);

        // replace wall with door
        Destroy(wallParent.transform.GetChild(index).gameObject);
        GameObject tempObject = GameObject.Instantiate(doors, wallPosition, wallRotation, wallParent.transform);

        doorAndRoom.Add((tempObject, direction, room));
    }

    /// <summary> Take spawned rooms and doors, connect together, and return the list of paired rooms </summary>
    GameObject RotateRooms()
    {
        (GameObject door, int pos, GameObject room) dar1 = doorAndRoom[0]; // door1
        (GameObject door, int pos, GameObject room) dar2 = doorAndRoom[1]; // door2

        // try to pair doors
        GameObject newRoomParent = PairDoors(dar2, dar1);

        if (newRoomParent != null) { return newRoomParent; }
        else
        {
            // rerotate dar to attempt to fit
            Debug.Log("Rotating room");

            if (dar1.pos > 1) { dar1.pos -= 2; }
            else { dar1.pos += 2; }
            dar1.room.transform.Rotate(new Vector3(0, 180, 0));

            // try to pair doors again
            newRoomParent = PairDoors(dar2, dar1);
            if (newRoomParent != null)
            {
                return newRoomParent;
            }
            else { Debug.Log("ERROR: no combination found"); return null; }

        }
    }

    /// <summary> Assigns old rooms and hallway to new room and returns new room </summary>
    GameObject PairDoors((GameObject door, int pos, GameObject room) dar2, (GameObject door, int pos, GameObject room) dar1)
    {
        float roomDistance = 2 * scale;
        GameObject newRoomParent = null;

        if (dar1.pos != dar2.pos) // if the doors face different directions
        {
            // Create a hallway
            GameObject newHallway = LinkDoors(dar1, dar2, roomDistance);
            if (newHallway == null) { Debug.Log("Couldn't find a path"); return null; }

            // change room parent to new gameobject
            newRoomParent = new GameObject("Room" + dar1.room.name.Remove(0, 4) + dar2.room.name.Remove(0, 4));
            newRoomParent.tag = "Room";
            dar1.room.transform.parent = newRoomParent.transform;
            dar2.room.transform.parent = newRoomParent.transform;
            newHallway.transform.parent = newRoomParent.transform;
        }

        return newRoomParent;
    }


    /// <summary> Take two doors and tie them together </summary>
    GameObject LinkDoors((GameObject door, int pos, GameObject room) dar1, (GameObject door, int pos, GameObject room) dar2, float roomDistance)
    {
        Vector3[] directions = { Vector3.left, Vector3.back, Vector3.right, Vector3.forward };

        Vector3 tile1 = dar1.door.transform.position + directions[dar1.pos] * scale / 2;
        Vector3 tile2 = dar2.door.transform.position + directions[dar2.pos] * scale / 2;

        Vector3 doorDisplacement = tile1 - tile2; // start at same place


        // move room based on door displacement
        doorDisplacement += roomDistance * directions[dar1.pos];
        if (dar1.pos + 2 != dar2.pos && dar1.pos - 2 != dar2.pos)
        {
            doorDisplacement -= roomDistance * directions[dar2.pos];
        }

        // move room if too close
        int debugInt = 0;
        collided = true;

        while (collided && debugInt < 100)
        {
            Debug.Log("Collided. Moving " + doorDisplacement);
            dar2.room.transform.Translate(doorDisplacement);
            collided = RoomFunctions.CheckForCollisions(dar2.room, scale);
            debugInt++;
        }

        //Debug.Log("Dir: " + dar1.pos + " Pos: " + dar1.door.transform.position);
        //Debug.Log("Dir: " + dar2.pos + " Pos: " + dar2.door.transform.position);

        // recheck tile positions
        tile1 = dar1.door.transform.position + directions[dar1.pos] * scale / 2;
        tile2 = dar2.door.transform.position + directions[dar2.pos] * scale / 2;

        // Create a path in between doors
        List<Vector3> hallwayPath = GeneralFunctions.FindShortestAvoidingTiles(tile1, tile2, scale);

        if (hallwayPath != null) { hallwayPath.Insert(0, tile1); hallwayPath.Insert(hallwayPath.Count - 1, tile2); }
        else { Debug.Log("A* couldn't find a path"); if (debug) { DebugGen.Instance.seed++; SceneManager.LoadScene(0); return null; } }

        GameObject hallwayParent = new GameObject("Hallway");
        Vector3 prevPos = dar1.door.transform.position;
        for (int i = 0; i < hallwayPath.Count - 1; i++)
        {
            GameObject.Instantiate(tiles, hallwayPath[i], Quaternion.identity, hallwayParent.transform);
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
                GameObject newObject = GameObject.Instantiate(walls, parent.transform, true);
                newObject.transform.position = spawnPos[i];
                newObject.transform.localRotation = Quaternion.Euler(-90, ((i + 1) % 2) * 90, 0);
            }
        }
    }
}
