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

    public char[,] room;
    public List<Vector2> finalRoomTiles;
    public Vector2 startTile;

    int size;
    public float scale = 10; // how many tiles apart are different objects
    [SerializeField] GameObject tiles;
    [SerializeField] GameObject walls;
    [SerializeField] GameObject doors;
    List<GameObject> spawnedTiles;
    List<GameObject> spawnedOutline;

    void Start()
    {
        spawnedOutline = new List<GameObject>();
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
                    if (startTile.x == 0 && startTile.y == 0) { startTile.x = x; startTile.y = y; }
                    // NOTE: try to make this less of a mess, but this
                    // may have to stay this way to prevent holes

                    bool[] locations = new bool[4] { 
                        x+1<size && room[x+1, y] == 'f',
                        y+1<size && room[x, y+1] == 'f', 
                        x-1>0 && room[x-1, y] == 'f', 
                        y-1>0 && room[x, y-1] == 'f' };

                    Vector3 spawnLeft = new Vector3((x - startTile.x) * scale + 5, 2.5f, (y - startTile.y) * scale);
                    Vector3 spawnRight = new Vector3((x - startTile.x) * scale - 5, 2.5f, (y - startTile.y) * scale);
                    Vector3 spawnAbove = new Vector3((x - startTile.x) * scale, 2.5f, (y - startTile.y) * scale + 5);
                    Vector3 spawnBelow = new Vector3((x - startTile.x) * scale, 2.5f, (y - startTile.y) * scale - 5);
                    Vector3[] spawnPos = new Vector3[4] {spawnLeft, spawnAbove, spawnRight, spawnBelow };

                    for (int i = 0; i<locations.Length; i++)
                    {
                        if (locations[i])
                        {
                            GameObject newObject = Object.Instantiate(walls, wallParent.transform, true);
                            newObject.transform.position = spawnPos[i];
                            newObject.transform.rotation = new Quaternion(0, (i+1) % 2, 0, (i+1) % 2);
                            spawnedOutline.Add(newObject);
                        }
                    }

                    finalRoomTiles.Add(new Vector2(x,y));
                }
            }
        }

        return newRoom;
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
        Debug.Log(size + " " + room.Length);
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
        int doorChance = Random.Range(0, 1);
        //if (doorChance == 0) { tileCode = 'D'; }

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
        else if (room[x, y] != 'w' && room[x,y] != 'D' && !problem) { room[x, y] = tileCode; OutlineRoom(x, y, currLocation, numDoors); }
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
        GameObject newRoom = new GameObject();
        GenerateNewRoom(); // create the array for the new room

        int[] tile = FindFirstTile();
        if (tile != null)
        {
            int numDoors = 2;
            OutlineRoom(tile[0], tile[1], 0, numDoors); // create the array for the outline
            CleanRoom();

            newRoom = DrawRoom(); // draw room and outline
            //wallParent.transform.position = Vector3.zero;
            //tileParent.transform.position = Vector3.zero;
        }

        return newRoom;
    }

    /// <summary> Use the room procedure to create multiple rooms </summary>
    void GenerateMultipleRooms()
    {
        ClearMap(); // only neccessary if regenerating the entire map

        int numRooms = 3;
        GameObject[] rooms = new GameObject[numRooms];
        int previousRoomSize = 0;

        for (int i = 0; i < numRooms; i++)
        {
            rooms[i] = RoomProcedure();
            rooms[i].transform.position += previousRoomSize * Vector3.right * scale * i;
            previousRoomSize = size;
        }
    }

    void CreatePathBetweenDoors(Vector3 door1, Vector2 door2)
    {
    }
}
