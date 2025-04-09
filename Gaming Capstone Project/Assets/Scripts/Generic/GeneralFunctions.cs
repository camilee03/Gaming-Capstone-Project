using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "GeneralFunctions", menuName = "Scriptable Objects/GeneralFunctions")]
public class GeneralFunctions : ScriptableObject
{
    public static Vector3 FindCenterPoint(GameObject parent)
    {
        float totalX = 0f;
        float totalY = 0f;
        float totalZ = 0f;
        for (int i=0; i < parent.transform.childCount; i++)
        {
            totalX += parent.transform.GetChild(i).transform.position.x;
            totalY += parent.transform.GetChild(i).transform.position.y;
            totalZ += parent.transform.GetChild(i).transform.position.z;
        }
        float centerX = totalX / parent.transform.childCount;
        float centerY = totalY / parent.transform.childCount;
        float centerZ = totalZ / parent.transform.childCount;

        return new Vector3(centerX, centerY, centerZ);
    }

    /// <summary> Finds shortest path between positions </summary>
    public static List<Vector3> FindShortestPath(Vector3 pos1, Vector3 pos2, float tileScale)
    {
        DataStructures.PriorityQueue<(Vector3, List<Vector3>, int)> fringe = new DataStructures.PriorityQueue<(Vector3, List<Vector3>, int)>();

        Vector3 currentState = pos1; // start from position of door1

        // Create first list of paths that can be taken
        foreach (Vector3 successor in GetSuccessors(currentState, tileScale))
        {
            List<Vector3> path = new List<Vector3>();
            path.Add(successor);
            int cost = 1;
            fringe.Push((successor, path, cost), (int)Vector3.Distance(successor, pos2) + cost);
        }

        List<Vector3> closed = new List<Vector3>();
        closed.Add(currentState);
        List<Vector3> solution = new List<Vector3>();
        List<Vector3> currentPath = new List<Vector3>();
        int currentCost = 0;
        (Vector3 pos, List<Vector3> path, int cost) node;

        //While fringe set is not empty
        while (!fringe.IsEmpty)
        {
            node = fringe.Pop();
            currentState = node.pos;
            currentPath = node.path;
            currentCost = node.cost;

            // If have arrived at destination, set path and stop
            if (Vector3.Distance(pos2, currentState) < tileScale/2)
            {
                solution = currentPath;
                return solution;
            }

            // Check if state is already expanded
            if (!closed.Contains(currentState))
            {
                closed.Add(currentState);

                foreach (Vector3 successor in GetSuccessors(currentState, tileScale))
                {
                    List<Vector3> path = new(currentPath);
                    path.Add(successor);
                    fringe.Push((successor, path, currentCost + 1), (int)Vector3.Distance(successor, pos2) + currentCost + 1); // change to heuristic later
                }
            }
        }

        Debug.Log("No path found");
        return null;

    }

    public static List<Vector3> FindShortestPathExcludingCollisions(Vector3 pos1, Vector3 pos2, float tileScale)
    {
        DataStructures.PriorityQueue<(Vector3, List<Vector3>, int)> fringe = new();

        Vector3 currentState = pos1; // start from pos1

        // Create first list of paths that can be taken
        foreach (Vector3 successor in GetSuccessorsWithoutCollisions(currentState, tileScale))
        {
            List<Vector3> path = new();
            path.Add(successor);
            int cost = 1;
            fringe.Push((successor, path, cost), (int)Vector3.Distance(successor, pos2) + cost);
        }

        List<Vector3> closed = new List<Vector3>();
        closed.Add(currentState);
        List<Vector3> solution = new List<Vector3>();
        List<Vector3> currentPath = new List<Vector3>();
        int currentCost = 1;
        (Vector3 pos, List<Vector3> path, int cost) node;

        //While fringe set is not empty
        while (!fringe.IsEmpty && currentCost < 100)
        {
            // pursue a new path
            node = fringe.Pop();
            currentState = node.pos;
            currentPath = node.path;
            currentCost = node.cost;

            // If have arrived at destination, set path and stop
            if (Vector3.Distance(pos2, currentState) < tileScale / 2)
            {
                solution = currentPath;
                return solution;
            }

            // Check if state is already expanded
            if (!closed.Contains(currentState))
            {
                closed.Add(currentState);

                foreach (Vector3 successor in GetSuccessorsWithoutCollisions(currentState, tileScale))
                {
                    List<Vector3> path = new(currentPath);
                    path.Add(successor);
                    fringe.Push((successor, path, currentCost + 1), (int)Vector3.Distance(successor, pos2) + currentCost + 1); 
                }
            }
        }

        Debug.Log("No path found");
        return null;

    }

