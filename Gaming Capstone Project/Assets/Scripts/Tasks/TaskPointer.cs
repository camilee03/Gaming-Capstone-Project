using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class TaskPointer : MonoBehaviour
{
    Vector3 taskPosition = new Vector3(0, 0, 0);
    RectTransform taskPointer;
    Image pointerImage;

    private void Start()
    {
        taskPointer = GetComponent<RectTransform>();
        pointerImage = GetComponent<Image>();

        StopTarget();
    }

    private void Update()
    {
        UpdateArrowLocation();
    }

    private void UpdateArrowLocation()
    {
        // Update vector direction
        Vector3 finalPosition = taskPosition;
        Vector3 pointerDirection = finalPosition - Camera.main.transform.position; // vector from camera to object

        // Find where to point at
        float angle = (Vector3.Angle(Camera.main.transform.forward, pointerDirection));
        Vector3 crossProduct = Vector3.Cross(Camera.main.transform.forward.normalized, pointerDirection.normalized);
        float dotProduct = Vector3.Dot(Camera.main.transform.forward.normalized, pointerDirection.normalized);
        if (crossProduct.y > 0) { angle = 360 - angle; }

        // Point arrow at target
        taskPointer.eulerAngles = new Vector3(0, 0, angle + 90);

        // Check location of arrow
        Vector3 targetPosScreenPoint = Camera.main.WorldToScreenPoint(taskPosition);
        int borderSize = 100;
        bool offscreen = dotProduct < 0 || (targetPosScreenPoint.x <= borderSize || targetPosScreenPoint.x >= Screen.width - borderSize ||
            targetPosScreenPoint.y <= borderSize || targetPosScreenPoint.y >= Screen.height - borderSize);

        // Move arrow based on location
        if (Vector3.Distance(Camera.main.transform.position, taskPosition) > 50) { taskPointer.localPosition = new Vector3(860, -440, 0); }
        else if (offscreen) { BorderOutline(targetPosScreenPoint, borderSize, dotProduct); }
        else { PointAtTarget(targetPosScreenPoint); }
    }

    void BorderOutline(Vector3 target, int borderSize, float dotProduct)
    {
        Vector3 borderPosition = target;
        borderPosition.z = 0;

        // Set in border if not already
        if (dotProduct > 0)
        {
            if (borderPosition.x <= borderSize) { borderPosition.x = borderSize; }
            if (borderPosition.x >= Screen.width - borderSize) { borderPosition.x = Screen.width - borderSize; }
            if (borderPosition.y >= Screen.height - borderSize) { borderPosition.y = borderSize; }
            if (borderPosition.y <= borderSize) { borderPosition.y = Screen.height - borderSize; }
        }
        else
        {
            if (borderPosition.x >= Screen.width - borderSize) { borderPosition.x = borderSize; }
            if (borderPosition.x <= borderSize) { borderPosition.x = Screen.width - borderSize; }
            if (borderPosition.y >= Screen.height - borderSize || borderPosition.y <= borderSize) { borderPosition.y =  Screen.height - borderSize; }
        }

        // Set pointer location based on vector
        Vector3 pointerWorldPos = Camera.main.ScreenToWorldPoint(borderPosition);
        taskPointer.position = borderPosition;
    }

    void PointAtTarget(Vector3 target)
    {
        taskPointer.localPosition = target;
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
