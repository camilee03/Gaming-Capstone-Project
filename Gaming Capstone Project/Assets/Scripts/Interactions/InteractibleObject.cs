using UnityEngine;

public class InteractibleObject : MonoBehaviour
{
    public bool PickUpable = false;
    public int amountOfHandsNeeded = 1;
    public float itemThickness = 0f;
    public bool customOffset = true;
    public Vector3 rotationOffset, positionOffset;
}
