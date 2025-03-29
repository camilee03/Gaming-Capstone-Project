using DG.Tweening;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEngine.Rendering.DebugUI;


public class RoomObjectType : MonoBehaviour
{
    List<Vector2> tiles; // tiles in the room that are untaken 
    List<Vector2> preTiles;

    List<Vector3> tilePositions = new();
    List<Vector3> wallPositions = new();


    char[,] descripterTiles; // what each tile has on it
    char[,] preDescripterTiles;
    bool spawnObjects = true;
    GameObject roomParent;
    Vector3 firstTile;

    RoomGeneration roomGenerator;
    List<GameObject> spawnedObjects = new List<GameObject>();

    SerializableDict dictionary;
    Dictionary<string, GameObject> objects;

    public char[] objectsRoom0;
    public char[] objectsRoom1;
    public char[] objectsRoom2;
    public char[] objectsRoom3;
    public char[] objectsRoom4;
    List<char[]> objectsInRooms;

    char[] objectIdentifiers = new char[] { 'b', 'C', 'c', 'f', 'L', 'l', 's', 'T', 't', 'v' };


    enum ItemType { Box, Button, Lever, Light, Table, Chair, 
        Doors, BulletinBoard, Radio, Terminal, Fan, Wires, 
        Furnace, Coal, Vent, Food, WashingMachine, Clothes, Cabinet, 
        EnergyCore, Trash }

    enum TaskCategory { Pickup, Interactable, Attack }

    enum TaskType { Visual, Audio, Tactile, None }

    enum Constraints { None, Orientation, Wall, Ceiling, }

    public void ObjectGeneration()
    {
        roomGenerator = GetComponent<RoomGeneration>();
        dictionary = GetComponent<SerializableDict>();
        objects = dictionary.dictionary;

        // temp list for debugging
        objectsInRooms = new List<char[]> { objectsRoom0, objectsRoom1, objectsRoom2, objectsRoom3, objectsRoom4 };

        // set random seed if there is one
        if (roomGenerator.seed != -1) { Random.InitState(roomGenerator.seed); }

        if (spawnObjects)
        {
            CleanRoom(); // get rid of old objects and reset room

            // Goes through each room and spawns objects for each
            int index = 0;
            foreach (var room in roomGenerator.rooms)
            {
                preTiles = room.finalRoomTiles;
                preDescripterTiles = room.objectLocations;
                roomParent = room.parent;
                firstTile = room.tileParent.transform.GetChild(0).position;


                // Create viable locations lists
                for (int i = 0; i < room.wallParent.transform.childCount-1; i++)
                {
                    wallPositions.Add(room.wallParent.transform.GetChild(i).position);
                }
                for (int i = 0; i < room.tileParent.transform.childCount-1; ++i)
                {
                    tilePositions.Add(room.tileParent.transform.GetChild(i).position);
                }

                BacktrackingSearch(objectsInRooms[index]);
                index++;
            }
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
            case 't': // table
            case 'T': // terminal
                if (oldTileCode == 'f' && leftTile == 'f' && rightTile == 'f' && upTile == 'f' && downTile == 'f') 
                    { return true; } break;
            case 'b': // bulletin board
                if (oldTileCode == 'w') { return true; } break;
            default:
                if (oldTileCode == 'f') { return true; }
                break;

        }
        return false;
    }
    private void BacktrackingSearch(char[] objectList)
    {
        int scale = 10;
        descripterTiles = (char[,])preDescripterTiles.Clone();
        spawnedObjects = new List<GameObject>();


        List<Object> unassignedObjects = new();


        foreach (char identifier in objectList)
        {
            Object newObject = new Object() { identifier = identifier, domains = tilePositions, constraint = Constraints.None };
            unassignedObjects.Add(newObject);
        }

        List<Object> assignedObjects = RecursiveBacktracking(new List<Object>(), unassignedObjects, unassignedObjects.Count);


        // Spawn assigned objects
        GameObject objectParent = new GameObject("ObjectParent");
        objectParent.transform.parent = roomParent.transform;

        foreach (Object newObject in assignedObjects)
        {
            PlaceObjects2(newObject.identifier, newObject.domains[0], scale, objectParent);
        }

    }

