using System.Collections.Generic;
using System.Linq;
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
        roomsInfo = new List<(char[,] room, List<Vector2> finalRoomTiles, GameObject roomParent)>();
        ClearMap(); // only neccessary if regenerating the entire map

        int numRooms = 4;
        GameObject[] rooms = new GameObject[numRooms];

        doorAndRoom = new();

        for (int i = 0; i < numRooms; i++) // spawn in each room and set doors
        {
            rooms[i] = RoomProcedure(i);
            ReplaceWallWithDoor(1, rooms[i].transform.GetChild(0).gameObject, rooms[i], roomsInfo[i].room);
        }

        if (numRooms > 1) { MoveAndPairDoors(numRooms); }

        finishedProcedure = true;
    }

    /// <summary> Chooses random walls to replace with a door </summary>
    void ReplaceWallWithDoor(int numDoors, GameObject wallParent, GameObject room, char[,] roomTiles)
    {
        for (int i = 0; i < numDoors; i++)
        {
            int index = Random.Range(0, wallParent.transform.childCount - 1); // find random wall

            // set door rotation as same as wall
            Transform wallTransform = wallParent.transform.GetChild(index);
            Vector3 wallPosition = wallTransform.position;
            Quaternion wallRotation = wallTransform.rotation;

            string name = wallParent.transform.GetChild(index).gameObject.name;
            char lastChar = name[name.Length - 1];
            int direction = (int) char.GetNumericValue(lastChar);

            // replace wall with door
            Destroy(wallParent.transform.GetChild(index).gameObject);
            GameObject tempObject = GameObject.Instantiate(doors, wallPosition, wallRotation, wallParent.transform);

            doorAndRoom.Add((tempObject, direction, room));
        }
    }

    /// <summary> Take spawned rooms and doors and connect together </summary>
    void MoveAndPairDoors(int numRooms)
    {
        // Ok so I'm thinking this is the first iteration with only one set of doors. Then run 
        // replacewallwithdoors again and connect doors based on where the rooms' positions are

        // 0 = left, 1 = up, 2 = right, 3 = down

        float roomDistance = 4 * scale;
        bool perfectlyAligned;

        bool[,] pairedRooms = new bool[doorAndRoom.Count,doorAndRoom.Count];
        List<int> finishedRooms = new List<int>();

        for (int i = 0; i < numRooms; i++) 
        {
            perfectlyAligned = false;

            if (finishedRooms.Contains(i)) { break; }

            (GameObject door, int pos, GameObject room) dar1 = doorAndRoom[i]; // new door to pair
            (GameObject door, int pos, GameObject room) dar2 = new();

            for (int ii = i; ii < doorAndRoom.Count; ii++)
            {
                if (finishedRooms.Contains(ii)) { break; }

                dar2 = doorAndRoom[ii]; // new door to pair

                if (dar1.pos != dar2.pos)
                {
                    LinkDoors(dar1, dar2, roomDistance);

                    perfectlyAligned = true;
                    finishedRooms.Add(i);
                    finishedRooms.Add(ii);
                    pairedRooms[i, ii] = true;
                    break;
                }
            }

            if (!perfectlyAligned)
            {
                Debug.Log("Rotating room");
                // rerotate dar to attempt to fit
                if (dar1.pos > 3) { dar1.pos -= 2; }
                else { dar1.pos += 2; }

                dar1.room.transform.Rotate(new Vector3(0, 180, 0));

                // rerun program
                for (int ii = i; ii < doorAndRoom.Count; ii++)
                {
                    if (finishedRooms.Contains(ii)) { break; }

                    dar2 = doorAndRoom[ii]; // new door to pair

                    if (dar1.pos != dar2.pos)
                    {
                        LinkDoors(dar1, dar2, roomDistance);

                        perfectlyAligned = true;

                        finishedRooms.Add(i);
                        finishedRooms.Add(ii);
                        pairedRooms[i, ii] = true;
                        break;
                    }
                }
            }
        }
    }
    
    /// <summary> Take two doors and tie them together </summary>
    void LinkDoors((GameObject door, int pos, GameObject room) dar1, (GameObject door, int pos, GameObject room) dar2, float roomDistance)
    {
        Vector3[] directions = { Vector3.left, Vector3.forward, Vector3.right, Vector3.back };

        Debug.Log(dar1.pos + " -- " + dar2.pos);

        Vector3 tile1 = dar1.door.transform.position + directions[dar1.pos] * scale/2;
        Vector3 tile2 = dar2.door.transform.position + directions[dar2.pos] * scale/2;

        Vector3 doorDisplacement = tile1 - tile2; // start at same place
        Debug.Log("Door displacement before:" + doorDisplacement);

        doorDisplacement += roomDistance * directions[dar1.pos];
        if (dar1.pos + 2 != dar2.pos || dar1.pos - 2 != dar2.pos)
        {
            doorDisplacement += roomDistance * directions[dar2.pos];
        }

        Debug.Log("Door displacement after:" + doorDisplacement);

        // move room based on door displacement
        dar2.room.transform.Translate(doorDisplacement);
        //dar2.door.transform.Translate(doorDisplacement);

        Debug.Log("Dir: " + dar1.pos + " Pos: " + dar1.door.transform.position);
        Debug.Log("Dir: " + dar2.pos + " Pos: " + dar2.door.transform.position);

        tile1 = dar1.door.transform.position - directions[dar1.pos] * scale / 2;
        tile2 = dar2.door.transform.position - directions[dar2.pos] * scale / 2;

        // Create a path in between doors
        List<Vector3> hallwayPath = GeneralFunctions.FindShortestPath(tile1, tile2, scale);
        GameObject hallwayParent = new GameObject("Hallway");
        Vector3 prevPos = dar1.door.transform.position;
        for (int i=0; i < hallwayPath.Count-1; i++)
        {
            GameObject.Instantiate(tiles, hallwayPath[i], Quaternion.identity, hallwayParent.transform);
            DrawWallsAroundDoors(prevPos, hallwayPath[i], hallwayPath[i + 1], hallwayParent);
            prevPos = hallwayPath[i];
        }
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
