using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomObjectType : MonoBehaviour
{
    List<Vector2> tiles; // tiles in the room that are untaken (maybe sort by constraints?)
    List<Vector2> preTiles;
    char[,] descripterTiles; // what each tile has on it
    RoomGeneration roomGenerator;
    Vector2 startTile;
    List<GameObject> spawnedObjects = new List<GameObject>();

    public bool spawnObjects;
    [SerializeField] GameObject tempObject;

    enum ItemType { Box, Button, Lever, Light, Table, Chair, 
        Doors, BulletinBoard, Radio, Terminal, Fan, Wires, 
        Furnace, Coal, Vent, Food, WashingMachine, Clothes, Cabinet, 
        EnergyCore, Trash }

    enum TaskCategory { Pickup, Interactable, Attack }

    enum TaskType { Visual, Audio, Tactile, None }

    enum Constraints { None, Orientation, Wall, Ceiling, }

    private void Start()
    {
        roomGenerator = GetComponent<RoomGeneration>();   
    }

    private void Update()
    {
        preTiles = roomGenerator.finalRoomTiles;
        descripterTiles = roomGenerator.room;
        startTile = roomGenerator.startTile;

        if (spawnObjects)
        {
            BacktrackingSearchTest();
        }
    }

    private bool SpawnObject(char newTileCode, char oldTileCode, Vector2 location) // determines if objects can be spawned according to constraints
    {
        switch (newTileCode)
        {
            case 'd': // door
                if (oldTileCode == 'w') { return true; } break;
            case 'b': // bulletin board
                if (oldTileCode == 'w') { return true; } break;
            case 't': // DOS terminal
                if (oldTileCode == 'f') { return true; } break;
            case 'l': // light
                if (oldTileCode != 'f' && oldTileCode != 'w') { return true; } break;
            case 'F': // fan
                if (oldTileCode == 'f') { return true; } break;
            default:
                break;

        }
        return false;
    }

    private void BacktrackingSearchTest()
    {
        CleanRoom(); // get rid of old objects

        char[] objectList = new char[5] { 't', 't', 't', 't', 't'};
        int scale = 10;
        int randomTile = 0; char oldCode;

        spawnObjects = false;
        for (int i = 0; i < objectList.Length - 1; i++)
        {
            bool canSpawn = false;
            randomTile = Random.Range(0, tiles.Count-2); // find new unassigned tile
            int iterations = 1;

            //-- Genreation Start --//
            while (!canSpawn || iterations == tiles.Count)
            {
                if (randomTile < tiles.Count - 1) { randomTile++; } else { randomTile = 0; }

                if (tiles[randomTile] != null)
                {
                    oldCode = descripterTiles[(int)tiles[randomTile].x, (int)tiles[randomTile].y]; // char code of current tile
                    canSpawn = SpawnObject(objectList[i], oldCode, tiles[randomTile]); 
                }
                else { canSpawn = false; }

                iterations++;
            }
            //-- Generation Done --//

            tiles.Remove(tiles[randomTile]); // remove changed tile from potential tile list
            descripterTiles[(int)tiles[randomTile].x, (int)tiles[randomTile].y] = objectList[i]; // set new tile
            
            GameObject newObject = GameObject.Instantiate(tempObject,
                new Vector3((tiles[randomTile].x - startTile.x) * scale, 2.5f, (tiles[randomTile].y - startTile.y) * scale),
                Quaternion.identity);
            spawnedObjects.Add(newObject);
        }
    }

    private void CleanRoom()
    {
        tiles = preTiles; // reset what tiles are taken

        foreach (GameObject obj in spawnedObjects)
        {
            GameObject.Destroy(obj);
        }
        spawnedObjects = new List<GameObject>();
    }
    private void DetermineTheme() // determine task type and theme based on GPT
    {

    }

    private void DetermineTask() // determine tasks based on GPT model
    { 

    }

    private void AddScripts() // add scripts to objects for interaction and connection when needed
    { 

    }




    // Each object has a value x,y that determines where it is on the graph
    // For every x,y certain functions must be applied to make each scenario true
    // Trim borders to make it easier?

    // Step 1: Choose random point inside room that hasn't been assigned yet (maybe keep a list for simplicity?)
    // Step 2: Choose an object to place in point that fufills constraints (decide on ordering --> maybe place big items first?)
    // Step 3: Repeat until no objects are left

    // Some global constraints: objects must be inside room / on wall, objects cannot be assigned to same place as another
}
