using DG.Tweening;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
    [Header("Domains")]
    List<Vector3> tilePositions = new();
    List<Vector3> wallPositions = new();
    Dictionary<Vector3, Quaternion> wallRotations = new();

    [Header("Existing Room")]
    char[,] descripterTiles; // what each tile has on it
    char[,] preDescripterTiles;
    GameObject roomParent;

    [Header("External Files")]
    RoomGeneration roomGenerator;
    RoomTypeJSON roomTypeJSON;
    SerializableDict dictionary;
    Dictionary<string, GameObject> objects;

    [Header("Spawning")]
    List<GameObject> spawnedObjects = new List<GameObject>();
    GameObject roomObject;
    GameObject roomParentObject;

    enum Constraints { None, Orientation, Wall, Ceiling, }
    enum Properties { None, Paired }

    private void Awake()
    {
        roomGenerator = GetComponent<RoomGeneration>();
        roomTypeJSON = GetComponent<RoomTypeJSON>();
        dictionary = GetComponent<SerializableDict>();
        objects = dictionary.dictionary;
    }

    // -- Generation Start -- //
    public void GenerationProcedure(Room room)
    {
        //CleanRoom(); // get rid of old objects and reset room

        // Get room specific variables
        preDescripterTiles = room.objectLocations;
        roomParent = room.parent;
        roomObject = room.roomObject;
        roomParentObject = room.roomParentObject;

        // Figure out what objects to spawn
        var roomTypeList = roomTypeJSON.rooms.room;
        int randomList = Random.Range(0, roomTypeList.Length - 1);
        string[] objectsToSpawn = roomTypeList[randomList].objects;
        room.roomName = roomTypeList[randomList].name;

        // Create viable locations lists
        wallPositions = new();
        wallRotations = new();
        for (int i = 0; i < room.wallParent.transform.childCount - 1; i++)
        {
            Transform newWall = room.wallParent.transform.GetChild(i);
            if (newWall.gameObject.name.StartsWith('W'))
            {
                wallPositions.Add(newWall.position);
                wallRotations[newWall.position] = newWall.rotation;
            }
            else { Debug.Log("DOOR"); }
        }
        tilePositions = new();
        for (int i = 0; i < room.tileParent.transform.childCount - 1; ++i)
        {
            tilePositions.Add(room.tileParent.transform.GetChild(i).position);
        }

        // Spawn objects
        room.objectParent = BacktrackingSearch(objectsToSpawn);
    }
    

    // -- Search Problem -- //
    private GameObject BacktrackingSearch(string[] oldObjectList)
    {
        // Set up initial variables
        int scale = 10;
        descripterTiles = (char[,])preDescripterTiles.Clone();
        spawnedObjects = new List<GameObject>();
        List<Object> unassignedObjects = new();

        // Add more objects based on available tile space
        // (leaves empty spaces if some will be on the walls, but that's intentional)
        char[] objectList = CreateObjectList(tilePositions.Count, oldObjectList);

        // Create objects & make sure there is enough space for all of them
        int numTilePositions = tilePositions.Count;
        int numWallPositions = wallPositions.Count;

        foreach (char identifier in objectList)
        {
            Object newObject = CreateObject(identifier);
            if (newObject.domains == null) { continue; }

            else if (newObject.constraint == Constraints.None && numTilePositions > 0) // check that can still place on tiles
            {
                unassignedObjects.Add(newObject);
                numTilePositions--;
            }
            else if (newObject.constraint == Constraints.Wall && numWallPositions > 0) // check that can still places on walls
            {
                numWallPositions--;
                unassignedObjects.Add(newObject);
            }
            else { unassignedObjects.Add(newObject); }
        }

        if (numWallPositions <= 0 || numTilePositions <= 0) { Debug.Log("Not enough space to spawn"); }


        // Spawn assigned objects
        List<Object> assignedObjects = RecursiveBacktracking(new List<Object>(), unassignedObjects, unassignedObjects.Count);
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
        // no more objects to assign
        if (assigned.Count == numObjects) { return assigned; } 

        // find new unassigned object
        Object newObject = unassigned[0];
        List<Vector3> domains = newObject.domains;

        // Find a place for unassigned object
        bool constaintsSatisfied = false;
        while (!constaintsSatisfied) {

            // Find new possible position
            if (domains.Count == 0) { Debug.LogError("Ran out of possible positions"); }
            int randomDomain = Random.Range(0, domains.Count-1);
            Vector3 domain = domains[randomDomain];
            domains.Remove(domain);

            if (CheckConstraints(assigned, domain, newObject)) // if constraints hold up
            {
                newObject.domains = new List<Vector3>() { domain };
                assigned.Add(newObject);
                unassigned.RemoveAt(0);

                List<Object> result = RecursiveBacktracking(assigned, unassigned, numObjects);

                if (result.Count == numObjects) { return result; }
                else { assigned.Remove(newObject); unassigned.Insert(0, newObject); }
            }
        }

        return null; // failed
    }

    private bool CheckConstraints(List<Object> assigned, Vector3 domain, Object unassigned)
    {
        if (assigned.Count == 0) { return true; }
        if (unassigned.constraint == Constraints.Ceiling) { return true; }

        bool hasPairing = unassigned.properties.Contains(Properties.Paired);
        bool constraintsHold = !hasPairing;

        foreach (Object obj in assigned)
        {
            if (obj.constraint == Constraints.Ceiling) { continue; }

            // Check that no assigned spot has the same pos as current
            if (obj.domains.Contains(domain)) { return false; }

            // Check pairing
            if (hasPairing && obj.identifier.Equals(unassigned.pairedIdentifier))
            {
                if (Vector3.Distance(domain, obj.domains[0]) <= 30)
                {
                    constraintsHold = true;
                }
            }
        }

        return constraintsHold;
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


    // -- Object Creation -- //
    private void PlaceObjects2(char type, Vector3 tilePos, int scale, GameObject parent)
    {
        GameObject newObject = null;
        Vector3 ceilingHeight = Vector3.up * 12;

        Vector3[] displacementVector = new Vector3[4] {Vector3.left, Vector3.up, Vector3.right, Vector3.down };
        Vector3 disp = Vector3.zero;
        if (wallRotations.Keys.Contains(tilePos)) { disp = displacementVector[FindWallDirection(wallRotations[tilePos], tilePos, roomParent)]; }

        float[] randomRotationsY = new float[4] { 90, 0, 180, -90 };
        int i = Random.Range(0, 4);

        switch (type)
        {
            case 'B': // button
                newObject = SpawnNetworkedObject(parent.transform, objects["button"], tilePos + disp, wallRotations[tilePos]);
                break;
            case 'b': // box
                newObject = SpawnNetworkedObject(parent.transform, objects["box"], tilePos + Vector3.up * 5, Quaternion.identity);
                break;
            case 'C': // Chair
                newObject = SpawnNetworkedObject(parent.transform, objects["chair"], tilePos, Quaternion.Euler(-90, randomRotationsY[i], 0));
                break;
            case 'c': // Coal
                int chooseRandomObject = Random.Range(0, 3);
                newObject = SpawnNetworkedObject(parent.transform, objects["coal" + chooseRandomObject], tilePos, Quaternion.identity);
                break;
            case 'e': // EnergyCore
                newObject = SpawnNetworkedObject(parent.transform, objects["energy core"], tilePos, Quaternion.identity);
                break;
            case 'F': // Furnace
                newObject = SpawnNetworkedObject(parent.transform, objects["furnace"], tilePos, Quaternion.Euler(-90, randomRotationsY[i], 0));
                break;
            case 'f': // fan
                newObject = SpawnNetworkedObject(parent.transform, objects["fan"], tilePos + Vector3.up * 10, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;
            case 'L': // light
                newObject = SpawnNetworkedObject(parent.transform, objects["light"], tilePos + ceilingHeight, Quaternion.Euler(-90, 0, 0));
                break;
            case 'l': // lever
                newObject = SpawnNetworkedObject(parent.transform, objects["lever"], tilePos + disp + Vector3.up * 3, wallRotations[tilePos]);
                break;
            case 'p': // paper
                newObject = SpawnNetworkedObject(parent.transform, objects["paper"], tilePos, Quaternion.identity);
                break;
            case 'r': // radio
                newObject = SpawnNetworkedObject(parent.transform, objects["radio"], tilePos, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;
            case 's': // speaker
                newObject = SpawnNetworkedObject(parent.transform, objects["speaker"], tilePos + disp + Vector3.up * 3, wallRotations[tilePos]);
                break;
            case 'T': // Table
                newObject = SpawnNetworkedObject(parent.transform, objects["table"], tilePos, Quaternion.Euler(-90, randomRotationsY[i], 0));
                newObject.transform.localScale /= 1.1f;
                break;
            case 't': // DOS terminal
                newObject = SpawnNetworkedObject(parent.transform, objects["DOS terminal"], tilePos, Quaternion.Euler(-90, randomRotationsY[i], 0));
                newObject.transform.localScale /= 1.1f;
                break;
            case 'v': // vent
                newObject = SpawnNetworkedObject(parent.transform, objects["vent"], tilePos + disp * 0.5f + Vector3.up, wallRotations[tilePos]);
                break;
            case 'W': // Chute
                newObject = SpawnNetworkedObject(parent.transform, objects["chute"], tilePos + disp, wallRotations[tilePos]);
                break;
            case 'w': // Wires
                newObject = SpawnNetworkedObject(parent.transform, objects["wires"], tilePos + disp, wallRotations[tilePos]);
                break;
            case 'X': // food
                newObject = SpawnNetworkedObject(parent.transform, objects["food"], tilePos, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;
            case 'x': // clothes
                newObject = SpawnNetworkedObject(parent.transform, objects["glove"], tilePos, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;
            case 'z': // trash
                newObject = SpawnNetworkedObject(parent.transform, objects["trash"], tilePos, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;

            default:
                Debug.Log($"{type} character not found");
                break;
        }


        spawnedObjects.Add(newObject);
    }

    int FindWallDirection(Quaternion rotation, Vector3 position, GameObject room)
    {
        if (rotation.eulerAngles.y % 180 == 90)
        {
            // Direction 0 (left)
            if (position.x < room.transform.position.x) { return 0; }

            // Direction 2 (right)
            return 2;
        }
        else
        {
            // Direction 1 (below)
            if (position.z < room.transform.position.z) { return 1; }

            // Direction 3 (above)
            return 3;
        }
    }

    private Object CreateObject(char identifier)
    {
        Object newObject = new Object();

        // Figure out constraints
        switch (identifier)
        {
            // -- No Constraints -- //
            case 'B': // Button
            case 'f': // Furnace
            case 'F': // Fan
            case 'T': // Terminal
            case 'e': // Energy Core
            case 'b': // Box
            case 'p': // Paper
            case 'X': // Food
            case 'c': // Coal
            case 'x': // Clothes
            case 'C': // Chair
            case 't': // Table
            case 'r': // Radio
                newObject = new Object() { identifier = identifier, domains = tilePositions, constraint = Constraints.None };
                break;

            // -- Wall Constraints -- //
            case 'W': // Chute
            case 'l': // Lever
            case 's': // Speaker
            case 'v': // Vent
            case 'w': // Wires
                newObject = new Object() { identifier = identifier, domains = wallPositions, constraint = Constraints.Wall };
                break;

            // -- Ceiling Constraints -- //
            case 'L': // Light
                newObject = new Object() { identifier = identifier, domains = tilePositions, constraint = Constraints.Ceiling };
                break;

            // -- Default -- //
            default:
                Debug.Log($"{identifier} character not found");
                break;
        }

        // Figure out properties
        Dictionary<char, char> pairedValues = new() { { 'C', 't'}, { 'c', 'f' }, { 'f', 'c' }, { 'X', 't' }, { 'W', 'C' } };

        newObject.properties = new();

        if (pairedValues.Keys.Contains(identifier))
        {
            newObject.properties.Add(Properties.Paired);
            newObject.pairedIdentifier = pairedValues[identifier];
        }

        if (newObject.properties.Count == 0)
        {
            newObject.properties.Add(Properties.None);
        }

        return newObject;
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


    // -- Object Type Creation -- //
    private char[] CreateObjectList(int numTiles, string[] objects) 
    {
        if (2 * objects.Length > numTiles) { Debug.Log("ERROR: More objects than space"); }

        char[] roomObjectList = new char[numTiles-1];
        int wiggleRoom = Mathf.Max((numTiles - objects.Length * 2) - 5, 0);
        int index = 0;

        foreach (string obj in objects)
        {
            int numObjs = NumberOfObjectsToSpawn(obj.ToCharArray()[0], wiggleRoom);
            for (int i = 0; i < numObjs; i++)
            {
                if (obj == "") { continue; }
                if (index >= roomObjectList.Length) { Debug.Log("Too many spawned objects. Breaking."); continue; }
                roomObjectList[index++] = obj.ToCharArray()[0];
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
            case 'p': // Paper
                return Mathf.Min(Random.Range(5, 5 + wiggleRoom), 10);

            case 'L': // Light
                return Random.Range(5, 10);

            // Scarce objects
            case 'B': // Button
            case 'F': // Fan
            case 'f': // Furnace
            case 't': // Table
            case 'T': // Terminal
            case 'e': // Energy Core
                return 1;

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
        public List<Properties> properties;
        public char pairedIdentifier;
    };
}


