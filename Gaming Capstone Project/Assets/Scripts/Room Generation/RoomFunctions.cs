using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomFunctions : ScriptableObject
{

    /// <summary> Takes in a Vector3 and returns the position on the grid </summary>
    public static (int x, int y) ConvertPosToTile(Vector3 pos, int direction, bool isWall, float scale)
    {
        float addition = 0;
        if (isWall) { addition = scale * 2; }

        (int x, int y) spawnLeft = ((int)((pos.x - addition) / scale), (int)(pos.z / scale));
        (int x, int y) spawnRight = ((int)((pos.x + addition) / scale), (int)(pos.z / scale));
        (int x, int y) spawnAbove = ((int)(pos.x / scale), (int)((pos.z - addition) / scale));
        (int x, int y) spawnBelow = ((int)(pos.x / scale), (int)((pos.z + addition) / scale));
        (int x, int y)[] spawnPositions = { spawnLeft, spawnAbove, spawnRight, spawnBelow };

        return spawnPositions[direction];
    }

    /// <summary> Takes in a grid position and finds out what direction its facing (mostly used for walls) </summary>
    public static Vector3 ConvertTileToPos(int x, int y, int direction, bool isWall, float scale)
    {
        float addition = 0;
        if (isWall) { addition = scale / 2; }
        Vector3 spawnLeft = new Vector3(x * scale + addition, 2.5f, y * scale);
        Vector3 spawnRight = new Vector3(x * scale - addition, 2.5f, y * scale);
        Vector3 spawnAbove = new Vector3(x * scale, 2.5f, y * scale + addition);
        Vector3 spawnBelow = new Vector3(x * scale, 2.5f, y * scale - addition);
        Vector3[] spawnPos = new Vector3[4] { spawnLeft, spawnAbove, spawnRight, spawnBelow };

        return spawnPos[direction];
    }

    /// <summary> Finds a root child based on the name to look for and the index it will be in </summary>
    public static List<GameObject> GetRootChild(GameObject parent, string name, int index, bool isRandom)
    {
        if (parent.transform.childCount <= index) return null;
        else if (parent.transform.GetChild(index).name == name) { return new List<GameObject> { parent }; }
        else if (isRandom)
        {
            int chooseRoom = Random.Range(0, parent.transform.childCount - 1);
            return GetRootChild(parent.transform.GetChild(chooseRoom).gameObject, name, index, true);
        }
        else
        {
            List<GameObject> rootChildren = new();
            for (int i = 0; i < parent.transform.childCount - 1; i++)
            {
                rootChildren.Add(GetRootChild(parent.transform.GetChild(i).gameObject, name, index, false)[0]);
            }
            return rootChildren;
        }
    }

    /// <summary> Iterates through tiles and see if current room collides with any of them </summary>
    public static bool CheckForCollisions(GameObject room, float scale)
    {
        List<GameObject> colliderList = GameObject.FindGameObjectsWithTag("Tile").ToList(); // list of tiles
        List<GameObject> roomList = GetRootChild(room, "TileParent", 1, false); // list of rooms in room
        //List<GameObject> hallwayList = GetRootChild(room, "Hallway", 2, false); // list of parents of hallways in room

        foreach (GameObject root in roomList) // iterates through rooms
        {
            Transform tiles = root.transform.GetChild(1); // gets tileParent
            int childNum = tiles.childCount;

            for (int i = 0; i < childNum; i++) // iterates through all room tiles
            {
                foreach (GameObject coll in colliderList) // iterates through all global tiles
                {
                    // Find the uppermost parent
                    GameObject parent = coll;
                    while(parent.transform.parent != null)
                    {
                        parent = parent.transform.parent.gameObject;
                    }

                    // Figure out if global and room tile collide
                    if (parent != room)
                    {
                        float distanceSquared = (coll.transform.position - tiles.GetChild(i).position).sqrMagnitude;
                        if (distanceSquared <= Mathf.Pow((2 * scale),2))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public static (float minX, float  minZ, float maxX, float maxZ) FindMapBoundaries()
    {
        float minX = 1000000;
        float minZ = 1000000;
        float maxX = -1000000;
        float maxZ = -1000000;

        float deviation = 100;
        foreach (Room room in RoomManager.Instance.rooms)
        {
            Transform t = room.parent.transform;
            foreach (Transform child in t.GetChild(0)) //sum the walls
            {
                Vector3 pos = child.position;
                if (pos.x < minX) minX = pos.x;
                if (pos.z < minZ) minZ = pos.z;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.x > maxZ) maxZ = pos.z;
            }
        }

        return (minX, minZ, maxX, maxZ);
    }

    public static Room FindRoomOfObject(GameObject obj)
    {
        Transform objectParent = obj.transform.parent;
        GameObject roomParent = null;
        if (objectParent != null) { roomParent = objectParent.parent.gameObject; }

        foreach (Room room in RoomManager.Instance.rooms)
        {
            if (roomParent == room.parent) { return room; }
        }

        return null;
    }

    public static List<Vector3> GetAllWallPositions()
    {
        List<Vector3> positions = new();
        
        foreach (Room room in RoomManager.Instance.rooms)
        {
            for (int i=0; i<room.wallParent.transform.childCount; i++)
            {
                positions.Add(RoundVector3(room.wallParent.transform.GetChild(i).position));
            }
        }

        return positions;
    }

    public static Vector3 RoundVector3(Vector3 vector)
    {
        return new Vector3((int)Mathf.CeilToInt(vector.x), 2.5f, (int)Mathf.CeilToInt(vector.z));
    }
}
