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
<<<<<<< Updated upstream
            Vector3 pt = rm.spawnPoints[i].transform.position;
=======
            Vector3 pt = rm.spawnPoints[i];
            midPoint += pt;
>>>>>>> Stashed changes
            GameObject newObject = GameObject.Instantiate(textPrefab, new Vector3(pt.x, 10, pt.z), Quaternion.EulerAngles(new Vector3(Mathf.Deg2Rad * 90, 0, 0)));
            newObject.name = "RoomText" + i;
            newObject.GetComponent<TextMesh>().text = rm.rooms[i].roomName;
        }
        midPoint = midPoint / rm.spawnPoints.Count;
        float maxDistance = 0;
        Vector3 maxDistanceVector;
        for (int i = 0; i < rm.spawnPoints.Count; i++)
        {
            Vector3 pt = rm.spawnPoints[i];
            if (Vector3.Distance(pt, midPoint) > maxDistance)
            {
                maxDistance = Vector3.Distance(pt, midPoint);
                maxDistanceVector = pt;
            }
        }
        foreach (Camera cam in cams)
        {
            cam.orthographicSize = maxDistance * 1.1f;
        }
        transform.position = new Vector3(midPoint.x, 30, midPoint.z);
        Debug.Log("Mid Position: " + transform.position);

    }
}