    public static List<Vector3> FindShortestAvoidingTiles(Vector3 pos1, Vector3 pos2, float tileScale)
    {
        //Debug.Log($"Position 1: {pos1} Position 2: {pos2} Tile scale: {tileScale}");

        DataStructures.PriorityQueue<(Vector3, int)> fringe = new();
        Dictionary<Vector3, Vector3> parentMap = new();
        GameObject[] listOfTiles = GameObject.FindGameObjectsWithTag("Tile");

        Vector3 currentState = pos1; // start from pos1
        List<Vector3> successors = GetSuccessorsAvoidingTiles(currentState, tileScale, listOfTiles);

        if (successors.Count == 0) { Debug.Log("Path Blocked"); return null; }

        // Create first list of paths that can be taken
        foreach (Vector3 successor in successors)
        {
            parentMap[successor] = currentState; 
            float dx = Mathf.Abs(successor.x - pos2.x);
            float dz = Mathf.Abs(successor.z - pos2.z); // Assuming y-axis is elevation
            int heuristic = (int)(dx + dz);

            fringe.Push((successor, 1), heuristic + 1);
        }

        HashSet<Vector3> closed = new();
        closed.Add(RoundVector3(currentState));
        (Vector3 pos, int cost) node;

        //While fringe set is not empty
        while (!fringe.IsEmpty)
        {
            // pursue a new path
            node = fringe.Pop();
            currentState = node.pos;

            // If have arrived at destination, set path and stop
            float distanceRemaining = (pos2 - currentState).sqrMagnitude;
            if (distanceRemaining <= (tileScale * tileScale) / 8.0f)
            {
                List<Vector3> path = new();
                Vector3 goal = currentState;
                while ((goal - pos1).sqrMagnitude > Mathf.Epsilon)
                {
                    path.Add(goal);
                    goal = parentMap[goal];
                }
                path.Reverse();
                return path;
            }
            
            // If the distance has over shot
            if (node.cost > 50) { Debug.Log($"Node cost: {node.cost}, Successor: {currentState}, Distance remaining: {distanceRemaining}"); break; }

            // Check if state is already expanded
            if (!closed.Contains(RoundVector3(currentState)))
            {
                closed.Add(RoundVector3(currentState));
                successors = GetSuccessorsAvoidingTiles(currentState, tileScale, listOfTiles);

                if (successors.Count == 0) { Debug.Log("Error, no successors found." + closed.Count); }

                foreach (Vector3 successor in successors)
                {
                    if (!parentMap.ContainsKey(successor)) { parentMap[successor] = currentState; }
                    float dx = Mathf.Abs(successor.x - pos2.x);
                    float dz = Mathf.Abs(successor.z - pos2.z); // Assuming y-axis is elevation
                    int heuristic = (int) (dx + dz);
                    fringe.Push((successor, node.cost + 1), heuristic + node.cost + 1);
                }
            }
        }

        Debug.Log($"Expanded nodes: {closed.Count}");

        return null;

    }

    /// <summary> Finds paths next to current path </summary>
    static List<Vector3> GetSuccessors(Vector3 parent, float scale)
    {
        List<Vector3> successors = new List<Vector3>();
        successors.Add(parent + new Vector3(scale, 0, 0));
        successors.Add(parent + new Vector3(-scale, 0, 0));
        successors.Add(parent + new Vector3(0, 0, scale));
        successors.Add(parent + new Vector3(0, 0, -scale));

        return successors;
    }

    /// <summary> Finds paths next to current path </summary>
    static List<Vector3> GetSuccessorsWithoutCollisions(Vector3 parent, float scale)
    {
        List<Vector3> successors = new();

        Collider[] collisions = new Collider[2];
        int numCollisons = Physics.OverlapBoxNonAlloc(parent + new Vector3(scale, 0, 0), Vector3.one * scale / 4, collisions);
        if (numCollisons == 0) { successors.Add(parent + new Vector3(scale, 0, 0)); }
        else { Debug.Log(parent + " has no successor right"); }

        collisions = new Collider[2];
        numCollisons = Physics.OverlapBoxNonAlloc(parent + new Vector3(scale, 0, 0), Vector3.one * scale / 4, collisions);
        if (numCollisons == 0) { successors.Add(parent + new Vector3(-scale, 0, 0)); }
        else { Debug.Log(parent + " has no successor left"); }

        collisions = new Collider[2];
        numCollisons = Physics.OverlapBoxNonAlloc(parent + new Vector3(scale, 0, 0), Vector3.one * scale / 4, collisions);
        if (numCollisons == 0) { successors.Add(parent + new Vector3(0, 0, scale)); }
        else { Debug.Log(parent + " has no successor down"); }

        collisions = new Collider[2];
        numCollisons = Physics.OverlapBoxNonAlloc(parent + new Vector3(scale, 0, 0), Vector3.one * scale / 4, collisions);
        if (numCollisons == 0) { successors.Add(parent + new Vector3(0, 0, -scale)); }
        else { Debug.Log(parent + " has no successor up"); }

        return successors;
    }

    static List<Vector3> GetSuccessorsAvoidingTiles(Vector3 parent, float scale, GameObject[] listOfTiles)
    {
        List<Vector3> successors = new List<Vector3>();
        bool[] hasSuccessor = new bool[4] { true, true, true, true };
        Vector3[] directions = { new Vector3(scale, 0, 0),
            new Vector3(-scale, 0, 0),
            new Vector3(0, 0, scale),
            new Vector3(0, 0, -scale)
        };

        // Find list of tiles
        HashSet<Vector3> tilePositions = new();
        foreach (GameObject tile in listOfTiles)
        {
            tilePositions.Add(RoundVector3(tile.transform.position));
        }

        // check to see if any tiles collide with successors
        foreach (Vector3 direction in directions)
        {
            Vector3 successor = parent + direction;
            if (!tilePositions.Contains(RoundVector3(successor)))
            {
                successors.Add(successor);
            }
        }

        return successors;
    }

    static Vector3 RoundVector3(Vector3 v)
    {
        int precision = 2;
        float factor = Mathf.Pow(10, precision);
        return new Vector3(
            Mathf.Round(v.x * factor) / factor,
            Mathf.Round(v.y * factor) / factor,
            Mathf.Round(v.z * factor) / factor
        );
    }
}
