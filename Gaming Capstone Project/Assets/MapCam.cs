using UnityEngine;

public class MapCam : MonoBehaviour
{
    public Camera[] cams;
    public GameObject textPrefab;
    public void Setup()
    {
        RoomManager rm = RoomManager.Instance;
        Vector3 midPoint = Vector3.zero;
        for(int i = 0; i < rm.spawnPoints.Count; i++)
        {
            Vector3 pt = rm.spawnPoints[i].transform.position;
            midPoint += pt;
            GameObject newObject = GameObject.Instantiate(textPrefab, new Vector3(pt.x, 10, pt.z), Quaternion.EulerAngles(new Vector3(Mathf.Deg2Rad * 90, 0, 0)));
            newObject.name = "RoomText" + i;
            newObject.GetComponent<TextMesh>().text = rm.rooms[i].roomName;
        }
        midPoint = midPoint / rm.spawnPoints.Count;
        float maxDistance = 0;
        Vector3 maxDistanceVector;
        float maxDistanceSize = 0;
        for (int i = 0; i < rm.spawnPoints.Count; i++)
        {
            Vector3 pt = rm.spawnPoints[i].transform.position;
            if (Vector3.Distance(pt, midPoint) > maxDistance)
            {
                maxDistance = Vector3.Distance(pt, midPoint);
                maxDistanceSize = rm.rooms[i].size * rm.rooms[i].scale;
                maxDistanceVector = pt;
            }
        }
        foreach (Camera cam in cams)
        {
            cam.orthographicSize = maxDistance + maxDistanceSize / 4;
        }
        transform.position = new Vector3(midPoint.x, 30, midPoint.z);
        Debug.Log("Mid Position: " + transform.position);

    }
}
