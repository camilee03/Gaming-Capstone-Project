using DG.Tweening;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.WindowsRuntime;
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
    List<Vector3> doorPositions = new();
    Dictionary<Vector3, GameObject> wallDict = new();

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
    List<Object> bestSolution = new List<Object>();

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
        wallDict = new();
        for (int i = 0; i < room.wallParent.transform.childCount - 1; i++)
        {
            Transform newWall = room.wallParent.transform.GetChild(i);
            if (newWall.gameObject.name.StartsWith('W'))
            {
                wallPositions.Add(newWall.position);
                Debug.Log(newWall.position);
                wallDict[RoomFunctions.RoundVector3(newWall.position)] = newWall.gameObject;
            }
            else { doorPositions.Add(newWall.position); }
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
            if (identifier == '\0' || identifier == ' ') { continue; }

            Object newObject = CreateObject(identifier);
            if (newObject == null) { continue; }
            if (newObject.domains == wallPositions) { Debug.Log("HEREWALL"); }
            if (newObject.domains == tilePositions) { Debug.Log("HERETILE"); }

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
            if (newObject.domains.Count == 0) { continue; }
            bool collided = false;
            foreach (Vector3 doorPos in doorPositions)
            {
                if (Vector3.Distance(doorPos, newObject.domains[0]) < 1) { collided = true; continue; }
            }
            if (collided) { continue; }

            PlaceObjects(newObject.identifier, newObject.domains[0], scale, objectParent);
        }

        return objectParent;
    }

    private List<Object> RecursiveBacktracking(List<Object> assigned, List<Object> unassigned, int numObjects)
    {
        // Update best solution
        if (assigned.Count > bestSolution.Count)
        {
            bestSolution = new List<Object>(assigned);
        }

        // no more objects to assign
        if (assigned.Count == numObjects) { return assigned; }
        if (unassigned.Count == 0) { return bestSolution; }

        // find new unassigned object
        Object newObject = unassigned[0];
        List<Vector3> domains = newObject.domains;

        // Find a place for unassigned object
        while (domains.Count > 0) {

            // Find new possible position
            int randomDomain = Random.Range(0, domains.Count-1);
            Vector3 domain = domains[randomDomain];
            domains.Remove(domain);

            // Check if can add the value
            if (CheckConstraints(assigned, domain, newObject))
            {
                newObject.domains = new List<Vector3>() { domain };
                assigned.Add(newObject);
                unassigned.RemoveAt(0);

                // Apply arc consistency to prune domains.
                if (ArcConsistency(unassigned))
                {
                    List<Object> result = RecursiveBacktracking(assigned, unassigned, numObjects);
                    if (result != null && result.Count == numObjects)
                        return result;
                }

                // Backtrack if no solution was found.
                assigned.Remove(newObject);
                unassigned.Insert(0, newObject);
            }

        }

        return bestSolution; // failed
    }

    private bool CheckConstraints(List<Object> assigned, Vector3 domain, Object unassigned)
    {
        if (assigned.Count == 0) { return true; } // nothing to check against
        if (unassigned.constraint == Constraints.Ceiling) { return true; } // only lights

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
                if (Vector3.Distance(domain, obj.domains[0]) <= 50)
                {
                    constraintsHold = true;
                }
            }
        }

        return constraintsHold;
    }

    // Assumes your custom object class has at least:
    //   List<Vector3> domains;
    //   a constraint field of type Constraints (e.g., Constraints.Ceiling)
    //   an identifier field (e.g., a string or number)
    //   a pairedIdentifier field (to match a pair)
    //   a properties collection (which may contain Properties.Paired)
    private bool ArcConsistency(List<Object> objects)
    {
        // Build a queue of all arcs (ordered pairs) between distinct objects.
        Queue<(Object, Object)> arcs = new Queue<(Object, Object)>();
        for (int i = 0; i < objects.Count; i++)
        {
            for (int j = i; j < objects.Count; j++)
            {
                if (i != j)
                    arcs.Enqueue((objects[i], objects[j]));
            }
        }

        // Process the arcs
        while (arcs.Count > 0)
        {
            var (xi, xj) = arcs.Dequeue();
            if (Revise(xi, xj)) // If any domain value was removed
            {
                if (xi.domains.Count == 0)
                {
                    Debug.Log("Arc consistency failed: object " + xi.identifier + " has no valid positions left.");
                    return false;
                }
                // Add all arcs (xk, xi) where xk is any neighbor (other object) other than xj.
                foreach (var xk in objects)
                {
                    if (xk != xi && xk != xj)
                        arcs.Enqueue((xk, xi));
                }
            }
        }
        return true;
    }

    private bool Revise(Object xi, Object xj)
    {
        bool revised = false;
        // Collect values to remove so we can iterate safely over xi.domains.
        List<Vector3> removalList = new List<Vector3>();

        foreach (Vector3 vi in xi.domains)
        {
            bool hasSupport = false;
            // Look for at least one value vj in xj's domain that is consistent with vi.
            foreach (Vector3 vj in xj.domains)
            {
                if (CheckBinaryConstraints(xi, vi, xj, vj))
                {
                    hasSupport = true;
                    break;
                }
            }
            // If no valid support exists, mark vi for removal.
            if (!hasSupport)
                removalList.Add(vi);
        }
        // Remove all unsupported values.
        foreach (var val in removalList)
        {
            xi.domains.Remove(val);
            revised = true;
        }
        return revised;
    }

    private bool CheckBinaryConstraints(Object obj1, Vector3 value1, Object obj2, Vector3 value2)
    {
        // If either object is of type Ceiling (for lights, for example), we assume constraints do not apply.
        if (obj1.constraint == Constraints.Ceiling || obj2.constraint == Constraints.Ceiling)
            return true;

        // Standard constraint: two objects (non-ceiling) should not occupy the same position.
        if (value1.Equals(value2))
            return false;

        // Check the pairing constraint if applicable.
        // For instance, if obj1 is paired, then when comparing with its paired object, the positions must be within 50 units.
        if (obj1.properties.Contains(Properties.Paired) && obj2.identifier.Equals(obj1.pairedIdentifier))
        {
            return Vector3.Distance(value1, value2) <= 50;
        }
        // Also check the symmetric case.
        if (obj2.properties.Contains(Properties.Paired) && obj1.identifier.Equals(obj2.pairedIdentifier))
        {
            return Vector3.Distance(value1, value2) <= 50;
        }

        // Otherwise, there’s no additional binary constraint.
        return true;
    }



    // -- Object Creation -- //
    private void PlaceObjects(char type, Vector3 tilePos, int scale, GameObject parent)
    {
        tilePos = RoomFunctions.RoundVector3(tilePos);

        GameObject newObject = null;
        Vector3 ceilingHeight = Vector3.up * 12;

        Vector3[] displacementVector = new Vector3[4] {Vector3.left, Vector3.down, Vector3.right, Vector3.up };
        Vector3 disp = Vector3.zero;

        if (wallDict.Keys.Contains(tilePos)) { disp = displacementVector[FindWallDirection(tilePos)]; }

        float[] randomRotationsY = new float[4] { 90, 0, 180, -90 };
        int i = Random.Range(0, 4);

        switch (type)
        {
            case 'B': // button
                newObject = SpawnNetworkedObject(parent.transform, objects["button"], tilePos + disp, wallDict[tilePos].transform.rotation);
                break;
            case 'b': // box
                newObject = SpawnNetworkedObject(parent.transform, objects["box"], tilePos, Quaternion.Euler(-90, 0, randomRotationsY[i]));
                break;
            case 'C': // Chair
                newObject = SpawnNetworkedObject(parent.transform, objects["chair"], tilePos, Quaternion.Euler(-90, 0, randomRotationsY[i]));
                break;
            case 'c': // Coal
                int chooseRandomObject = Random.Range(0, 3);
                newObject = SpawnNetworkedObject(parent.transform, objects["coal" + chooseRandomObject], tilePos, Quaternion.identity);
                break;
            case 'e': // EnergyCore
                newObject = SpawnNetworkedObject(parent.transform, objects["energy core"], tilePos, Quaternion.identity);
                break;
            case 'F': // Furnace
                newObject = SpawnNetworkedObject(parent.transform, objects["furnace"], tilePos, Quaternion.Euler(-90, 0, randomRotationsY[i]));
                break;
            case 'f': // fan
                newObject = SpawnNetworkedObject(parent.transform, objects["fan"], tilePos + Vector3.up * 10, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;
            case 'L': // light
                newObject = SpawnNetworkedObject(parent.transform, objects["light"], tilePos + ceilingHeight, Quaternion.Euler(-90, 0, 0));
                break;
            case 'l': // lever
                newObject = SpawnNetworkedObject(parent.transform, objects["lever"], tilePos + disp + Vector3.up * 3, wallDict[tilePos].transform.rotation);
                break;
            case 'P': // Poster
                newObject = SpawnNetworkedObject(parent.transform, objects["poster"], tilePos + disp * 0.5f + Vector3.up * 4, Quaternion.LookRotation(wallDict[tilePos].transform.right) * Quaternion.Euler(-90, 0, 90));
                break;
            case 'p': // paper
                newObject = SpawnNetworkedObject(parent.transform, objects["paper"], tilePos, Quaternion.identity);
                break;
            case 'r': // radio
                newObject = SpawnNetworkedObject(parent.transform, objects["radio"], tilePos, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;
            case 's': // speaker
                newObject = SpawnNetworkedObject(parent.transform, objects["speaker"], tilePos + disp + Vector3.up * 3, Quaternion.LookRotation(wallDict[tilePos].transform.forward) * Quaternion.Euler(0, 90, 0));
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
                newObject = SpawnNetworkedObject(parent.transform, objects["vent"], tilePos + disp * 0.5f + Vector3.up, Quaternion.LookRotation(wallDict[tilePos].transform.right) * Quaternion.Euler(-90, 0, 90));
                break;
            case 'W': // Chute
                newObject = SpawnNetworkedObject(parent.transform, objects["chute"], tilePos + disp, wallDict[tilePos].transform.rotation);
                break;
            case 'w': // Wires
                newObject = SpawnNetworkedObject(parent.transform, objects["wires"], tilePos + disp, wallDict[tilePos].transform.rotation);
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

    int FindWallDirection(Vector3 position)
    {
        Vector3 doorDir = -wallDict[position].transform.up;

        int result = doorDir == Vector3.right ? 0 : 
            doorDir == Vector3.forward ? 1 :
            doorDir == Vector3.left ? 2 : 3;

        return result;
    }

    private Object CreateObject(char identifier)
    {
        Object newObject = null;

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
                newObject = new Object() { identifier = identifier, domains = new(tilePositions), constraint = Constraints.None };
                break;

            // -- Wall Constraints -- //
            case 'W': // Chute
            case 'l': // Lever
            case 'P': // Poster
            case 's': // Speaker
            case 'v': // Vent
            case 'w': // Wires
                newObject = new Object() { identifier = identifier, domains = new(wallPositions), constraint = Constraints.Wall };
                break;

            // -- Ceiling Constraints -- //
            case 'L': // Light
                newObject = new Object() { identifier = identifier, domains = new(tilePositions), constraint = Constraints.Ceiling };
                break;

            // -- Default -- //
            default:
                Debug.Log($"{identifier} character not found");
                return newObject;
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
        int wiggleRoom = 0;
        if (2 * objects.Length > numTiles) { Debug.Log("ERROR: More objects than space"); }
        else { wiggleRoom = Mathf.Max((numTiles - objects.Length * 2) - 5, 0); }

        char[] roomObjectList = new char[numTiles - 1];
        int index = 0;

        foreach (string obj in objects)
        {
            int numObjs = NumberOfObjectsToSpawn(obj.ToCharArray()[0], wiggleRoom, numTiles);
            for (int i = 0; i < numObjs; i++)
            {
                if (obj == "") { continue; }
                if (index >= numTiles-1) { Debug.Log("Too many spawned objects. Breaking."); break; }
                roomObjectList[index++] = obj.ToCharArray()[0];
            }
        }

        return roomObjectList;
    }

    private int NumberOfObjectsToSpawn(char obj, int wiggleRoom, int numTiles)
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
                return numTiles/3;

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
                return Mathf.Min(Random.Range(1, 1 + wiggleRoom), 4);
        }

    }

 

    class Object
    {
        public char identifier;
        public List<Vector3> domains; // all the locations this object can be
        public Constraints constraint;
        public List<Properties> properties;
        public char pairedIdentifier;
    };
}


