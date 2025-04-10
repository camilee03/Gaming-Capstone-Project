using DG.Tweening;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Xml.Serialization;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEngine.Rendering.DebugUI;


public class ObjectGeneration : NetworkBehaviour
{
    List<Vector3> tilePositions = new();
    List<Vector3> wallPositions = new();
    Dictionary<Vector3, Quaternion> wallRotations = new();

    char[,] descripterTiles; // what each tile has on it
    char[,] preDescripterTiles;
    GameObject roomParent;
    Vector3 firstTile;

    RoomGeneration roomGenerator;
    RoomTypeJSON roomTypeJSON;
    List<GameObject> spawnedObjects = new List<GameObject>();

    SerializableDict dictionary;
    Dictionary<string, GameObject> objects;

    GameObject roomObject;
    GameObject roomParentObject;

    enum ItemType { Box, Button, Lever, Light, Table, Chair, 
        Doors, BulletinBoard, Radio, Terminal, Fan, Wires, 
        Furnace, Coal, Vent, Food, WashingMachine, Clothes, Cabinet, 
        EnergyCore, Trash }

    enum TaskCategory { Pickup, Interactable, Attack }

    enum TaskType { Visual, Audio, Tactile, None }

    enum Constraints { None, Orientation, Wall, Ceiling, }

    private void Awake()
    {
        roomGenerator = GetComponent<RoomGeneration>();
        roomTypeJSON = GetComponent<RoomTypeJSON>();
        dictionary = GetComponent<SerializableDict>();
        objects = dictionary.dictionary;

        // set random seed if there is one
        if (roomGenerator.seed != -1) { Random.InitState(roomGenerator.seed); }
    }

    public void GenerationProcedure(Room room)
    {
        //CleanRoom(); // get rid of old objects and reset room

        // Figure out what objects to spawn
        List<string[]> objectList = roomTypeJSON.objectList;
        int randomList = Random.Range(0, objectList.Count - 1);
        string[] objectsToSpawn = objectList[randomList];
        foreach (string obj in objectsToSpawn)
        {
            Debug.Log(obj);
        }

        // Goes through room and spawns objects
        preDescripterTiles = room.objectLocations;
        roomParent = room.parent;
        firstTile = room.tileParent.transform.GetChild(0).position;
        roomObject = room.roomObject;
        roomParentObject = room.roomParentObject;

        // Create viable locations lists
        wallPositions = new();
        wallRotations = new();
        for (int i = 0; i < room.wallParent.transform.childCount - 1; i++)
        {
            wallPositions.Add(room.wallParent.transform.GetChild(i).position);
            wallRotations[room.wallParent.transform.GetChild(i).position] = room.wallParent.transform.GetChild(i).rotation;
        }
        tilePositions = new();
        for (int i = 0; i < room.tileParent.transform.childCount - 1; ++i)
        {
            tilePositions.Add(room.tileParent.transform.GetChild(i).position);
        }

        // Spawn objects
        room.objectParent = BacktrackingSearch(objectsToSpawn);
    }
    