    private List<Object> RecursiveBacktracking(List<Object> assigned, List<Object> unassigned, int numObjects)
    {
        if (assigned.Count == numObjects) { return assigned; }

        int randomTile = Random.Range(0, unassigned.Count-1); // find new unassigned tile (change)
        List<Vector3> domains = unassigned[randomTile].domains;

        bool constaintsSatisfied = false;

        while (!constaintsSatisfied) {

            // Find new possible position
            int randomDomain = Random.Range(0, domains.Count-1);
            Vector3 domain = domains[randomDomain];
            domains.Remove(domain);

            if (CheckConstraints(assigned, domain, 0)) // if constraints hold up
            {
                Object newObject = unassigned[randomTile];
                newObject.domains = new List<Vector3>() { domain };
                assigned.Add(newObject);

                List<Object> result = RecursiveBacktracking(assigned, unassigned, numObjects);

                if (result.Count == numObjects) { return result; }
                else { assigned.Remove(newObject); }
            }
        }

        return null; // failed
    }

    private bool CheckConstraints(List<Object> assigned, Vector3 domain, int type)
    {
        switch (type)
        {
            case 0:
                foreach (Object obj in assigned)
                {
                    if (obj.domains.Contains(domain)) { return false; }
                }
                return true;
            default: return false;
        }
    }


    private void ArcConsistency(List<Object> objects)
    {
        foreach(Object obj in objects)
        {
            foreach(Vector3 domain in obj.domains)
            {
                // if two objects have the same constraints, check that there is one position that doesn't equal this position for each object



                // check other constraints here



                // remove if constraints are not met
            }

            // if domain == null, arc consistency failed
        }
    }

