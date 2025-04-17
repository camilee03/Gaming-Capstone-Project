using System.Collections.Generic;
using System.Drawing;
using Unity.Netcode;
using UnityEngine;

public class Room : NetworkBehaviour
{

    [Header("Public variables")]
    public GameObject parent;
    public GameObject wallParent; // children = all walls & doors
    public List<GameObject> spawnedWalls = new();
    public GameObject tileParent; // children = all floor tiles
    public GameObject objectParent; // children = all objects
    public string roomName;
    public char[,] objectLocations;


    [Header("Room data storage")]
    bool[,] roomTiles;
    bool[,] outline;


    List<GameObject> spawnedTiles = new();

    [HideInInspector] public int size;
    [HideInInspector] public float scale;

    [Header("Spawn Type")]
    GameObject tile;
    GameObject wall;
    public GameObject roomObject;
    public GameObject roomParentObject;


    // -- Public Functions -- //

    public Room(float scale, GameObject tile, GameObject wall, GameObject roomObject, GameObject roomParentObject)
    {
        this.scale = scale;
        this.tile = tile;
        this.wall = wall;
        this.roomObject = roomObject;
        this.roomParentObject = roomParentObject;
    }

    /// <summary> Starts the room generation </summary>
    public void RoomProcedure(int roomNum, ObjectGeneration objectSpawner)
    {
        GameObject newRoom = null;
        GenerateNewRoom(); // create the array for the new room

        int[] tile = FindFirstTile();
        if (tile != null)
        {
            OutlineRoom(tile[0], tile[1], 0); // create the array for the outline
            CleanRoom();

            newRoom = DrawRoom(); // draw room and outline
        }

        newRoom.name = "Room" + roomNum;
        roomName = newRoom.name;
        newRoom.tag = "Room";

        parent = newRoom;
    }

    /// <summary> Deletes old instances of room </summary>
    public void ClearRoom()
    {
        GameObject.Destroy(parent);
    }


    // -- ROOM GENERATION -- //

