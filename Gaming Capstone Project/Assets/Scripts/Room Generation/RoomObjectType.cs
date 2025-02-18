using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomObjectType : MonoBehaviour
{
    List<Vector2> tiles; // tiles in the room that are untaken (maybe sort by constraints?)
    List<Vector2> preTiles;
    char[,] descripterTiles; // what each tile has on it
    char[,] preDescripterTiles;
    bool spawnObjects = true;

    RoomGeneration roomGenerator;
    Vector2 startTile;
    List<GameObject> spawnedObjects = new List<GameObject>();

    [SerializeField] GameObject objectParent;
    [SerializeField] GameObject[] objects;

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
        var room = roomGenerator.roomsInfo[0];
            preTiles = room.finalRoomTiles;
            preDescripterTiles = room.room;
            startTile = room.startTile;

        if (roomGenerator.finishedProcedure && spawnObjects) {
            BacktrackingSearchTest();
        }
    }

    private bool SpawnObject(char newTileCode, char oldTileCode, Vector2 location) // determines if objects can be spawned according to constraints
    {
        char leftTile = descripterTiles[(int)location.x - 1, (int)location.y];
        char rightTile = descripterTiles[(int)location.x + 1, (int)location.y];
        char upTile = descripterTiles[(int)location.x, (int)location.y - 1];
        char downTile = descripterTiles[(int)location.x, (int)location.y + 1];

        switch (newTileCode)
        {
            case 'd': // door
                if (oldTileCode == 'w') { return true; } break;
            case 't': // table
                if (oldTileCode == 'f' && leftTile == 'f' && rightTile == 'f' && upTile == 'f' && downTile == 'f') 
                    { return true; } break;
            case 'b': // bulletin board
                if (oldTileCode == 'w') { return true; } break;
            case 'T': // DOS terminal
                Debug.Log(oldTileCode);
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
        CleanRoom(); // get rid of old objects and reset room

        char[] objectList = new char[8] { 't', 'F', 'b', 'b', 'l', 'l', 'l', 'T'};
        int scale = 10;
        int randomTile = 0; char oldCode;

        spawnObjects = false;
        for (int i = 0; i < objectList.Length; i++)
        {
            bool canSpawn = false;
            randomTile = Random.Range(0, tiles.Count-1); // find new unassigned tile
            int iterations = 1; // if iterations goes through, no acceptable tile

            //-- Genreation Start --//
            while (!canSpawn && iterations != tiles.Count)
            {
                if (randomTile < tiles.Count - 1) { randomTile++; } else { randomTile = 0; }

                if (tiles[randomTile] != null)
                {
                    oldCode = descripterTiles[(int)tiles[randomTile].x, (int)tiles[randomTile].y]; // char code of current tile
                    canSpawn = SpawnObject(objectList[i], oldCode, tiles[randomTile]); // check spawn conditions

                    iterations++;
                }
            }
            //-- Generation Done --//

            if (canSpawn)
            {
                tiles.Remove(tiles[randomTile]); // remove changed tile from potential tile list
                descripterTiles[(int)tiles[randomTile].x, (int)tiles[randomTile].y] = objectList[i]; // set new tile

                PlaceObjects(objectList[i], randomTile, scale);
            }
        }
    }

    private void CleanRoom()
    {
        tiles = new List<Vector2>(preTiles); // reset what tiles are taken
        descripterTiles = (char[,])preDescripterTiles.Clone();

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

    private void PlaceObjects(char type, int tilePos, int scale)
    {
        GameObject newObject = null;

        switch (type)
        {
            case 'b': // bulletin board
                newObject = GameObject.Instantiate(objects[0],
                    new Vector3((tiles[tilePos].x - startTile.x) * scale, 2.5f, 
                        (tiles[tilePos].y - (startTile.y + 1)) * scale),
                    Quaternion.identity, objectParent.transform);
                break;
            case 'T': // Table
                newObject = GameObject.Instantiate(objects[1],
                    new Vector3((tiles[tilePos].x - startTile.x) * scale, 2.5f * 1.5f,
                        (tiles[tilePos].y - (startTile.y + 1)) * scale),
                    Quaternion.identity, objectParent.transform);
                newObject.transform.localScale *= 2;
                break;
            case 'c': // Chair
                newObject = GameObject.Instantiate(objects[5],
                    new Vector3((tiles[tilePos].x - startTile.x) * scale, 2.5f * 1.5f,
                        (tiles[tilePos].y - (startTile.y + 1)) * scale),
                    Quaternion.identity, objectParent.transform);
                newObject.transform.localScale *= 2;
                break;
            case 't': // DOS terminal
                newObject = GameObject.Instantiate(objects[3],
                    new Vector3((tiles[tilePos].x - startTile.x) * scale, 3f, 
                        (tiles[tilePos].y - (startTile.y + 1)) * scale),
                    new Quaternion(0, 1, 0, 1), objectParent.transform);
                break;
            case 'l': // light
                newObject = GameObject.Instantiate(objects[4],
                    new Vector3((tiles[tilePos].x - startTile.x) * scale, 5f, 
                        (tiles[tilePos].y - (startTile.y + 1)) * scale),
                    new Quaternion(1, 0, 0, 1), objectParent.transform);
                break;
            case 'F': // fan
                newObject = GameObject.Instantiate(objects[2],
                    new Vector3((tiles[tilePos].x - startTile.x) * scale, 5.5f, 
                        (tiles[tilePos].y - (startTile.y + 1)) * scale),
                    Quaternion.identity, objectParent.transform);
                break;
            default:
                break;

        }

        spawnedObjects.Add(newObject);
    }


    // Each object has a value x,y that determines where it is on the graph
    // For every x,y certain functions must be applied to make each scenario true
    // Trim borders to make it easier?

    // Step 1: Choose random point inside room that hasn't been assigned yet (maybe keep a list for simplicity?)
    // Step 2: Choose an object to place in point that fufills constraints (decide on ordering --> maybe place big items first?)
    // Step 3: Repeat until no objects are left

    // Some global constraints: objects must be inside room / on wall, objects cannot be assigned to same place as another
}
