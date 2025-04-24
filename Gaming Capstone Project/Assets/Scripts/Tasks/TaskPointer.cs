using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class TaskPointer : MonoBehaviour
{
    Vector3 taskPosition = new Vector3(0, 0, 0);
    RectTransform taskPointer;
    RawImage pointerImage;
    public GameObject questPointer;

    private void Start()
    {
        taskPointer = GetComponent<RectTransform>();
        pointerImage = GetComponent<RawImage>();

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

        // Check location of arrow
        float dotProduct = Vector3.Dot(Camera.main.transform.forward.normalized, pointerDirection.normalized);
        Vector3 targetPosScreenPoint = Camera.main.WorldToScreenPoint(taskPosition);
        int borderSize = 100;
        bool offscreen = dotProduct < 0 || (targetPosScreenPoint.x <= borderSize || targetPosScreenPoint.x >= Screen.width - borderSize ||
            targetPosScreenPoint.y <= borderSize || targetPosScreenPoint.y >= Screen.height - borderSize);

        // Find where to point at
        float angle = (Vector3.Angle(Camera.main.transform.forward, pointerDirection));
        Vector3 crossProduct = Vector3.Cross(Camera.main.transform.forward.normalized, pointerDirection.normalized);
        if (crossProduct.y > 0) { angle = 360 - angle; }

        // Point arrow at target
        taskPointer.eulerAngles = new Vector3(0, 0, angle + 90);

        // Move arrow based on location
        if (Vector3.Distance(Camera.main.transform.position, taskPosition) > 50 || offscreen) { SetAtCorner(); }
        //else if (offscreen) { BorderOutline(targetPosScreenPoint, borderSize, dotProduct); }
        else { UpdateQuestArrowLocation(); pointerImage.enabled = false; }
    }

    private void UpdateQuestArrowLocation()
    {
        questPointer.SetActive(true);
        questPointer.transform.position = taskPosition + Vector3.up * 5;
        Vector3 lookDirection = questPointer.transform.position - Camera.main.transform.position;
        lookDirection.y = 0;
        questPointer.transform.rotation = Quaternion.LookRotation(lookDirection) * Quaternion.Euler(0, 0, -90);
    }

    void BorderOutline(Vector3 target, int borderSize, float dotProduct)
    {
        questPointer.SetActive(false);
        pointerImage.enabled = true;
        Vector3 borderPosition = target;
        borderPosition.z = 0;

        // Set in border if not already
        if (borderPosition.y > Screen.height - borderSize) { borderPosition.y = borderSize; }
        if (borderPosition.y < borderSize) { borderPosition.y = Screen.height - borderSize; }
        if (borderPosition.x <= Screen.width / 2 && (borderSize != borderPosition.y && borderPosition.y != Screen.height - borderSize))
        {
            borderPosition.x = borderSize;
        }
        else if (borderPosition.x > Screen.width - borderSize) { borderPosition.x = borderSize; }
        if (borderPosition.x > Screen.width / 2 && (borderSize != borderPosition.y && borderPosition.y != Screen.height - borderSize))
        {
            borderPosition.x = Screen.width - borderSize;
        }
        else if (borderPosition.x > Screen.width - borderSize) { borderPosition.x = Screen.width - borderSize; }

        // Set pointer location based on vector
        Vector3 pointerWorldPos = Camera.main.ScreenToWorldPoint(borderPosition);
        taskPointer.position = borderPosition;
    }

    void SetAtCorner()
    {
        questPointer.SetActive(false); 
        pointerImage.enabled = true; 
        taskPointer.localPosition = new Vector3(860, -440, 0);
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