    private GameObject BacktrackingSearch(string[] objectList)
    {
        int scale = 10;
        descripterTiles = (char[,])preDescripterTiles.Clone();
        spawnedObjects = new List<GameObject>();

        List<Object> unassignedObjects = new();

        // Spawn in a set number of new objects
        int numTilePositions = tilePositions.Count;
        int numWallPositions = wallPositions.Count;

        foreach (string identifier in objectList)
        {
            (Object newObject, int amount) = DetermineObjectNum(identifier.ToCharArray()[0]);

            for (int i=0; i<amount; i++)
            {
                if (newObject.constraint == Constraints.None && numTilePositions > 0) // check that can still place on tiles
                {
                    unassignedObjects.Add(newObject); 
                    numTilePositions--; 
                }
                if (newObject.constraint == Constraints.Wall && numWallPositions > 0) // check that can still places on walls
                { 
                    numWallPositions--;
                    unassignedObjects.Add(newObject);
                }
            }
        }

        List<Object> assignedObjects = RecursiveBacktracking(new List<Object>(), unassignedObjects, unassignedObjects.Count);


        // Spawn assigned objects
        GameObject objectParent = SpawnNetworkedObject(roomParent.transform, roomParentObject, Vector3.zero, Quaternion.identity);
        objectParent.name = "ObjectParent";

        foreach (Object newObject in assignedObjects)
        {
            PlaceObjects2(newObject.identifier, newObject.domains[0], scale, objectParent);
        }

        return objectParent;
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

    private void PlaceObjects2(char type, Vector3 tilePos, int scale, GameObject parent)
    {
        GameObject newObject = null;
        Vector3 ceilingHeight = Vector3.up * 12;

        switch (type)
        {
            case 'b': // bulletin board
                newObject = SpawnNetworkedObject(parent.transform, objects["bulletin board"], Vector3.zero, Quaternion.identity);
                newObject.transform.position = tilePos;
                newObject.transform.localRotation = wallRotations[tilePos];
                break;
            case 'C': // Chair
                newObject = SpawnNetworkedObject(parent.transform, objects["chair"], Vector3.zero, Quaternion.identity);
                newObject.transform.position = tilePos;
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 'c': // Chute
                newObject = SpawnNetworkedObject(parent.transform, objects["chute"], Vector3.zero, Quaternion.identity);
                newObject.transform.localScale *= 2;
                newObject.transform.position = tilePos + Vector3.up * 5;
                newObject.transform.localRotation = wallRotations[tilePos];
                break;
            case 'f': // fan
                newObject = SpawnNetworkedObject(parent.transform, objects["fan"], Vector3.zero, Quaternion.identity);
                newObject.transform.position = tilePos + Vector3.up * 3;
                newObject.transform.localRotation = Quaternion.identity;
                break;
            case 'L': // light
                newObject = SpawnNetworkedObject(parent.transform, objects["light"], Vector3.zero, Quaternion.identity);
                newObject.transform.position = tilePos + ceilingHeight;
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 'l': // lever
                newObject = SpawnNetworkedObject(parent.transform, objects["lever"], Vector3.zero, Quaternion.identity);
                newObject.transform.position = tilePos + Vector3.up * 3;
                newObject.transform.localRotation = wallRotations[tilePos];
                break;
            case 's': // speaker
                newObject = SpawnNetworkedObject(parent.transform, objects["speaker"], Vector3.zero, Quaternion.identity);
                newObject.transform.position = tilePos + Vector3.up * 3;
                newObject.transform.localRotation = wallRotations[tilePos];
                break;
            case 'T': // Table
                newObject = SpawnNetworkedObject(parent.transform, objects["table"], Vector3.zero, Quaternion.identity);
                newObject.transform.localScale /= 1.1f;
                newObject.transform.position = tilePos;
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 't': // DOS terminal
                newObject = SpawnNetworkedObject(parent.transform, objects["DOS terminal"], Vector3.zero, Quaternion.identity);
                newObject.transform.localScale /= 1.1f;
                newObject.transform.position = tilePos;
                newObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 'v': // vent
                newObject = SpawnNetworkedObject(parent.transform, objects["vent"], Vector3.zero, Quaternion.identity);
                newObject.transform.position = tilePos + Vector3.up * 1;
                newObject.transform.localRotation = wallRotations[tilePos]; //+ Quaternion.Euler(-90, 0, 0)
                break;

            default: Debug.Log($"{type} character not found"); break;

        }

        //newObject.transform.position += firstTile;
        spawnedObjects.Add(newObject);
    }


    private (Object newObject, int numObjects) DetermineObjectNum(char identifier)
    {
        Object newObject = new Object();
        int amount = 0;

        switch (identifier)
        {
            // --  Scarce -- //

            case 't': // DOS terminal
                newObject = new Object() { identifier = identifier, domains = tilePositions, constraint = Constraints.None };
                amount = 1;
                break;

            // -- Plentiful -- //

            case 'L': // light
                newObject = new Object() { identifier = identifier, domains = tilePositions, constraint = Constraints.Ceiling };
                amount = Random.Range(2, 10);
                break;

            // -- Normal -- //

            case 'c': // Chute
            case 'l': // lever
            case 's': // speaker
            case 'v': // vent
                newObject = new Object() { identifier = identifier, domains = wallPositions, constraint = Constraints.Wall };
                amount = Random.Range(1, 5);
                break;
            case 'C': // Chair
            case 'f': // fan
            case 'T': // Table
                newObject = new Object() { identifier = identifier, domains = tilePositions, constraint = Constraints.None };
                amount = Random.Range(1, 5);
                break;


            default: Debug.Log($"{identifier} character not found"); break;

        }

        return (newObject, amount);
    }

    private void DetermineTheme() // determine task type and theme based on GPT
    {

    }




    private char[] CreateObjectList(int numTiles, RoomType roomType) 
    {
        if (2 * roomType.objects.Length > numTiles) { Debug.Log("ERROR: More objects than space"); }

        char[] roomObjectList = new char[numTiles-2];
        int wiggleRoom = Mathf.Max((numTiles - roomType.objects.Length * 2) - 5, 0);
        int index = 0;

        foreach (char obj in roomType.objects)
        {
            int numObjs = NumberOfObjectsToSpawn(obj, wiggleRoom);
            for (int i = 0; i < numObjs; i++)
            {
                roomObjectList[index++] = obj;
            }

        }

        return roomObjectList;
    }

    private int NumberOfObjectsToSpawn(char obj, int wiggleRoom)
    {
        switch (obj)
        {
            // Plentiful objects
            case 'x': // Clothes
            case 'c': // Coal
            case 'X': // Food
            case 'L': // Light
            case 'p': // Paper
                return 1;

            // Scarce objects
            case 'B': // Button
            case 'F': // Fan
            case 'f': // Furnace
            case 't': // Table
            case 'T': // Terminal
                return Mathf.Min(Random.Range(5, 5 + wiggleRoom), 10);

            // Default for objects without these properties
            default:
                return Mathf.Min(Random.Range(1, 1 + wiggleRoom), 5);
        }

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

    struct RoomType
    {
        public string name;
        public char[] objects;
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


