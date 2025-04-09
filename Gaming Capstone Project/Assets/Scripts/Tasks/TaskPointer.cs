using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class TaskPointer : MonoBehaviour
{
    Vector3 taskPosition = new Vector3(-32.9243469f, 2.5f, 7.50069332f);
    RectTransform taskPointer;
    Image pointerImage;

    private void Start()
    {
        taskPointer = GetComponent<RectTransform>();
        pointerImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (taskPointer != null) { UpdateArrowLocation(); }
    }

    private void UpdateArrowLocation()
    {
        // Update camera position
        Vector3 finalPosition = taskPosition;

        // Update location vector points to
        Vector3 pointerDirection = finalPosition - Camera.main.transform.position; // vector from camera to object

        Debug.Log(pointerDirection);
        float angle = Vector3.Angle(Camera.main.transform.forward, pointerDirection) - 90;

        taskPointer.rotation = Quaternion.LookRotation(pointerDirection, Camera.main.transform.up);

        // Point arrow at target
        //taskPointer.localEulerAngles = new Vector3(0, 0, angle);

        // Check location of arrow
        Vector3 targetPosScreenPoint = Camera.main.WorldToScreenPoint(taskPosition);
        int borderSize = 100;
        bool offscreen = targetPosScreenPoint.x <= borderSize || targetPosScreenPoint.x >= Screen.width - borderSize ||
            targetPosScreenPoint.z <= borderSize || targetPosScreenPoint.z >= Screen.height - borderSize;

        // Move arrow based on location
        //if (offscreen) { BorderOutline(targetPosScreenPoint, borderSize); }
        //else { PointAtTarget(targetPosScreenPoint); }
    }

    void BorderOutline(Vector3 target, int borderSize)
    {
        Vector3 borderPosition = target;

        // Set in border if not already
        if (borderPosition.x <= borderSize) { borderPosition.x = borderSize; } 
        if (borderPosition.x >= Screen.width - borderSize) {  borderPosition.x = Screen.width - borderSize; }
        if (borderPosition.y <= borderSize) { borderPosition.y = borderSize; }
        if (borderPosition.y >= Screen.height - borderSize) { borderPosition.y = Screen.height - borderSize; }

        // Set pointer location based on vector
        Vector3 pointerWorldPos = Camera.main.ScreenToWorldPoint(borderPosition);
        taskPointer.position = pointerWorldPos;
        taskPointer.localPosition = new Vector3(taskPointer.localPosition.x, taskPointer.localPosition.y, 0);
    }

    void PointAtTarget(Vector3 target)
    {
        Vector3 pointerWorldPos = Camera.main.ScreenToWorldPoint(target);
        taskPointer.position = pointerWorldPos;
        taskPointer.localPosition = new Vector3(taskPointer.localPosition.x, taskPointer.localPosition.y, 0);
    }

    public void SetTarget(Vector3 targetPosition)
    {
        taskPosition = targetPosition;
        gameObject.SetActive(true);
    }

    public void StopTarget()
    {
        gameObject.SetActive(false);
    }

}
