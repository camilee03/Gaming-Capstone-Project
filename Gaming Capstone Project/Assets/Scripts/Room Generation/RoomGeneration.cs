using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;

public class RoomGeneration : MonoBehaviour
{
    // Syntax:
    // '' = undefined
    // d = defined
    // w = wall
    // f = floor

    [Header("Room data storage")]
    bool[,] roomTiles;
    bool[,] outline;
    char[,] room;
    List<Vector2> finalRoomTiles;
    List<int> finishedRooms = new(); // holds rooms that have been paired

    public List<(char[,] room, List<Vector2> finalRoomTiles, GameObject roomParent)> roomsInfo;
    public bool finishedProcedure;
    int size;

    [Header("Spawn Data")]
    public float scale = 10; // how many tiles apart are different objects
    public int seed = -1; // set to zero if no seed wanted
    [SerializeField] GameObject tiles;
    [SerializeField] GameObject walls;
    [SerializeField] GameObject doors;
    List<GameObject> spawnedTiles;
    List<GameObject> spawnedOutline;
    List<int> outlineDirections;
    List<(GameObject door, int pos, GameObject room)> doorAndRoom;

    void Awake()
    {
        if (seed != -1) { Random.InitState(seed); }
        spawnedOutline = new();
        outlineDirections = new();
        spawnedTiles = new();
        GenerateMultipleRooms();
    }

