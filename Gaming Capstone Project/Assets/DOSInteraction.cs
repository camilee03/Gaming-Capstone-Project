using UnityEngine;

public class DOSInteraction : MonoBehaviour
{

    public Transform cameraMovementPoint;
    public CameraMovement CameraMovementScript;
    public GameObject camera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        camera = Camera.main.gameObject;
        CameraMovementScript = camera.GetComponent<CameraMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartInteraction()
    {
        //Tween Camera to point
    }
}
