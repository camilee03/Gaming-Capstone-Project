using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    GameObject player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.position = player.transform.position;
        gameObject.transform.rotation = player.transform.rotation;
    }
}
