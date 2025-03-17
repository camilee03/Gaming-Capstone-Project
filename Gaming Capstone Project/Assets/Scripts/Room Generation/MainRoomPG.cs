using Mono.Cecil;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using static TileSpawner;

public class MainRoomPG : MonoBehaviour
{
    public GameObject URCorner;
    public GameObject ULCorner;
    public GameObject BRCorner;
    public GameObject BLCorner;

    GameObject[] corners;

    GameObject upEdge;
    GameObject rightEdge;
    GameObject downEdge;
    GameObject leftEdge;
    GameObject[] edges;

    public Vector3 position;
    public Quaternion rotation;
    public GameObject parent;

    ObjectPool<GameObject>[] pools;

    Vector3[] newCorners;
    Vector3[] oldCorners;


    // -- NOTE : right edge is +180degrees and moved of left edge --> same for up and down -- //
    // in the same way, you can copy the middle rows based on an edge

    // don't base the spawn on moving tile because it could be wrong: base it on the last tile placed


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        upEdge = new GameObject("Up Edge");
        rightEdge = new GameObject("Right Edge");
        downEdge = new GameObject("Down Edge");
        leftEdge = new GameObject("Left Edge");
        edges = new GameObject[4] { upEdge, downEdge, rightEdge, leftEdge };

        pools = GetComponent<TileSpawner>().pools;

        newCorners = new Vector3[4] { URCorner.transform.position, ULCorner.transform.position, BRCorner.transform.position, BLCorner.transform.position };
        corners = new GameObject[4] { URCorner, ULCorner, BRCorner, BLCorner };

        CreateEdge(newCorners[0], newCorners[1], 0, 2, 0);
        CreateEdge(newCorners[3], newCorners[2], 0, 2, 1);
        CreateEdge(newCorners[0], newCorners[2], 2, 0, 2);
        CreateEdge(newCorners[3], newCorners[1], 2, 0, 3);

        oldCorners = newCorners;
    }



    // Update is called once per frame
    void Update()
    {
        // resets corner positions
        newCorners = new Vector3[4] { URCorner.transform.position, ULCorner.transform.position, BRCorner.transform.position, BLCorner.transform.position };

        Vector3[] directions = new Vector3[3] { Vector3.right, Vector3.up, Vector3.forward };


        (int, int)[] pairs = new (int, int)[4] { (0, 1), (2, 3), (0, 2), (1, 3) };
        (int, int)[] pairsofpairs = new (int, int)[4] { (2, 3), (0, 1), (1, 3), (0, 2) };
        int index = 0;

        foreach ((int one, int two) in pairs)
        {
            // determines if take the x or z value
            int loc = 2;
            if (index > 1) { loc = 0; }


            if (newCorners[one][loc] != oldCorners[one][loc] || newCorners[two][loc] != oldCorners[two][loc])
            {
                // make sure edge and corner positions remain the same

                Vector3 delta1 = directions[loc] * (newCorners[one][loc] - oldCorners[one][loc]);
                Vector3 delta2 = directions[loc] * (newCorners[two][loc] - oldCorners[two][loc]);

                edges[index].transform.position += delta1 += delta2;
                newCorners[one] = corners[one].transform.position += delta2;
                newCorners[two] = corners[two].transform.position += delta1;


                // add new edge pieces if needed

                (int oneone, int twotwo) = pairsofpairs[index];

                int direction = 2 * ((one + oneone - 1) % 2);
                int direction2 = 2 * ((two + twotwo - 1) % 2);
                int opDirection = 2 * ((one + oneone) % 2);
                int opDirection2 = 2 * ((two + twotwo) % 2);

                CreateEdge(newCorners[one], newCorners[oneone], direction, opDirection, index); 
                CreateEdge(newCorners[two], newCorners[twotwo], direction2, opDirection2, index);
            }

            index++;
        }


        oldCorners = newCorners;
    }

    void CreateEdge(Vector3 corner1, Vector3 corner2, int direction, int oppositeDir, int poolNum)
    {
        Vector3 distance = corner1 - corner2;
        Vector3[] directions = new Vector3[3] {Vector3.right, Vector3.up, Vector3.forward};
        Vector3[] rotations = new Vector3[4] { Vector3.zero, Vector3.up * 180, Vector3.up * 90, Vector3.up * -90 };

        List<GameObject> edge = new();

        float offset = Mathf.Abs(distance[direction]) / 10;

        if (edges[poolNum].transform.childCount < offset - 1)
        {
            Vector3 startPos = Vector3.Min(corner1, corner2);
            int dir = 1;
            if (startPos != corner1) { dir = -1; }

            for (int i = 1; i < offset - edges[poolNum].transform.childCount; i++)
            {
                // set variables
                GameObject newObject;
                position = corner1 + dir * directions[direction] * 10 * i;
                rotation = Quaternion.Euler(rotations[poolNum]);
                parent = edges[poolNum];

                // get pool object
                pools[poolNum].Get(out newObject);
            }
        }

        else if (edges[poolNum].transform.childCount >= offset && offset > 2) // returning errors with double releases
        {
            for (int i = edges[poolNum].transform.childCount - 1; i > offset; i--)
            {
                pools[poolNum].Release(edges[poolNum].transform.GetChild(i).gameObject);
            }
        }
    }

}
