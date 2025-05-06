using UnityEngine;
using Unity.Netcode;

public class MapCam : NetworkBehaviour
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
            if (IsOwner)
            {
                if (DebugGen.Instance.doDebug) { Debug.Log("Setting up Room Text"); }
                if (IsServer) { setUpRoomTextClientRpc(rm.rooms[i].roomName, pt, i); }
                else { setUpRoomTextServerRpc(rm.rooms[i].roomName, pt, i); }
            }
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
        if (IsOwner)
        {
            if (DebugGen.Instance.doDebug) Debug.Log("Setting Up Cams");
            if (IsServer) { setUpCamClientRpc(maxDistance, maxDistanceSize, midPoint); }
            else { setUpCamServerRpc(maxDistance, maxDistanceSize, midPoint); }
        }
    }
    [ClientRpc]
    public void setUpRoomTextClientRpc(string text, Vector3 point, int index)
    {
        setUpRoomText(text, point, index);
    }
    [ServerRpc]
    public void setUpRoomTextServerRpc(string text, Vector3 point, int index)
    {
        setUpRoomTextClientRpc(text, point, index);
    }
    void setUpRoomText(string text, Vector3 point, int index)
    {
        GameObject newObject = GameObject.Instantiate(textPrefab, new Vector3(point.x, 10, point.z), Quaternion.EulerAngles(new Vector3(Mathf.Deg2Rad * 90, 0, 0)));
        newObject.name = "RoomText" + index;
        newObject.GetComponent<TextMesh>().text = text;
    }

    [ClientRpc]
    public void setUpCamClientRpc(float maxDistance, float maxDistanceSize, Vector3 midPoint)
    {
        setUpCam(maxDistance, maxDistanceSize, midPoint);
    }
    [ServerRpc]
    public void setUpCamServerRpc(float maxDistance, float maxDistanceSize, Vector3 midPoint)
    {
        setUpCamClientRpc(maxDistance, maxDistanceSize, midPoint);
    }
    void setUpCam(float maxDistance, float maxDistanceSize, Vector3 midPoint)
    {
        foreach (Camera cam in cams)
        {
            cam.orthographicSize = maxDistance + maxDistanceSize / 4;
        }
        transform.position = new Vector3(midPoint.x, 30, midPoint.z);
        if (DebugGen.Instance.doDebug) Debug.Log("Mid Position: " + transform.position);
    }
}