    public void CleanRoom()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            GameObject.Destroy(obj);
        }
        spawnedObjects = new List<GameObject>();
    }

    private void PrepareRoom()
    {
        tiles = new List<Vector2>(preTiles); // reset what tiles are taken
        descripterTiles = (char[,])preDescripterTiles.Clone();

        spawnedObjects = new List<GameObject>();
    }

   
    private void PlaceObjects(char type, int tilePos, int scale, GameObject parent)
    {
        GameObject newObject = null;

        switch (type)
        {
            case 'b': // bulletin board
                newObject = GameObject.Instantiate(objects["bulletin board"], parent.transform);
                newObject.transform.localPosition = new Vector3(tiles[tilePos].x * scale, 2.5f, (tiles[tilePos].y - 1) * scale);
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'C': // Chair
                newObject = GameObject.Instantiate(objects["chair"], parent.transform);
                newObject.transform.localScale *= 2;
                newObject.transform.localPosition = new Vector3(tiles[tilePos].x * scale, 2.5f * 1.5f, (tiles[tilePos].y - 1) * scale);
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'c': // Chute
                newObject = GameObject.Instantiate(objects["chute"], parent.transform);
                newObject.transform.localScale *= 2;
                newObject.transform.localPosition = new Vector3(tiles[tilePos].x * scale, 2.5f * 1.5f, (tiles[tilePos].y - 1) * scale);
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'F': // fan
                newObject = GameObject.Instantiate(objects["fan"], parent.transform);
                newObject.transform.localPosition = new Vector3(tiles[tilePos].x * scale, 5.5f, (tiles[tilePos].y - 1) * scale);
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'L': // light
                newObject = GameObject.Instantiate(objects["light"], parent.transform);
                newObject.transform.localPosition = new Vector3(tiles[tilePos].x * scale, 15f, (tiles[tilePos].y - 1) * scale);
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 'l': // lever
                newObject = GameObject.Instantiate(objects["lever"], parent.transform);
                newObject.transform.localPosition = new Vector3(tiles[tilePos].x * scale, 5.5f, (tiles[tilePos].y - 1) * scale);
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 's': // speaker
                newObject = GameObject.Instantiate(objects["speaker"], parent.transform);
                newObject.transform.localPosition = new Vector3(tiles[tilePos].x * scale, 5.5f, (tiles[tilePos].y - 1) * scale);
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'T': // Table
                newObject = GameObject.Instantiate(objects["table"], parent.transform);
                newObject.transform.localScale *= 2;
                newObject.transform.localPosition = new Vector3(tiles[tilePos].x * scale, 2.5f * 1.5f, (tiles[tilePos].y - 1) * scale);
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 't': // DOS terminal
                newObject = GameObject.Instantiate(objects["DOS terminal"], parent.transform);
                newObject.transform.localPosition = new Vector3(tiles[tilePos].x * scale, 3f, (tiles[tilePos].y - 1) * scale);
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 'v': // vent
                newObject = GameObject.Instantiate(objects["vent"], parent.transform);
                newObject.transform.localPosition = new Vector3(tiles[tilePos].x * scale, 3f, (tiles[tilePos].y - 1) * scale);
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;

            default: Debug.Log("No such character found"); break;

        }

        //newObject.transform.position += firstTile;
        spawnedObjects.Add(newObject);
    }

    private void PlaceObjects2(char type, Vector3 tilePos, int scale, GameObject parent)
    {
        GameObject newObject = null;

        switch (type)
        {
            case 'b': // bulletin board
                newObject = GameObject.Instantiate(objects["bulletin board"], parent.transform);
                newObject.transform.position = tilePos;
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'C': // Chair
                newObject = GameObject.Instantiate(objects["chair"], parent.transform);
                newObject.transform.localScale *= 2;
                newObject.transform.position = tilePos + Vector3.up * 10;
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'c': // Chute
                newObject = GameObject.Instantiate(objects["chute"], parent.transform);
                newObject.transform.localScale *= 2;
                newObject.transform.position = tilePos + Vector3.up * 10;
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'F': // fan
                newObject = GameObject.Instantiate(objects["fan"], parent.transform);
                newObject.transform.position = tilePos + Vector3.up * 3;
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'L': // light
                newObject = GameObject.Instantiate(objects["light"], parent.transform);
                newObject.transform.position = tilePos + Vector3.up * 14;
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 'l': // lever
                newObject = GameObject.Instantiate(objects["lever"], parent.transform);
                newObject.transform.position = tilePos + Vector3.up * 3;
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 's': // speaker
                newObject = GameObject.Instantiate(objects["speaker"], parent.transform);
                newObject.transform.position = tilePos + Vector3.up * 3;
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'T': // Table
                newObject = GameObject.Instantiate(objects["table"], parent.transform);
                newObject.transform.localScale *= 2;
                newObject.transform.position = tilePos + Vector3.up * 10;
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 't': // DOS terminal
                newObject = GameObject.Instantiate(objects["DOS terminal"], parent.transform);
                newObject.transform.position = tilePos + Vector3.up * 1;
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 'v': // vent
                newObject = GameObject.Instantiate(objects["vent"], parent.transform);
                newObject.transform.position = tilePos + Vector3.up * 1;
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;

            default: Debug.Log("No such character found"); break;

        }

        //newObject.transform.position += firstTile;
        spawnedObjects.Add(newObject);
    }


    private void DetermineTheme() // determine task type and theme based on GPT
    {

    }

    private void DetermineTask() // determine tasks based on GPT model
    {

    }

    // Each object has a value x,y that determines where it is on the graph
    // For every x,y certain functions must be applied to make each scenario true
    // Trim borders to make it easier?

    // Step 1: Choose random point inside room that hasn't been assigned yet (maybe keep a list for simplicity?)
    // Step 2: Choose an object to place in point that fufills constraints (decide on ordering --> maybe place big items first?)
    // Step 3: Repeat until no objects are left

    // Some global constraints: objects must be inside room / on wall, objects cannot be assigned to same place as another

    struct Object
    {
        public char identifier;
        public List<Vector3> domains; // all the locations this object can be
        public Constraints constraint;
    };
}


