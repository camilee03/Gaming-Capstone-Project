using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RoomGeneration : MonoBehaviour
{
    // Syntax:
    // '' = undefined
    // d = defined
    // w = wall
    // f = floor


    bool[,] roomTiles;
    bool[,] outline;

    char[,] room;
    List<Vector2> finalRoomTiles;
    Vector2 startTile;

    public List<(char[,] room, List<Vector2> finalRoomTiles, Vector2 startTile, float scale)> roomsInfo;
    public bool finishedProcedure;

    int size;
    float scale = 10; // how many tiles apart are different objects
    [SerializeField] GameObject tiles;
    [SerializeField] GameObject walls;
    [SerializeField] GameObject doors;
    List<GameObject> spawnedTiles;
    List<GameObject> spawnedOutline;
    List<int> outlineDirections;
    List<(GameObject door, int pos, GameObject room)> doorAndRoom;

    void Start()
    {
        Random.InitState(10);
        spawnedOutline = new List<GameObject>();
        outlineDirections = new List<int>();
        spawnedTiles = new List<GameObject>();
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
                    Vector3 position = new Vector3((x-startTile.x) * scale, 2.5f, (y-startTile.y) * scale);
                    GameObject newObject = Object.Instantiate(tiles, tileParent.transform, true);
                    newObject.transform.position = position;
                    spawnedTiles.Add(newObject);

                    finalRoomTiles.Add(new Vector2(x,y));
                }
                else if (room[x, y] == 'w') // spawn wall
                {
                    SpawnObject(walls, x, y, wallParent.transform);
                }

            }
        }

        return newRoom;
    }

    /// <summary> See where to spawn designated objects </summary>
    void SpawnObject(GameObject child, int x, int y, Transform parent) 
    {
        GameObject newObject = null; 

        if (startTile.x == 0 && startTile.y == 0) { startTile.x = x; startTile.y = y; } // set first tile if not already set

        bool[] locations = new bool[4] { // conditions to spawn
                        x+1<size && room[x+1, y] == 'f', // floor to right (place left)
                        y+1<size && room[x, y+1] == 'f', // floor down (place up)
                        x-1>0 && room[x-1, y] == 'f', // floor left (place right)
                        y-1>0 && room[x, y-1] == 'f' }; // floor up (place down)

        // Create spawn locations for each direction
        Vector3 spawnLeft = new Vector3((x - startTile.x) * scale + scale/2, 2.5f, (y - startTile.y) * scale);
        Vector3 spawnRight = new Vector3((x - startTile.x) * scale - scale/2, 2.5f, (y - startTile.y) * scale);
        Vector3 spawnAbove = new Vector3((x - startTile.x) * scale, 2.5f, (y - startTile.y) * scale + scale / 2);
        Vector3 spawnBelow = new Vector3((x - startTile.x) * scale, 2.5f, (y - startTile.y) * scale - scale / 2);
        Vector3[] spawnPos = new Vector3[4] { spawnLeft, spawnAbove, spawnRight, spawnBelow };

        // Check which walls need to be spawned in
        for (int i = 0; i < locations.Length; i++)
        {
            if (locations[i]) // if can spawn
            {
                newObject = GameObject.Instantiate(child, parent, true);
                newObject.transform.position = spawnPos[i];
                newObject.transform.localRotation = Quaternion.Euler(-90, ((i + 1) % 2) * 90, 0);
                spawnedOutline.Add(newObject);
                outlineDirections.Add(i);
            }
        }

        finalRoomTiles.Add(new Vector2(x, y));

    }

    /// <summary> Deletes old instances of room </summary>
    void ClearMap() { 
        foreach (GameObject tile in spawnedTiles)
        {
            Object.Destroy(tile);
        }
        spawnedTiles = new List<GameObject>();

        foreach (GameObject line in spawnedOutline)
        {
            Object.Destroy(line);
        }
        spawnedOutline = new List<GameObject>();
    }

    /// <summary> Creates new bounds for a room </summary>
    void GenerateNewRoom()
    {
        size = Random.Range(10, 20);
        room = new char[size, size]; // set random room size
        finalRoomTiles = new List<Vector2>(); // reset filled room tiles

        int numSquares = Random.Range(1, 5); // determine how many squares will be generated

        for (int i = 0; i < numSquares; i++) // for each square, place randomly in the grid
        {
            int x0 = Random.Range(3, size - 5); // leave gap on edges
            int y0 = Random.Range(3, size - 5); 
            int width = Random.Range(2, size - (x0 + 3)); // at least 2 width to prevent small rooms
            int height = Random.Range(2, size - (y0 + 3));

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
    void OutlineRoom(int x, int y, int currLocation, int numDoors)
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
        else if (room[x, y] != 'w' && !problem) { room[x, y] = tileCode; OutlineRoom(x, y, currLocation, numDoors); }
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
    GameObject RoomProcedure()
    {
        GameObject newRoom = null;
        GenerateNewRoom(); // create the array for the new room

        int[] tile = FindFirstTile();
        if (tile != null)
        {
            OutlineRoom(tile[0], tile[1], 0, 0); // create the array for the outline
            CleanRoom();

            newRoom = DrawRoom(); // draw room and outline
            //wallParent.transform.position = Vector3.zero;
            //tileParent.transform.position = Vector3.zero;
        }

        roomsInfo.Add((room, finalRoomTiles, startTile, scale));

        return newRoom;
    }

    /// <summary> Use the room procedure to create multiple rooms </summary>
    void GenerateMultipleRooms()
    {
        roomsInfo = new List<(char[,] room, List<Vector2> finalRoomTiles, Vector2 startTile, float scale)>();
        ClearMap(); // only neccessary if regenerating the entire map

        int numRooms = 1;
        GameObject[] rooms = new GameObject[numRooms];

        doorAndRoom = new List<(GameObject door, int pos, GameObject room)>();

        for (int i = 0; i < numRooms; i++) // spawn in each room and set doors
        {
            rooms[i] = RoomProcedure();
            ReplaceWallWithDoor(1, rooms[i].transform.GetChild(0).gameObject, rooms[i]);
        }

        if (numRooms > 1) { MoveAndPairDoors(); }

        finishedProcedure = true;
    }

    /// <summary> Chooses random walls to replace with a door </summary>
    void ReplaceWallWithDoor(int numDoors, GameObject wallParent, GameObject room)
    {
        for (int i = 0; i < numDoors; i++)
        {
            int index = Random.Range(0, spawnedOutline.Count - 1);

            // set door rotation as same as wall
            Transform wallTransform = spawnedOutline[index].transform;
            Vector3 wallPosition = wallTransform.position;
            Quaternion wallRotation = wallTransform.rotation;
            Destroy(spawnedOutline[index]);

            GameObject tempObject = GameObject.Instantiate(doors, wallPosition, wallRotation, wallParent.transform);
            doorAndRoom.Add((tempObject, outlineDirections[index], room));
        }
    }

    /// <summary> Take spawned rooms and doors and connect together </summary>
    void MoveAndPairDoors()
    {
        // Ok so I'm thinking this is the first iteration with only one set of doors. Then run 
        // replacewallwithdoors again and connect doors based on where the rooms' positions are

        // 0 = left, 1 = up, 2 = right, 3 = down

        int debugInt = 0;
        float roomDistance = 10 * scale;
        bool perfectlyAligned = true;

        while (doorAndRoom.Count > 0 && debugInt < 100 && perfectlyAligned)
        {
            (GameObject door, int pos, GameObject room) dar = doorAndRoom[0]; // new door to pair

            debugInt++;

            // check to see if any doors perfectly allign
            perfectlyAligned = false;
            foreach ((GameObject door, int pos, GameObject room) darNext in doorAndRoom)
            {
                if (dar.room != darNext.room)
                {
                    Debug.Log("Aligned " + dar.pos + " " + darNext.pos);
                    if (darNext.pos + 2 == dar.pos || darNext.pos - 2 == dar.pos)
                    {
                        Vector3 doorDisplacement;
                        Vector3 direction;
                        switch (dar.pos)
                        {
                            case 1:
                                doorDisplacement = new Vector3((dar.door.transform.position.x - darNext.door.transform.position.x), 0, roomDistance);
                                direction = Vector3.forward; break;
                            case 3:
                                doorDisplacement = new Vector3((-dar.door.transform.position.x + darNext.door.transform.position.x), 0, roomDistance);
                                direction = Vector3.forward; break;
                            case 0:
                                doorDisplacement = new Vector3(roomDistance, 0, (dar.door.transform.position.z - darNext.door.transform.position.z));
                                direction = Vector3.right; break;
                            case 2:
                                doorDisplacement = new Vector3(roomDistance, 0, (-dar.door.transform.position.z + darNext.door.transform.position.z));
                                direction = Vector3.right; break;
                            default: doorDisplacement = Vector3.zero; direction = Vector3.zero; break;
                        }

                        darNext.room.transform.Translate(doorDisplacement); // move room based on door positions
                        CreatePathBetweenDoors(dar.door.transform.position+direction*scale/2, darNext.door.transform.position+direction*scale/2);
                        doorAndRoom.RemoveAt(0); doorAndRoom.Remove(darNext); // now that the doors are assigned, remove
                        perfectlyAligned = true;
                        break;
                    }
                }
            }

            // if there were no perfectly alligned rooms:
            if (!perfectlyAligned)
            {
                Debug.Log("Not aligned");
                foreach ((GameObject door, int pos, GameObject room) darNext in doorAndRoom)
                {
                    if (dar.room != darNext.room)
                    {
                        Debug.Log(dar.pos + " " + darNext.pos);
                        if (darNext.pos != dar.pos)
                        {
                            Vector3 doorDisplacement = Vector3.zero; Vector3 direction = Vector3.zero;

                            switch (dar.pos)
                            {
                                case 1:
                                case 3:
                                    int mult = 1;
                                    if (dar.pos == 1) { mult = -1; }
                                    if (darNext.pos == 2) // left
                                    {
                                        doorDisplacement = new Vector3((dar.door.transform.position.x - darNext.door.transform.position.x) - 2* scale, 0, mult* roomDistance);
                                        direction = Vector3.forward;
                                    }
                                    else if (darNext.pos == 0) // right
                                    {
                                        doorDisplacement = new Vector3((dar.door.transform.position.x - darNext.door.transform.position.x) + 2*scale, 0, mult*roomDistance);
                                        direction = Vector3.forward;
                                    }
                                    break;
                                case 0:
                                case 2:
                                    mult = 1;
                                    if (dar.pos == 0) { mult = -1; }
                                    if (darNext.pos == 1) // up
                                    {
                                        doorDisplacement = new Vector3(mult * roomDistance, 0, (dar.door.transform.position.z - darNext.door.transform.position.z) + 2*scale);
                                        direction = Vector3.right;
                                    }
                                    else if (darNext.pos == 3) // down
                                    {
                                        doorDisplacement = new Vector3(mult* roomDistance, 0, (dar.door.transform.position.z - darNext.door.transform.position.z) - 2*scale);
                                        direction = Vector3.right;
                                    }
                                    break;
                            }

                            darNext.room.transform.Translate(doorDisplacement); // move room based on door positions
                            CreatePathBetweenDoors(dar.door.transform.position, darNext.door.transform.position);
                            doorAndRoom.RemoveAt(0); doorAndRoom.Remove(darNext); // now that the doors are assigned, remove
                            perfectlyAligned = true;
                            break;
                        }
                    }
                }
            }

            if (!perfectlyAligned) { 
                Debug.Log("ERROR"); 
                if (dar.pos > 3) { dar.pos -= 2; }
                else { dar.pos += 2; }
                dar.room.transform.Rotate(new Vector3(0, 180, 0));

                foreach ((GameObject door, int pos, GameObject room) darNext in doorAndRoom)
                {
                    if (dar.room != darNext.room)
                    {
                        Debug.Log("newly aligned " + dar.pos + " " + darNext.pos);
                        if (darNext.pos + 2 == dar.pos || darNext.pos - 2 == dar.pos)
                        {
                            Vector3 doorDisplacement;
                            Vector3 direction;
                            switch (dar.pos)
                            {
                                case 1:
                                    doorDisplacement = new Vector3((dar.door.transform.position.x - darNext.door.transform.position.x), 0, roomDistance);
                                    direction = Vector3.forward; break;
                                case 3:
                                    doorDisplacement = new Vector3((-dar.door.transform.position.x + darNext.door.transform.position.x), 0, roomDistance);
                                    direction = Vector3.forward; break;
                                case 0:
                                    doorDisplacement = new Vector3(roomDistance, 0, (dar.door.transform.position.z - darNext.door.transform.position.z));
                                    direction = Vector3.right; break;
                                case 2:
                                    doorDisplacement = new Vector3(roomDistance, 0, (-dar.door.transform.position.z + darNext.door.transform.position.z));
                                    direction = Vector3.right; break;
                                default: doorDisplacement = Vector3.zero; direction = Vector3.zero; break;
                            }

                            darNext.room.transform.Translate(doorDisplacement); // move room based on door positions
                            CreatePathBetweenDoors(dar.door.transform.position + direction * scale / 2, darNext.door.transform.position + direction * scale / 2);
                            doorAndRoom.RemoveAt(0); doorAndRoom.Remove(darNext); // now that the doors are assigned, remove
                            perfectlyAligned = true;
                            break;
                        }
                    }
                }

            }
        }
    }

    void CreatePathBetweenDoors(Vector3 door1, Vector3 door2)
    {
        DataStructures.PriorityQueue<(Vector3, List<Vector3>)> fringe = new DataStructures.PriorityQueue<(Vector3, List<Vector3>)>();

        Vector3 currentState = door1; // start from position of door1

        foreach (Vector3 successor in GetSuccessors(currentState))
        {
            List<Vector3> path = new List<Vector3>();
            path.Add(successor);
            fringe.Push((successor, path), (int)Vector3.Distance(successor, door2)); // change to heuristic later
        }

        List<Vector3> closed = new List<Vector3>();
        closed.Add(currentState);
        List<Vector3> solution = new List<Vector3>();
        List<Vector3> currentPath = new List<Vector3>();
        (Vector3 pos, List<Vector3> path) node;

        //While fringe set is not empty
        while (fringe.count > 0)
        {
            node = fringe.Pop();
            currentState = node.pos;
            currentPath = node.path;

            // Move to this state and finish
            if (Vector3.Distance(door2, currentState) < scale)
            {
                solution = currentPath;

                GameObject hallwayParent = new GameObject("Hallway");

                foreach (Vector3 sol in solution)
                {
                    GameObject newPath = GameObject.Instantiate(tiles, hallwayParent.transform);
                    newPath.transform.position = sol;
                }
                break;
            }

            // Check if state is already expanded
            if (!closed.Contains(currentState))
            {
                closed.Add(currentState);

                foreach (Vector3 successor in GetSuccessors(currentState))
                {
                    List<Vector3> path = currentPath;
                    path.Add(successor);
                    fringe.Push((successor, path), (int)Vector3.Distance(successor, door2)); // change to heuristic later
                }
            }
        }

    }

    List<Vector3> GetSuccessors(Vector3 parent)
    {
        List<Vector3> successors = new List<Vector3>();
        successors.Add(parent + new Vector3(scale, 0, 0));
        successors.Add(parent + new Vector3(-scale, 0, 0));
        successors.Add(parent + new Vector3(0, 0, scale));
        successors.Add(parent + new Vector3(0, 0, -scale));

        return successors;
    }
}
