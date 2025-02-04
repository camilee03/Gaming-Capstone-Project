using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomObjectType : MonoBehaviour
{
    List<Vector2> tiles; // tiles in the room that are untaken (maybe sort by constraints?)
    char[,] descripterTiles; // what each tile has on it
    RoomGeneration roomGenerator;
    Vector2 startTile;

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
        tiles = roomGenerator.finalRoomTiles;
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
            case 'd':
                if (oldTileCode == 'w') { return true; } break;
            case 'b':
                if (oldTileCode == 'w') { return true; } break;
            case 't':
                if (oldTileCode == 'f') { return true; } break;
            case 'l':
                if (oldTileCode != 'w') { return true; } break;
            case 'F':
                if (oldTileCode == 'f') { return true; } break;
            default:
                break;

        }
        return false;
    }

    private void BacktrackingSearchTest()
    {
        char[] objectList = new char[5] { 'd', 'b', 't', 'l', 'f'};
        int index = 0;
        int scale = 10;

        //while (index < objectList.Length) // EDIT: currently runs in an infinite loop
        //{
            int randomTile = Random.Range(0, tiles.Count);
            if (SpawnObject(objectList[index], descripterTiles[(int)tiles[randomTile].x, (int)tiles[randomTile].y], tiles[randomTile]))
            {
                tiles.RemoveAt(randomTile); // remove tile from potential tile list
                descripterTiles[(int)tiles[randomTile].x, (int)tiles[randomTile].y] = objectList[index]; // set new tile

                GameObject.Instantiate(tempObject,
                    new Vector3((tiles[randomTile].x - startTile.x) * scale - 5, 2.5f, (tiles[randomTile].y - startTile.y) * scale), 
                    Quaternion.identity);

                index++;
            }
            else { Debug.Log("Didn't work"); }
        //}
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
