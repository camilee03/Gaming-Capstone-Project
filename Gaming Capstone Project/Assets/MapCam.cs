using UnityEngine;

public class MapCam : MonoBehaviour
{
    public Camera[] cams;
    public GameObject textPrefab;
    public void Setup()
    {
        (float minX, float minZ, float maxX, float maxZ) = RoomFunctions.FindMapBoundaries();
        Vector3 midPos = new Vector3((minX + maxX) / 2, transform.position.y, (minZ + maxZ) / 2);
        transform.position = midPos;
        foreach (Camera cam in cams)
            cam.orthographicSize = Mathf.Max(Mathf.Abs(maxX - minX), Mathf.Abs(maxZ - minZ)) * .6f;
        RoomManager rm = RoomManager.Instance;
        for(int i = 0; i < rm.spawnPoints.Count; i++)
        {
            Vector3 pt = rm.spawnPoints[i].transform.position;
            GameObject newObject = GameObject.Instantiate(textPrefab, new Vector3(pt.x, 10, pt.z), Quaternion.Euler(new Vector3(Mathf.Deg2Rad * 90, 0, 0)));
            newObject.name = "RoomText" + i;
            newObject.GetComponent<TextMesh>().text = "Room " + i;
        }
    }
}
