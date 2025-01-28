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
    int size;
    [SerializeField] GameObject tiles;
    [SerializeField] GameObject lines;
    List<GameObject> spawnedTiles;
    List<GameObject> spawnedOutline;

    void Start()
    {
        spawnedOutline = new List<GameObject>();
        spawnedTiles = new List<GameObject>();
        RoomProcedure();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearRoom(); // get rid of any remaining tiles
            RoomProcedure();
        }
    }

    /// <summary> Draws the generated room </summary>
    void DrawRoom()
    {
        float scale = 10;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (room[x, y] == 'd') // spawn floor (CHANGE TO f)
                {
                    Vector3 position = new Vector3(x * scale, 0, y * scale);
                    GameObject newObject = Object.Instantiate(tiles);
                    newObject.transform.position = position;
                    spawnedTiles.Add(newObject);
                }
                else if (room[x, y] == 'w') // spawn wall
                {
                    Vector3 position = new Vector3(x * scale, 0, y * scale);
                    GameObject newObject = Object.Instantiate(lines);
                    newObject.transform.position = position;
                    spawnedOutline.Add(newObject);
                }
            }
        }
    }

    /// <summary> Deletes old instances of a room </summary>
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

    /// <summary> Creates new bounds for a room </summary>
    void GenerateNewRoom()
    {
        size = Random.Range(5, 20);
        room = new char[size, size]; // set random room size

        int numSquares = Random.Range(1, 5); // determine how many squares will be generated

        for (int i = 0; i < numSquares; i++) // for each square, place randomly in the grid
        {
            int x0 = Random.Range(4, size - 5); // leave gap on edges
            int y0 = Random.Range(4, size - 5); 
            int width = Random.Range(2, size - (x0 + 4)); // at least 2 width to prevent small rooms
            int height = Random.Range(2, size - (y0 + 4));

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

        switch (currLocation)
        {
            case 0: // above
                Debug.Log("Going right");
                if (room[x+1,y] == 'd') 
                {
                    if (room[x + 1, y + up] == 'd') { y += up; currLocation = 3; } // go up
                    else { y += up; x += 1; } // weave and go right
                } 
                else if (room[x + 1, y + down] == 'd') { x += 1; } // continue right
                else if (room[x, y + 2 * down] == 'd') { y += down; x += 1; currLocation = 1; } // go down
                else { problem = true; }
                break;

            case 1: // right
                Debug.Log("Going down");
                if (room[x, y + down] == 'd') 
                {
                    if (room[x + 1, y + down] == 'd') { x += 1; currLocation = 0; } // go right
                    else { x += 1; y += down; } // weave and go down
                } 
                else if (room[x - 1, y + down] == 'd') { y += down; } // continue down
                else if (room[x - 2, y] == 'd') { y += down; x -= 1; currLocation = 2; } // go left
                else { problem = true; }
                break;

            case 2: // below
                Debug.Log("Going left");
                if (room[x - 1, y] == 'd') 
                {
                    if (room[x - 1, y + up] == 'd') { y += down; currLocation = 1; } // go down
                    else { y += down; x -= 1; } // weave and go left
                } 
                else if (room[x - 1, y + up] == 'd') { x -= 1; } // continue left
                else if (room[x, y + 2 * up] == 'd') { y += up; x -= 1; currLocation = 3; } // go up
                else { problem = true; }
                break;

            case 3: // left
                Debug.Log("Going up");
                if (room[x, y + up] == 'd') 
                { 
                    if (room[x - 1, y + up] == 'd')  { x -= 1; currLocation = 2; } // go left
                    else { x -= 1; y += up; } // weave and go up
                } // go left
                else if (room[x + 1, y + up] == 'd') { y += up; } // continue up
                else if (room[x + 2, y] == 'd') { y += up; x += 1; currLocation = 0; } // go right
                else { problem = true; }
                break;

        }

        // Check to see if outline is done
        if (x >= size | y >= size) { Debug.Log("Too big: " + x + ", " + y); }
        else if (room[x, y] != 'w' && !problem) { room[x, y] = 'w'; OutlineRoom(x, y, currLocation); }
        else if (problem) { Debug.Log("PROBLEM"); }
        else { Debug.Log("Room outlined"); }

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
                    Debug.Log("Starting at " + x + " : " + (y-1));
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
        int currentZeros = 0;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (room[x, y] == 'w') { inZeros = true; currentZeros++; }
                else if (inZeros && room[x, y] == ' ') { inZeros = false; }
                else if (inZeros && room[x,y] == 'd') { room[x, y] = 'f'; }
                else if (!inZeros && room[x,y] == 'd') { room[x, y] = ' '; }

                if (currentZeros == 2) { currentZeros = 0; inZeros = false; }
            }
        }
    }

    /// <summary> Starts the room generation </summary>
    void RoomProcedure()
    {
        GenerateNewRoom(); // create the array for the new room

        int[] tile = FindFirstTile();
        if (tile != null)
        {
            OutlineRoom(tile[0], tile[1], 0); // create the array for the outline
                                              //CleanRoom();

            DrawRoom(); // draw room and outline
        }
    }
}