    /// <summary> Draws the generated room and returns the relevant room GameObject </summary>
    GameObject DrawRoom()
    {
        // Create parenting objects which will store room
        GameObject newRoom = new GameObject("RoomTempName");
        GameObject wallParent = new GameObject("WallParent");
        wallParent.transform.SetParent(newRoom.transform, false);
        GameObject tileParent = new GameObject("TileParent");
        tileParent.transform.SetParent(newRoom.transform, false);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (room[x, y] == 'f') // spawn floor
                {
                    GameObject newObject = Object.Instantiate(tiles, tileParent.transform, true);
                    newObject.name = "Tile" + x + "" + y;

                    Vector3 position = new Vector3(x * scale, 2.5f, y * scale);
                    newObject.transform.position = position;
                    spawnedTiles.Add(newObject);

                    finalRoomTiles.Add(new Vector2(x,y));
                }
                else if (room[x, y] == 'w') // spawn wall
                {
                    SpawnWall(walls, x, y, wallParent.transform);
                }

            }
        }

        return newRoom;
    }

    /// <summary> See where to spawn designated objects </summary>
    void SpawnWall(GameObject child, int x, int y, Transform parent)
    {
        GameObject newObject = null;

        bool[] locations = new bool[4] { // conditions to spawn
            x+1<size && room[x+1, y] == 'f', // floor to right (place left)
            y+1<size && room[x, y+1] == 'f', // floor down (place up)
            x-1>0 && room[x-1, y] == 'f', // floor left (place right)
            y-1>0 && room[x, y-1] == 'f' }; // floor up (place down)

        // Check which walls need to be spawned in
        for (int i = 0; i < locations.Length; i++)
        {
            if (locations[i]) // if can spawn
            {
                newObject = GameObject.Instantiate(child, parent, true);
                newObject.transform.position = ConvertTileToPos(x, y, i, true);
                newObject.transform.localRotation = Quaternion.Euler(-90, ((i + 1) % 2) * 90, 0);
                newObject.name = "Wall" + i;

                spawnedOutline.Add(newObject);
                outlineDirections.Add(i);
            }
        }

        finalRoomTiles.Add(new Vector2(x, y));

    }

    /// <summary> Takes in a Vector3 and returns the position on the grid </summary>
    (int x, int y) ConvertPosToTile(Vector3 pos, int direction, bool isWall)
    {
        float addition = 0;
        if (isWall) { addition = scale * 2; }

        (int x, int y) spawnLeft = ((int) ((pos.x - addition) / scale), (int) (pos.z / scale));
        (int x, int y) spawnRight = ((int)((pos.x + addition) / scale), (int) (pos.z / scale));
        (int x, int y) spawnAbove = ((int)(pos.x / scale), (int) ((pos.z - addition) / scale));
        (int x, int y) spawnBelow = ((int)(pos.x / scale), (int) ((pos.z + addition) / scale));
        (int x, int y)[] spawnPositions = { spawnLeft, spawnAbove, spawnRight, spawnBelow };

        return spawnPositions[direction];
    }

    /// <summary> Takes in a grid position and finds out what direction its facing (mostly used for walls) </summary>
    Vector3 ConvertTileToPos(int x, int y, int direction, bool isWall)
    {
        float addition = 0;
        if (isWall) { addition = scale / 2; }
        Vector3 spawnLeft = new Vector3(x * scale + addition, 2.5f, y * scale);
        Vector3 spawnRight = new Vector3(x * scale - addition, 2.5f, y * scale);
        Vector3 spawnAbove = new Vector3(x * scale, 2.5f, y * scale + addition);
        Vector3 spawnBelow = new Vector3(x * scale, 2.5f, y * scale - addition);
        Vector3[] spawnPos = new Vector3[4] { spawnLeft, spawnAbove, spawnRight, spawnBelow };

        return spawnPos[direction];
    }

    /// <summary> Deletes old instances of room </summary>
    void ClearMap() { 
        foreach (GameObject tile in spawnedTiles)
        {
            Object.Destroy(tile);
        }
        spawnedTiles = new();

        foreach (GameObject line in spawnedOutline)
        {
            Object.Destroy(line);
        }
        spawnedOutline = new();
    }

    /// <summary> Creates new bounds for a room </summary>
    void GenerateNewRoom()
    {
        size = Random.Range(10, 20);
        room = new char[size, size]; // set random room size
        finalRoomTiles = new(); // reset filled room tiles

        int numSquares = Random.Range(1, 5); // determine how many squares will be generated

        for (int i = 0; i < numSquares; i++) // for each square, place randomly in the grid
        {
            int x0 = Random.Range(3, size - 6); // leave gap on edges
            int y0 = Random.Range(3, size - 6); 
            int width = Random.Range(3, size - (x0 + 3)); // at least 3 w&h to prevent small rooms
            int height = Random.Range(3, size - (y0 + 3));

            PlaceSquare(x0, y0, width, height); // for each square generated, place tile in array
        }
    }

    /// <summary> Adds the generated room to the array </summary>
    void PlaceSquare(int x0, int y0, int width, int height)
    {
        for (int x = x0; x < width + x0; x++)
        {
            for (int y = y0; y < height + y0; y++)
            {
                room[x,y] = 'd'; // set as defined: floor will be determined later
            }
        }
    }

    /// <summary> Determines where the outline will go </summary>
    void OutlineRoom(int x, int y, int currLocation)
    {
        int down = 1;
        int up = -1;
        bool problem = false;

        char tileCode = 'w';

        switch (currLocation)
        {
            case 0: // above
                //Debug.Log("Going right at " + x + "," + y);
                if (room[x+1,y] == 'd') 
                {
                    if (room[x + 1, y + up] == 'd') { y += up; currLocation = 3; } // go up
                    else { y += up; x += 1; } // weave and go right
                } 
                else if (room[x + 1, y + down] == 'd') { x += 1; } // continue right
                else if (room[x, y + 1 * down] == 'd') { y += down; x += 1; currLocation = 1; } // go down
                else { problem = true; }
                break;

            case 1: // right
                //Debug.Log("Going down at " + x + "," + y);
                if (room[x, y + down] == 'd') 
                {
                    if (room[x + 1, y + down] == 'd') { x += 1; currLocation = 0; } // go right
                    else { x += 1; y += down; } // weave and go down
                } 
                else if (room[x - 1, y + down] == 'd') { y += down; } // continue down
                else if (room[x - 1, y] == 'd') { y += down; x -= 1; currLocation = 2; } // go left
                else { problem = true; }
                break;

            case 2: // below
                //Debug.Log("Going left at " + x + "," + y);
                if (room[x - 1, y] == 'd') 
                {
                    if (room[x - 1, y + down] == 'd') { y += down; currLocation = 1; } // go down
                    else { y += down; x -= 1; } // weave and go left
                } 
                else if (room[x - 1, y + up] == 'd') { x -= 1; } // continue left
                else if (room[x, y + 1 * up] == 'd') { y += up; x -= 1; currLocation = 3; } // go up
                else { problem = true; }
                break;

            case 3: // left
                //Debug.Log("Going up at " + x + "," + y);
                if (room[x, y + up] == 'd') 
                { 
                    if (room[x - 1, y + up] == 'd')  { x -= 1; currLocation = 2; } // go left
                    else { x -= 1; y += up; } // weave and go up
                } // go left
                else if (room[x + 1, y + up] == 'd') { y += up; } // continue up
                else if (room[x + 1, y] == 'd') { y += up; x += 1; currLocation = 0; } // go right
                else { problem = true; }
                break;

        }

        // Check to see if outline is done
        if (x >= size | y >= size | x < 0 | y < 0) { Debug.Log("Too big or small: " + x + ", " + y); }
        else if (room[x, y] != 'w' && !problem) { room[x, y] = tileCode; OutlineRoom(x, y, currLocation); }
        else if (problem) { Debug.Log("PROBLEM"); }

    }

    /// <summary> From L>R U>D finds the first d tile </summary>
    int[] FindFirstTile()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (room[x, y] == 'd')
                {
                    return new int[2] { x, y - 1}; // y = y - 1 because of outline
                }
            }
        }

        return null;
    }
    
    /// <summary> Turns d tiles into f tiles </summary>
    void CleanRoom()
    {
        bool inZeros = false; // if within the bounds of the wall
        int currentWalls = 0; // the num of walls surrounding something
        char prev = ' ';

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (room[x, y] == 'w') { inZeros = true; } // enter walls
                else if (inZeros && room[x, y] == ' ') { inZeros = false; currentWalls = 0; } // exit walls
                else if (inZeros && room[x, y] == 'd')
                {
                    if (prev == 'w') { currentWalls++; } // add surrounding wall
                    if (currentWalls < 2) { room[x, y] = 'f'; } // found inside walls
                    else { currentWalls = 0; }
                }
                else if (!inZeros && room[x,y] == 'd') { room[x, y] = ' '; } // find outside walls

                prev = room[x,y];
            }
            currentWalls = 0; inZeros = false;
        }
    }

    /// <summary> Starts the room generation </summary>
    GameObject RoomProcedure(int roomNum)
    {
        GameObject newRoom = null;
        GenerateNewRoom(); // create the array for the new room

        int[] tile = FindFirstTile();
        if (tile != null)
        {
            OutlineRoom(tile[0], tile[1], 0); // create the array for the outline
            CleanRoom();

            newRoom = DrawRoom(); // draw room and outline
            //wallParent.transform.position = Vector3.zero;
            //tileParent.transform.position = Vector3.zero;
        }

        newRoom.name = "Room" + roomNum;
        roomsInfo.Add((room, finalRoomTiles, newRoom));

        return newRoom;
    }

    /// <summary> Use the room procedure to create multiple rooms </summary>
    void GenerateMultipleRooms()
    {
        roomsInfo = new();
        ClearMap(); // only neccessary if regenerating the entire map

        int numPlayers = 2;
        int numRooms = (int)Mathf.Pow(2, numPlayers);
        GameObject[] rooms = new GameObject[numRooms];


        // -- Maybe try to take out this section at some point? --//

        doorAndRoom = new(); // set by replacewallwithdoor

        for (int i = 0; i < numRooms; i++) // spawn in each room and set doors
        {
            rooms[i] = RoomProcedure(i);
            ReplaceWallWithDoor(rooms[i].transform.GetChild(0).gameObject, rooms[i]);
        }

        rooms = PairRooms();
        numRooms /= 2;

        //-- end --//


        while (numRooms > 1)
        {
            doorAndRoom = new();
            for (int i = 0; i < numRooms; i++)
            {
                // choose a room from paired rooms to add a door to
                GameObject chosenRoom = GetRootChild(rooms[i], "WallParent");

                ReplaceWallWithDoor(chosenRoom.transform.GetChild(0).gameObject, rooms[i]);
            }

            rooms = PairRooms();
            numRooms /= 2;
        }

        finishedProcedure = true;
    }

    GameObject GetRootChild(GameObject parent, string name)
    {
        if (parent.transform.GetChild(0).name == name) { return parent; }
        else {
            int chooseRoom = Random.Range(0, parent.transform.childCount - 1);
            return GetRootChild(parent.transform.GetChild(chooseRoom).gameObject, name); 
        }
    }

    /// <summary> Chooses random walls to replace with a door </summary>
    void ReplaceWallWithDoor(GameObject wallParent, GameObject room)
    {
        int index = Random.Range(0, wallParent.transform.childCount - 1); // find random wall

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
    GameObject[] PairRooms()
    {
        int numRooms = doorAndRoom.Count;
        int roomIndex = 0;
        GameObject[] newRooms = new GameObject[numRooms]; // stores generated parent rooms
        finishedRooms = new(); // stores indices of finished rooms

        for (int i = 0; i < numRooms; i++) 
        {
            if (finishedRooms.Contains(i)) { continue; }

            (GameObject door, int pos, GameObject room) dar1 = doorAndRoom[i]; // new door to pair

            // try to pair doors
            GameObject newRoomParent = PairDoors(i, dar1);

            if (newRoomParent != null)
            {
                newRooms[roomIndex] = newRoomParent; 
                roomIndex++;
            }
            else
            {
                // rerotate dar to attempt to fit
                Debug.Log("Rotating room");

                if (dar1.pos > 1) { dar1.pos -= 2; }
                else { dar1.pos += 2; }
                dar1.room.transform.Rotate(new Vector3(0, 180, 0));

                // try to pair doors again
                newRoomParent = PairDoors(i, dar1);
                if (newRoomParent != null) 
                {
                    newRooms[roomIndex] = newRoomParent; 
                    roomIndex++;
                }
                else { Debug.Log("ERROR: no combination found"); }
            }
        }

        return newRooms;
    }
    
    /// <summary> Assigns old rooms and hallway to new room and returns new room </summary>
    GameObject PairDoors(int i, (GameObject door, int pos, GameObject room) dar1)
    {
        float roomDistance = 2 * scale;
        GameObject newRoomParent = null;
        (GameObject door, int pos, GameObject room) dar2 = new();

        for (int ii = i; ii < doorAndRoom.Count; ii++)
        {
            if (finishedRooms.Contains(ii)) { continue; }

            dar2 = doorAndRoom[ii]; // new door to pair
            if (dar1.room.name.Contains(dar2.room.name.Remove(0,4)) || dar2.room.name.Contains(dar1.room.name.Remove(0,4))) { continue; }

            if (dar1.pos != dar2.pos) // if the doors face different directions
            {
                // Create a hallway
                GameObject newHallway = LinkDoors(dar1, dar2, roomDistance);

                // add paired rooms to finished
                finishedRooms.Add(i); finishedRooms.Add(ii);

                // change room parent to new gameobject
                newRoomParent = new GameObject("Room" + dar1.room.name.Remove(0,4) + dar2.room.name.Remove(0,4));
                dar1.room.transform.parent = newRoomParent.transform;
                dar2.room.transform.parent = newRoomParent.transform;
                newHallway.transform.parent = newRoomParent.transform;

                break;
            }
        }

        return newRoomParent;
    }

    /// <summary> Take two doors and tie them together </summary>
    GameObject LinkDoors((GameObject door, int pos, GameObject room) dar1, (GameObject door, int pos, GameObject room) dar2, float roomDistance)
    {
        Vector3[] directions = { Vector3.left, Vector3.back, Vector3.right, Vector3.forward };

        Vector3 tile1 = dar1.door.transform.position + directions[dar1.pos] * scale/2;
        Vector3 tile2 = dar2.door.transform.position + directions[dar2.pos] * scale/2;

        Vector3 doorDisplacement = tile1 - tile2; // start at same place


        // move room based on door displacement
        doorDisplacement += roomDistance * directions[dar1.pos];
        if (dar1.pos + 2 != dar2.pos && dar1.pos - 2 != dar2.pos)
        {
            doorDisplacement -= roomDistance * directions[dar2.pos];
        }
        dar2.room.transform.Translate(doorDisplacement);

        // move again if too close
        int debugInt = 0;
        while (CheckForCollisions(dar2.room) && debugInt < 100)
        {
            dar2.room.transform.Translate(doorDisplacement/2);
            Debug.Log(debugInt);
            debugInt++;
        }

        //Debug.Log("Dir: " + dar1.pos + " Pos: " + dar1.door.transform.position);
        //Debug.Log("Dir: " + dar2.pos + " Pos: " + dar2.door.transform.position);

        // recheck tile positions
        tile1 = dar1.door.transform.position + directions[dar1.pos] * scale / 2; // different from first?
        tile2 = dar2.door.transform.position + directions[dar2.pos] * scale / 2;

        // Create a path in between doors
        List<Vector3> hallwayPath = GeneralFunctions.FindShortestPathExcludingCollisions(tile1, tile2, scale);
        hallwayPath.Insert(0, tile1); hallwayPath.Insert(hallwayPath.Count - 1, tile2);

        GameObject hallwayParent = new GameObject("Hallway");
        Vector3 prevPos = dar1.door.transform.position;
        for (int i=0; i < hallwayPath.Count-1; i++)
        {
            GameObject.Instantiate(tiles, hallwayPath[i], Quaternion.identity, hallwayParent.transform);
            DrawWallsAroundDoors(prevPos, hallwayPath[i], hallwayPath[i + 1], hallwayParent);
            prevPos = hallwayPath[i];
        }

        return hallwayParent;
    }

    bool CheckForCollisions(GameObject room)
    {
        bool hasCollided = false;

        // Iterate through trying to find a collision
        if (room.transform.GetChild(1).name == "TileParent")
        {
            Transform tiles = room.transform.GetChild(1);

            int childNum = tiles.childCount;

            for (int i = 0; i < childNum; i++)
            {
                Collider[] collisions = Physics.OverlapBox(tiles.GetChild(i).position, Vector3.one * size);
                if (collisions.Length > 0) { Debug.Log(tiles.GetChild(i).name + " hit something"); return true; }
            }
        }
        // Go to next child to find collision
        else
        {
            for (int i = 0; i < room.transform.childCount-1; i++)
            {
                hasCollided = hasCollided || CheckForCollisions(room.transform.GetChild(i).gameObject);
            }
        }

        if (hasCollided) { Debug.Log("should have hit something"); }
        return hasCollided;
    }
    
    /// <summary> Outlines a hallway with walls </summary>
    void DrawWallsAroundDoors(Vector3 prevPos, Vector3 pos, Vector3 nextPos, GameObject parent)
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
            if (positions[i] != prevPos && positions[i] != nextPos)
            {
                GameObject newObject = GameObject.Instantiate(walls, parent.transform, true);
                newObject.transform.position = spawnPos[i];
                newObject.transform.localRotation = Quaternion.Euler(-90, ((i + 1) % 2) * 90, 0);
            }
        }
    }
}
