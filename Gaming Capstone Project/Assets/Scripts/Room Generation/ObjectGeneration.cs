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
                if ((doorPos - newObject.domains[0]).sqrMagnitude < 10) { collided = true; continue; }
            }

            if (!collided) { PlaceObjects(newObject.identifier, newObject.domains[0], scale, objectParent); }
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

        // ----- MRV: Select unassigned variable with the fewest available domain values -----
        Object newObject = unassigned.OrderBy(o => o.domains.Count).First();
        unassigned.Remove(newObject);

        // ----- LCV: Order the domain values by least constraining impact -----
        var orderedDomains = newObject.domains
            .OrderBy(domain => CountEliminatedValues(newObject, domain, unassigned))
            .ThenBy(domain => Random.value)
            .ToList();

        // Find a place for unassigned object
        foreach (Vector3 domain in orderedDomains) {

            // Check if can add the value
            if (CheckConstraints(assigned, domain, newObject))
            {
                // Save a copy of the current domain for backtracking.
                List<Vector3> originalDomains = new List<Vector3>(newObject.domains);
                newObject.domains = new List<Vector3>() { domain };
                assigned.Add(newObject);

                // Apply arc consistency to prune domains.
                if (ArcConsistency(unassigned))
                {
                    List<Object> result = RecursiveBacktracking(assigned, unassigned, numObjects);
                    if (result != null && result.Count == numObjects)
                        return result;
                }

                // Backtrack if no solution was found.
                assigned.Remove(newObject);
                newObject.domains = originalDomains;
            }

        }

        unassigned.Add(newObject);
        return bestSolution; // failed
    }

    // Helper: Count how many domain values would be eliminated in unassigned variables
    // if we assign 'domain' to the variable 'obj'.
    private int CountEliminatedValues(Object obj, Vector3 domain, List<Object> unassigned)
    {
        int eliminationCount = 0;
        // For every neighbor of the object (i.e., every unassigned variable)
        foreach (Object neighbor in unassigned)
        {
            foreach (Vector3 neighborValue in neighbor.domains)
            {
                // If the binary constraint between obj assigned this domain value and the neighbor's
                // domain value fails, it would eliminate that neighbor value.
                if (!CheckBinaryConstraints(obj, domain, neighbor, neighborValue))
                {
                    eliminationCount++;
                }
            }
        }
        return eliminationCount;
    }

    private bool CheckConstraints(List<Object> assigned, Vector3 domain, Object unassigned)
    {
        if (assigned.Count == 0) { return true; } // nothing to check against

        // Check for collisions
        foreach (Object obj in assigned)
        {
            // Different categories are allowed to overlap.
            if (obj.constraint != unassigned.constraint)
                continue;  

            foreach (Vector3 position in obj.domains)
            {
                if ((domain - position).sqrMagnitude < 10)
                    return false;
            }
        }

        // Check if two objects are close enough
        if (unassigned.properties.Contains(Properties.Paired))
        {
            // Look up the designated parent by its identifier.
            Object pair = assigned.FirstOrDefault(o => o.identifier.Equals(unassigned.pairedIdentifier));
            if (pair != null)
            {
                // Enforce the distance constraint.
                if (Vector3.Distance(domain, pair.domains[0]) > 30)
                    return false;
            }
        }

        return true;

    }

    // Performs Arc Consistency to the CSP
    private bool ArcConsistency(List<Object> objects)
    {
        // Build a queue of all arcs (ordered pairs) between distinct objects.
        Queue<(Object, Object)> arcs = new Queue<(Object, Object)>();
        for (int i = 0; i < objects.Count; i++)
        {
            for (int j = i + 1; j < objects.Count; j++)
            {
                // Enqueue both directions, if needed:
                arcs.Enqueue((objects[i], objects[j]));
                arcs.Enqueue((objects[j], objects[i]));
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
        List<Vector3> removalList = new();

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
        // Standard constraint: two objects of the same constraint should not occupy the same position.
        if ((value1 - value2).sqrMagnitude < 10 && obj1.constraint == obj2.constraint)
            return false;

        // Check the pairing constraint if applicable.
        // For instance, if obj1 is paired, then when comparing with its paired object, the positions must be within 50 units.
        if (obj1.properties.Contains(Properties.Paired) && obj2.identifier.Equals(obj1.pairedIdentifier))
        {
            return Vector3.Distance(value1, value2) <= 30;
        }
        // Also check the symmetric case.
        if (obj2.properties.Contains(Properties.Paired) && obj1.identifier.Equals(obj2.pairedIdentifier))
        {
            return Vector3.Distance(value1, value2) <= 30;
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
        Vector3 fallHeight = Vector3.up * 2;

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
                newObject = SpawnNetworkedObject(parent.transform, objects["box"], tilePos + fallHeight, Quaternion.Euler(-90, 0, randomRotationsY[i]));
                break;
            case 'C': // Chair
                newObject = SpawnNetworkedObject(parent.transform, objects["chair"], tilePos, Quaternion.Euler(-90, 0, randomRotationsY[i]));
                break;
            case 'c': // Coal
                int chooseRandomObject = Random.Range(0, 3);
                newObject = SpawnNetworkedObject(parent.transform, objects["coal" + chooseRandomObject], tilePos + fallHeight, Quaternion.identity);
                break;
            case 'e': // EnergyCore
                newObject = SpawnNetworkedObject(parent.transform, objects["energy core"], tilePos, Quaternion.identity);
                break;
            case 'F': // Furnace
                newObject = SpawnNetworkedObject(parent.transform, objects["furnace"], tilePos, Quaternion.Euler(-90, 0, randomRotationsY[i]));
                break;
            case 'f': // fan
                newObject = SpawnNetworkedObject(parent.transform, objects["fan"], tilePos + Vector3.up * 6, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;
            case 'L': // light
                newObject = SpawnNetworkedObject(parent.transform, objects["light"], tilePos + ceilingHeight, Quaternion.Euler(-90, 0, 0));
                break;
            case 'l': // lever
                if (wallDict.Keys.Contains(tilePos))
                {
                    newObject = SpawnNetworkedObject(parent.transform, objects["lever"], tilePos + disp * 0.5f + Vector3.up * 3, Quaternion.LookRotation(wallDict[tilePos].transform.right) * Quaternion.Euler(-90, 0, 90));
                }
                break;
            case 'P': // Poster
                if (wallDict.Keys.Contains(tilePos))
                {
                    newObject = SpawnNetworkedObject(parent.transform, objects["poster"], tilePos + disp * 0.5f + Vector3.up * 5, Quaternion.LookRotation(wallDict[tilePos].transform.right) * Quaternion.Euler(-90, 0, 90));
                }
                break;
            case 'p': // paper
                newObject = SpawnNetworkedObject(parent.transform, objects["paper"], tilePos + fallHeight, Quaternion.identity);
                break;
            case 'r': // radio
                newObject = SpawnNetworkedObject(parent.transform, objects["radio"], tilePos, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;
            case 's': // speaker
                if (wallDict.Keys.Contains(tilePos))
                {
                    newObject = SpawnNetworkedObject(parent.transform, objects["speaker"], tilePos + disp * 1.6f + Vector3.up * 4, Quaternion.LookRotation(wallDict[tilePos].transform.right) * Quaternion.Euler(-90, 0, 90));
                }
                break;
            case 't': // Table
                newObject = SpawnNetworkedObject(parent.transform, objects["table"], tilePos, Quaternion.Euler(-90, randomRotationsY[i], 0));
                newObject.transform.localScale /= 1.1f;
                break;
            case 'T': // DOS terminal
                newObject = SpawnNetworkedObject(parent.transform, objects["DOS terminal"], tilePos, Quaternion.Euler(-90, randomRotationsY[i], 0));
                newObject.transform.localScale /= 1.1f;
                break;
            case 'v': // vent
                if (wallDict.Keys.Contains(tilePos))
                {
                    newObject = SpawnNetworkedObject(parent.transform, objects["vent"], tilePos + disp * 0.5f + Vector3.up, Quaternion.LookRotation(wallDict[tilePos].transform.right) * Quaternion.Euler(-90, 0, 90));
                }
                break;
            case 'W': // Chute
                if (wallDict.Keys.Contains(tilePos))
                {
                    newObject = SpawnNetworkedObject(parent.transform, objects["chute"], tilePos + disp * 0.5f + Vector3.up * 5, Quaternion.LookRotation(wallDict[tilePos].transform.right) * Quaternion.Euler(-90, 0, 90));
                }
                break;
            case 'w': // Wires
                if (wallDict.Keys.Contains(tilePos))
                {
                    newObject = SpawnNetworkedObject(parent.transform, objects["wires"], tilePos + disp * 0.5f + Vector3.up * 5, Quaternion.LookRotation(wallDict[tilePos].transform.right) * Quaternion.Euler(-90, 0, 90));
                }
                break;
            case 'X': // food
                newObject = SpawnNetworkedObject(parent.transform, objects["food"], tilePos + fallHeight, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;
            case 'x': // clothes
                newObject = SpawnNetworkedObject(parent.transform, objects["glove"], tilePos + fallHeight, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;
            case 'z': // trash
                newObject = SpawnNetworkedObject(parent.transform, objects["trash"], tilePos + fallHeight, Quaternion.Euler(0, randomRotationsY[i], 0));
                break;

            default:
                Debug.Log($"{type} character not found");
                break;
        }


        if (newObject != null) { spawnedObjects.Add(newObject); }
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

        if (child != null)
        {
            instance = Instantiate(child, position, rotation);
            NetworkObject instanceNetworkObject = instance.GetComponent<NetworkObject>();
            if (instanceNetworkObject == null) { Debug.LogError(child.name + " needs a NetworkObject"); }
            instanceNetworkObject.Spawn(true);

            if (parent != null) { instanceNetworkObject.TrySetParent(parent); }
        }

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


