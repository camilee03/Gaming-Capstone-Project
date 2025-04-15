using System.Collections.Generic;
using System.Drawing;
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
        while (fringe.count > 0)
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
        while (fringe.count > 0 && currentCost < 100)
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
        DataStructures.PriorityQueue<(Vector3, List<Vector3>, int)> fringe = new();

        Vector3 currentState = pos1; // start from pos1

        // Create first list of paths that can be taken
        foreach (Vector3 successor in GetSuccessorsAvoidingTiles(currentState, tileScale))
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
        while (fringe._list.Count > 0 && currentCost < 50)
        {
            // pursue a new path
            node = fringe.Pop();
            currentState = node.pos;
            currentPath = node.path;
            currentCost = node.cost;

            // If have arrived at destination, set path and stop
            if (Vector3.Distance(pos2, currentState) < tileScale / 4)
            {
                solution = currentPath;
                return solution;
            }

            // Check if state is already expanded
            if (!closed.Contains(currentState))
            {
                closed.Add(currentState);

                foreach (Vector3 successor in GetSuccessorsAvoidingTiles(currentState, tileScale))
                {
                    List<Vector3> path = new(currentPath);
                    path.Add(successor);
                    fringe.Push((successor, path, currentCost + 1), (int)Vector3.Distance(successor, pos2) + currentCost + 1);
                }
            }
        }

        Debug.Log("No path found. Current cost = " + currentCost + " Current path length = " + currentPath.Count);
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

    static List<Vector3> GetSuccessorsAvoidingTiles(Vector3 parent, float scale)
    {
        GameObject[] listOfTiles = GameObject.FindGameObjectsWithTag("Tile");
        List<Vector3> successors = new List<Vector3>();
        bool[] hasSuccessor = new bool[4] { true, true, true, true };

        // check to see if any tiles collide with successors
        foreach (GameObject tile in listOfTiles) 
        {
            if (Vector3.Distance(tile.transform.position, parent + new Vector3(scale, 0, 0)) < scale / 4)
            {
                hasSuccessor[0] = false; continue;
            }
            if (Vector3.Distance(tile.transform.position, parent + new Vector3(-scale, 0, 0)) < scale / 4)
            {
                hasSuccessor[1] = false; continue;
            }
            if (Vector3.Distance(tile.transform.position, parent + new Vector3(0, 0, scale)) < scale / 4)
            {
                hasSuccessor[2] = false; continue;
            }
            if (Vector3.Distance(tile.transform.position, parent + new Vector3(0, 0, -scale)) < scale / 4)
            {
                hasSuccessor[3] = false; continue;
            }
        }

        //Debug.Log("Checking " + parent);

        if (hasSuccessor[0]) { successors.Add(parent + new Vector3(scale, 0, 0)); }
        if (hasSuccessor[1]) { successors.Add(parent + new Vector3(-scale, 0, 0)); }
        if (hasSuccessor[2]) { successors.Add(parent + new Vector3(0, 0, scale)); }
        if (hasSuccessor[3]) { successors.Add(parent + new Vector3(0, 0, -scale)); }

        Debug.Log("Num successors: " +  successors.Count);

        return successors;
    }
}
