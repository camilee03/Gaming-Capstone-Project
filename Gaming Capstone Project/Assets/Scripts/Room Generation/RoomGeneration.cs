using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RoomGeneration : MonoBehaviour
{
    bool[,] roomTiles;
    bool[,] outline;
    int size;
    [SerializeField] GameObject tiles;
    [SerializeField] GameObject lines;
    List<GameObject> spawnedTiles;
    List<GameObject> spawnedOutline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnedOutline = new List<GameObject>();
        spawnedTiles = new List<GameObject>();
        RoomProcedure();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearRoom(); // get rid of any remaining tiles
            RoomProcedure();
        }
    }

    void DrawRoom()
    {
        float scale = 10;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (roomTiles[x, y])
                {
                    Vector3 position = new Vector3(x * scale, 0, y * scale);
                    GameObject newObject = Object.Instantiate(tiles);
                    newObject.transform.position = position;
                    spawnedTiles.Add( newObject );
                }
            }
        }
    }

    void DrawOutline()
    {
        float scale = 10;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (outline[x, y])
                {
                    Vector3 position = new Vector3(x * scale, 0, y * scale);
                    GameObject newObject = Object.Instantiate(lines);
                    newObject.transform.position = position;
                    spawnedOutline.Add(newObject);
                }
            }
        }
    }

    void ClearRoom() { 
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

    void GenerateNewRoom()
    {
        size = Random.Range(5, 20);
        roomTiles = new bool[size, size]; // set random room size

        int numSquares = Random.Range(1, 5); // determine how many squares will be generated

        for (int i = 0; i < numSquares; i++) // for each square, place randomly in the grid
        {
            int x0 = Random.Range(3, size - 4); // leave gap on edges
            int y0 = Random.Range(3, size - 4); 
            int width = Random.Range(1, size - (x0 + 3));
            int height = Random.Range(1, size - (y0 + 3));

            PlaceSquare(x0, y0, width, height);
        }
    }

    void PlaceSquare(int x0, int y0, int width, int height)
    {
        for (int x = x0; x < width + x0; x++)
        {
            for (int y = y0; y < height + y0; y++)
            {
                roomTiles[x,y] = true;
                Debug.Log(x + ", " + y);
            }
        }
    }

    void OutlineRoom(int x, int y, int currLocation)
    {
        int down = 1;
        int up = -1;
        bool problem = false;

        switch (currLocation)
        {
            case 0: // above
                Debug.Log("Going right");
                if (roomTiles[x+1,y]) 
                {
                    if (roomTiles[x + 1, y + up]) { y += up; currLocation = 3; } // go up
                    else { y += up; x += 1; } // weave and go right
                } 
                else if (roomTiles[x+1,y+down]) { x += 1; } // continue right
                else if (roomTiles[x, y+2*down]) { y += down; x += 1; currLocation = 1; } // go down
                else { problem = true; }
                break;
            case 1: // right
                Debug.Log("Going down");
                if (roomTiles[x, y+down]) 
                {
                    if (roomTiles[x + 1, y + down]) { x += 1; currLocation = 0; } // go right
                    else { x += 1; y += down; } // weave and go down
                } 
                else if (roomTiles[x-1, y+down]) { y += down; } // continue down
                else if (roomTiles[x-2, y]) { y += down; x -= 1; currLocation = 2; } // go left
                else { problem = true; }
                break;
            case 2: // below
                Debug.Log("Going left");
                if (roomTiles[x-1, y]) 
                {
                    if (roomTiles[x - 1, y + up]) { y += down; currLocation = 1; } // go down
                    else { y += down; x -= 1; } // weave and go left
                } 
                else if (roomTiles[x-1, y+up]) { x -= 1; } // continue left
                else if (roomTiles[x, y+2*up]) { y += up; x -= 1; currLocation = 3; } // go up
                else { problem = true; }
                break;
            case 3: // left
                Debug.Log("Going up");
                if (roomTiles[x, y+up]) 
                { 
                    if (roomTiles[x-1, y + up])  { x -= 1; currLocation = 2; } // go left
                    else { x -= 1; y += up; } // weave and go up
                } // go left
                else if (roomTiles[x+1, y+up]) { y += up; } // continue up
                else if (roomTiles[x+2, y]) { y += up; x += 1; currLocation = 0; } // go right
                else { problem = true; }
                break;

        }

        // Check to see if outline is done
        if (!outline[x, y] && !problem) { outline[x, y] = true; OutlineRoom(x, y, currLocation); }
        else if (problem) { Debug.Log("PROBLEM"); }

    }

    int[] FindFirstTile()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (roomTiles[x, y])
                {
                    Debug.Log("Starting at " + x + " : " + (y-1));
                    return new int[2] { x, y - 1}; // y = y - 1 because of outline
                }
            }
        }

        return null;
    }

    void RoomProcedure()
    {
        GenerateNewRoom(); // create the array for the new room

        outline = new bool[size, size]; // reset the outline array
        int[] tile = FindFirstTile();
        OutlineRoom(tile[0], tile[1], 0); // create the array for the outline

        DrawRoom(); // draw room
        DrawOutline(); // draw outline
    }
}