    /// <summary> 
    /// Draws the generated room and returns the relevant room GameObject </summary>
    GameObject DrawRoom()
    {
        // Create parenting objects which will store room
        GameObject newRoom = SpawnNetworkedObject(null, roomObject, Vector3.zero, Quaternion.identity);

        wallParent = SpawnNetworkedObject(newRoom.transform, roomParentObject, Vector3.zero, Quaternion.identity);
        wallParent.name = "WallParent";

        tileParent = SpawnNetworkedObject(newRoom.transform, roomParentObject, Vector3.zero, Quaternion.identity);
        tileParent.name = "TileParent";

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (objectLocations[x, y] == 'f') // spawn floor
                {
                    GameObject newObject = SpawnNetworkedObject(tileParent.transform, tile, new Vector3(x * scale, 2.5f, y * scale), Quaternion.identity);
                    newObject.name = "Tile" + x + "" + y;
                    spawnedTiles.Add(newObject);
                }
                else if (objectLocations[x, y] == 'w') // spawn wall
                {
                    SpawnWall(wall, x, y, wallParent.transform);
                }

            }
        }

        return newRoom;
    }

    /// <summary> Creates new bounds for a room </summary>
    void GenerateNewRoom()
    {
        size = Random.Range(11, 20);
        objectLocations = new char[size, size]; // set random room size

        int numSquares = Random.Range(1, 5); // determine how many squares will be generated

        for (int i = 0; i < numSquares; i++) // for each square, place randomly in the grid
        {
            int x0 = Random.Range(4, size - 6); // leave gap on edges
            int y0 = Random.Range(3, size - 6);
            int width = Random.Range(3, size - (x0 + 3)); // at least 3 w&h to prevent small rooms
            int height = Random.Range(3, size - (y0 + 3));

            PlaceSquare(x0, y0, width, height); // for each square generated, place tile in array
        }
    }



    // -- WALL GENERATION -- //

    /// <summary> See where to spawn designated objects </summary>
    void SpawnWall(GameObject child, int x, int y, Transform parent)
    {
        GameObject newObject = null;

        // Define outward directions for walls
        Vector3[] outwardDirections = new Vector3[4] {
        Vector3.left,    // Pointing West
        Vector3.back,    // Pointing North
        Vector3.right,   // Pointing East
        Vector3.forward  // Pointing South
        };

        bool[] locations = new bool[4] { // conditions to spawn
            x+1<size && objectLocations[x+1, y] == 'f', // floor to right (place left)
            y+1<size && objectLocations[x, y+1] == 'f', // floor down (place up)
            x-1>0 && objectLocations[x-1, y] == 'f', // floor left (place right)
            y-1>0 && objectLocations[x, y-1] == 'f' }; // floor up (place down)

        // Check which walls need to be spawned in
        for (int i = 0; i < locations.Length; i++)
        {
            if (locations[i]) // if can spawn
            {
                // Calculate the outward direction
                Vector3 outwardDir = outwardDirections[i];

                // Combine -90 degrees on x-axis with outward direction rotation
                Quaternion wallRotation = Quaternion.LookRotation(outwardDir, Vector3.up) * Quaternion.Euler(-90, 0, 0);
                newObject = SpawnNetworkedObject(parent, child, RoomFunctions.ConvertTileToPos(x, y, i, true, scale), wallRotation);


                newObject.name = "Wall" + i;

                spawnedWalls.Add(newObject);
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
                if (objectLocations[x + 1, y] == 'd')
                {
                    if (objectLocations[x + 1, y + up] == 'd') { y += up; currLocation = 3; } // go up
                    else { y += up; x += 1; } // weave and go right
                }
                else if (objectLocations[x + 1, y + down] == 'd') { x += 1; } // continue right
                else if (objectLocations[x, y + 1 * down] == 'd') { y += down; x += 1; currLocation = 1; } // go down
                else { problem = true; }
                break;

            case 1: // right
                //Debug.Log("Going down at " + x + "," + y);
                if (objectLocations[x, y + down] == 'd')
                {
                    if (objectLocations[x + 1, y + down] == 'd') { x += 1; currLocation = 0; } // go right
                    else { x += 1; y += down; } // weave and go down
                }
                else if (objectLocations[x - 1, y + down] == 'd') { y += down; } // continue down
                else if (objectLocations[x - 1, y] == 'd') { y += down; x -= 1; currLocation = 2; } // go left
                else { problem = true; }
                break;

            case 2: // below
                //Debug.Log("Going left at " + x + "," + y);
                if (objectLocations[x - 1, y] == 'd')
                {
                    if (objectLocations[x - 1, y + down] == 'd') { y += down; currLocation = 1; } // go down
                    else { y += down; x -= 1; } // weave and go left
                }
                else if (objectLocations[x - 1, y + up] == 'd') { x -= 1; } // continue left
                else if (objectLocations[x, y + 1 * up] == 'd') { y += up; x -= 1; currLocation = 3; } // go up
                else { problem = true; }
                break;

            case 3: // left
                //Debug.Log("Going up at " + x + "," + y);
                if (objectLocations[x, y + up] == 'd')
                {
                    if (objectLocations[x - 1, y + up] == 'd') { x -= 1; currLocation = 2; } // go left
                    else { x -= 1; y += up; } // weave and go up
                } // go left
                else if (objectLocations[x + 1, y + up] == 'd') { y += up; } // continue up
                else if (objectLocations[x + 1, y] == 'd') { y += up; x += 1; currLocation = 0; } // go right
                else { problem = true; }
                break;

        }

        // Check to see if outline is done
        if (x >= size | y >= size | x < 0 | y < 0) { Debug.Log("Too big or small: " + x + ", " + y); }
        else if (objectLocations[x, y] != 'w' && !problem) { objectLocations[x, y] = tileCode; OutlineRoom(x, y, currLocation); }
        else if (problem) { Debug.Log("PROBLEM"); }

    }


    // -- TILE GENERATION -- //

    /// <summary> Adds the generated room to the array </summary>
    void PlaceSquare(int x0, int y0, int width, int height)
    {
        for (int x = x0; x < width + x0; x++)
        {
            for (int y = y0; y < height + y0; y++)
            {
                objectLocations[x, y] = 'd'; // set as defined: floor will be determined later
            }
        }
    }

    /// <summary> From L>R U>D finds the first d tile </summary>
    int[] FindFirstTile()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (objectLocations[x, y] == 'd')
                {
                    return new int[2] { x, y - 1 }; // y = y - 1 because of outline
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
                if (objectLocations[x, y] == 'w') { inZeros = true; } // enter walls
                else if (inZeros && objectLocations[x, y] == ' ') { inZeros = false; currentWalls = 0; } // exit walls
                else if (inZeros && objectLocations[x, y] == 'd')
                {
                    if (prev == 'w') { currentWalls++; } // add surrounding wall
                    if (currentWalls < 2) { objectLocations[x, y] = 'f'; } // found inside walls
                    else { currentWalls = 0; }
                }
                else if (!inZeros && objectLocations[x, y] == 'd') { objectLocations[x, y] = ' '; } // find outside walls

                prev = objectLocations[x, y];
            }
            currentWalls = 0; inZeros = false;
        }
    }


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
